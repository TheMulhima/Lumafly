using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using JetBrains.Annotations;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using PropertyChanged.SourceGenerator;
using ReactiveUI;
using Scarab.Interfaces;
using Scarab.Models;
using Scarab.Services;
using Scarab.Util;

namespace Scarab.ViewModels
{
    public partial class ModListViewModel : ViewModelBase
    {
        private readonly SortableObservableCollection<ModItem> _items;

        private readonly ISettings _settings;
        private readonly IInstaller _installer;
        private readonly IModSource _mods;
        private readonly IModDatabase _db;
        private readonly ReverseDependencySearch _reverseDependencySearch;
        private readonly IModLinksChanges _modlinksChanges;
        
        [Notify("ProgressBarVisible")]
        private bool _pbVisible;

        [Notify("ProgressBarIndeterminate")]
        private bool _pbIndeterminate;

        [Notify("Progress")]
        private double _pbProgress;

        [Notify]
        private IEnumerable<ModItem> _selectedItems;

        [Notify]
        private string? _search;
        
        private bool _updating;

        [Notify]
        private bool _isExactSearch;

        [Notify]
        private bool _isNormalSearch = true;
        
        [Notify]
        private string _dependencySearchItem;

        [Notify]
        private bool _new7Days = true, _updated7Days = true;
        [Notify]
        private bool _whatsNew_UpdatedMods, _whatsNew_NewMods = true;

        [Notify]
        private ModFilterState _modFilterState = ModFilterState.All;
        public IEnumerable<string> ModNames { get; }
        public ObservableCollection<TagItem> TagList { get; }
        public ReactiveCommand<Unit, Unit> ToggleApi { get; }
        public ReactiveCommand<Unit, Unit> UpdateApi { get; } 
        public ReactiveCommand<Unit, Unit> ManuallyInstallMod { get; }
        public ReactiveCommand<Unit, Unit> ChangePath { get; }
        
        private static readonly Dictionary<string, string> ExpectedTagList = new Dictionary<string, string>
        {
            {"Boss", Resources.ModLinks_Tags_Boss},
            {"Gameplay", Resources.ModLinks_Tags_Gameplay},
            {"Utility", Resources.ModLinks_Tags_Utility},
            {"Cosmetic", Resources.ModLinks_Tags_Cosmetic},
            {"Library", Resources.ModLinks_Tags_Library},
            {"Expansion", Resources.ModLinks_Tags_Expansion},
        };
        
        public ModListViewModel(ISettings settings, IModDatabase db, IInstaller inst, IModSource mods)
        {
            _settings = settings;
            _installer = inst;
            _mods = mods;
            _db = db;

            _items = new SortableObservableCollection<ModItem>(db.Items.OrderBy(ModToOrderedTuple));

            SelectedItems = _selectedItems = _items;
            
            _reverseDependencySearch = new ReverseDependencySearch(_items);

            _modlinksChanges = new ModLinksChanges(_items, settings);
            
            Task.Run(async () =>
            {
                await _modlinksChanges.LoadChanges();
                RaisePropertyChanged(nameof(LoadedWhatsNew));
                RaisePropertyChanged(nameof(IsLoadingWhatsNew));
                RaisePropertyChanged(nameof(ShouldShowWhatsNewInfoText));
                RaisePropertyChanged(nameof(WhatsNewLoadingText));
                RaisePropertyChanged(nameof(ShouldShowWhatsNewErrorIcon));
                SelectMods();
            });

            _dependencySearchItem = "";

            ModNames = _items.Where(x => x.State is not NotInModLinksState).Select(x => x.Name);

            ToggleApi = ReactiveCommand.CreateFromTask(ToggleApiCommand);
            ChangePath = ReactiveCommand.CreateFromTask(ChangePathAsync);
            UpdateApi = ReactiveCommand.CreateFromTask(UpdateApiAsync);
            ManuallyInstallMod = ReactiveCommand.CreateFromTask(ManuallyInstallModAsync);

            HashSet<string> tagsInModlinks = new();
            foreach (var mod in _items)
            {
                if (!mod.HasTags) continue;
                foreach (var tag in mod.Tags)
                {
                    tagsInModlinks.Add(tag);
                }
            }

            TagList = new ObservableCollection<TagItem>(tagsInModlinks.Select(x => 
                new TagItem(
                    x,
                    ExpectedTagList.TryGetValue(x, out var localizedTag) ? localizedTag : x,
                    false)));
        }

        [UsedImplicitly]
        public void ClearSearch()
        {
            Search = "";
            DependencySearchItem = "";
        }
        
        [UsedImplicitly]
        private bool NoFilteredItems => !FilteredItems.Any() && !IsInWhatsNew;
        
        [UsedImplicitly]
        private bool IsInWhatsNew => ModFilterState == ModFilterState.WhatsNew;
        
        [UsedImplicitly]
        private string WhatsNewLoadingText => _modlinksChanges.IsReady is null
            ? Resources.MVVM_LoadingWhatsNew 
            : (!_modlinksChanges.IsReady.Value ? Resources.MVVM_NotAbleToLoadWhatsNew : "");

        [UsedImplicitly] 
        private bool IsLoadingWhatsNew => IsInWhatsNew && _modlinksChanges.IsReady is null;
        
        [UsedImplicitly] 
        private bool ShouldShowWhatsNewInfoText => IsInWhatsNew && (_modlinksChanges.IsReady is null || !_modlinksChanges.IsReady.Value);

        [UsedImplicitly] 
        private bool ShouldShowWhatsNewErrorIcon => IsInWhatsNew && (!_modlinksChanges.IsReady ?? false);

        [UsedImplicitly] 
        private bool LoadedWhatsNew => IsInWhatsNew && (_modlinksChanges.IsReady ?? false);

        [UsedImplicitly]
        private IEnumerable<ModItem> FilteredItems
        {
            get
            {
                if (IsInWhatsNew)
                {
                    return SelectedItems
                        .Where(x =>
                            WhatsNew_UpdatedMods &&
                            x.RecentChangeInfo.IsUpdatedRecently &&
                            x.RecentChangeInfo.LastUpdated >= DateTime.UtcNow.AddDays(-1 * (Updated7Days ? 8 : 31))
                            ||
                            WhatsNew_NewMods &&
                            x.RecentChangeInfo.IsCreatedRecently &&
                            x.RecentChangeInfo.LastCreated >= DateTime.UtcNow.AddDays(-1 * (New7Days ? 8 : 31)));
                }
                
                if (IsNormalSearch)
                {
                    if (string.IsNullOrEmpty(Search)) 
                        return SelectedItems;
                    
                    if (IsExactSearch)
                        return SelectedItems.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase));
                    else 
                        return SelectedItems.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                                                         x.Description.Contains(Search, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    if (string.IsNullOrEmpty(DependencySearchItem))
                        return SelectedItems;
                    
                    // this isnt user input so we can do normal comparison
                    var mod = _items.First(x => x.Name == DependencySearchItem && x.State is not NotInModLinksState);
                    
                    return SelectedItems
                        .Intersect(_reverseDependencySearch.GetAllDependentAndIntegratedMods(mod));
                }
            }
        }

        public string ApiButtonText => _mods.ApiInstall is InstalledState { Enabled: var enabled } 
            ? (
                enabled ? Resources.MLVM_ApiButtonText_DisableAPI 
                    : Resources.MLVM_ApiButtonText_EnableAPI 
            )
            : Resources.MLVM_ApiButtonText_ToggleAPI;

        public bool ApiOutOfDate => _mods.ApiInstall is InstalledState { Version: var v } && v.Major < _db.Api.Version;

        public bool CanUpdateAll => _items.Any(x => x.State is InstalledState { Updated: false }) && !_updating;
        public bool CanUninstallAll => _items.Any(x => x.State is InstalledState or NotInModLinksState);
        public bool CanDisableAll => _items.Any(x => x.State is InstalledState {Enabled: true} or NotInModLinksState {Enabled: true});
        public bool CanEnableAll => _items.Any(x => x.State is InstalledState {Enabled: false} or NotInModLinksState {Enabled: false});
        
        private async Task ToggleApiCommand()
        {
            async Task<bool> DoActionWithWithErrorHandling(Func<Task> Action)
            {
                try
                {
                    await Action();
                }
                catch (IOException io)
                {
                    await DisplayErrors.HandleIOExceptionWhenDownloading(io, $"{Resources.MVVM_Install} API");
                    return false;
                }
                catch (Exception e)
                {
                    await DisplayErrors.DisplayGenericError(Resources.MVVM_Install, "API", e);
                    return false;
                }

                return true;
            }

            bool shouldInstallAndToggle = false, shouldInstallVanilla = false;
            if (_mods.ApiInstall is InstalledState installedState)
            {
                //returns false only when api state is installed but it isnt when we check
                bool isApiActuallyInstalled = await _installer.CheckAPI();

                /* this accounts for edge case when scarab says api is installed (installedState.Enabled) when its not (!isApiActuallyInstalled)
                so we first install to ensure scarab's state matches with reality and then we toggle to do what user wants
                */
                shouldInstallAndToggle = !isApiActuallyInstalled && installedState.Enabled;

                // if we reach here we need to manually install vanilla cuz its not here
                shouldInstallVanilla = installedState.Enabled && !_mods.HasVanilla;

            }

            if (_mods.ApiInstall is not InstalledState)
            {
                var success = await DoActionWithWithErrorHandling(() => _installer.InstallApi());
                if (!success) return;

                if (shouldInstallAndToggle)
                    await _installer.ToggleApi();
            }
            else
            {
                if (shouldInstallVanilla)
                {
                    var success = await DoActionWithWithErrorHandling(() => _installer.InstallVanilla());
                    if (!success) return;
                }

                await _installer.ToggleApi();
            }

            RaisePropertyChanged(nameof(ApiButtonText));
            RaisePropertyChanged(nameof(ApiOutOfDate));
        }

        private async Task ChangePathAsync()
        {
            string? path = await PathUtil.SelectPathFallible();

            if (path is null)
                return;

            _settings.ManagedFolder = path;
            _settings.Save();

            await _mods.Reset();

            await MessageBoxUtil.GetMessageBoxStandardWindow(Resources.MLVM_ChangePathAsync_Msgbox_Title, 
                Resources.MLVM_ChangePathAsync_Msgbox_Text).Show();
            
            // Shutting down is easier than re-doing the source and all the items.
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }

        public void OpenModsDirectory()
        {
            var modsFolder = Path.Combine(_settings.ManagedFolder, "Mods");

            // Create the directory if it doesn't exist,
            // so we don't open a non-existent folder.
            Directory.CreateDirectory(modsFolder);
            
            Process.Start(new ProcessStartInfo(modsFolder) {
                    UseShellExecute = true
            });
        }
        
        public void OpenSavesDirectory()
        {
            // try catch just incase it doesn't exist
            try
            {
                Process.Start(new ProcessStartInfo(GlobalSettingsFinder.GetSavesFolder()) {
                    UseShellExecute = true 
                });
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to open saves directory. {e}");
            }
        }

        public static void Donate() => Process.Start(new ProcessStartInfo("https://ko-fi.com/mulhima") { UseShellExecute = true });

        [UsedImplicitly]
        public void SelectModsWithFilter(ModFilterState modFilterState)
        {
            ModFilterState = modFilterState;
            SelectMods();
        }
        
        private void SelectMods()
        {
            SelectedItems = _modFilterState switch
            {
                ModFilterState.All => _items,
                ModFilterState.Installed => _items.Where(x => x.Installed),
                ModFilterState.Enabled => _items.Where(x => x.State is InstalledState { Enabled: true } or NotInModLinksState { Enabled: true }),
                ModFilterState.OutOfDate => _items.Where(x => x.State is InstalledState { Updated: false }),
                ModFilterState.WhatsNew => _items.Where(x => x.RecentChangeInfo.IsUpdatedRecently || x.RecentChangeInfo.IsCreatedRecently),
                _ => throw new InvalidOperationException("Invalid mod filter state")
            };

            var selectedTags = TagList
                .Where(x => x.IsSelected)
                .Select(x => x.TagName)
                .ToList();

            if (selectedTags.Count > 0)
            {
                SelectedItems = SelectedItems
                    .Where(x => x.HasTags &&
                                x.Tags.Any(tagsDefined => selectedTags
                                    .Any(tagsSelected => tagsSelected == tagsDefined)));
            }

            RaisePropertyChanged(nameof(FilteredItems));
        }

        public async Task UpdateUnupdated()
        {
            _updating = false;
            
            RaisePropertyChanged(nameof(CanUpdateAll));
            
            var outOfDate = _items.Where(x => x.State is InstalledState { Updated: false }).ToList();

            foreach (ModItem mod in outOfDate)
            {
                // Mods can get updated as dependencies of others while doing this
                if (mod.State is not InstalledState { Updated: false })
                    continue;
                
                await OnUpdate(mod);
            }
        }

        [UsedImplicitly]
        private async Task UninstallAll()
        {
            await DisplayErrors.DoActionAfterConfirmation(true,
                () => DisplayErrors.DisplayAreYouSureWarning("Are you sure you want to uninstall all mods?"),
                async () =>
                {
                    var toUninstall = _items.Where(x => x.State is InstalledState or NotInModLinksState).ToList();
                    foreach (var mod in toUninstall)
                    {
                        if (mod.State is not (InstalledState or NotInModLinksState))
                            continue;

                        await InternalModDownload(mod, mod.OnInstall);
                    }
                });
        }

        public void DisableAllInstalled()
        {
            var toDisable = _items.Where(x => x.State is InstalledState {Enabled:true} or NotInModLinksState{Enabled:true}).ToList();

            foreach (ModItem mod in toDisable)
            {
                if (mod.State is not (InstalledState {Enabled:true} or NotInModLinksState {Enabled:true}))
                    continue;

                _installer.Toggle(mod);
            }

            RaisePropertyChanged(nameof(CanDisableAll));
            RaisePropertyChanged(nameof(CanEnableAll));
        }
        
        public async Task ForceUpdateAll()
        {
            var toUpdate = _items.Where(x => x.State is InstalledState).ToList();
            
            foreach (ModItem mod in toUpdate)
            {
                var state = (InstalledState) mod.State;
                mod.State = new InstalledState(state.Enabled, new Version(0,0,0,0), false);
                await _mods.RecordInstalledState(mod);
                mod.CallOnPropertyChanged(nameof(mod.UpdateAvailable));
                mod.CallOnPropertyChanged(nameof(mod.VersionText));
            }
            
            RaisePropertyChanged(nameof(FilteredItems));
            RaisePropertyChanged(nameof(SelectedItems));
            await UpdateUnupdated();
        }

        [UsedImplicitly]
        private async Task OnEnable(ModItem item)
        {
            try
            {
                var dependents = _reverseDependencySearch.GetAllEnabledDependents(item).ToList();

                if (!item.EnabledIsChecked ||
                    dependents.Count == 0 ||
                    await DisplayErrors.DisplayHasDependentsWarning(item.Name, dependents))
                {
                    await _installer.Toggle(item);
                }

                // to reset the visuals of the toggle to the correct value
                item.CallOnPropertyChanged(nameof(item.EnabledIsChecked));
                RaisePropertyChanged(nameof(CanDisableAll));
                RaisePropertyChanged(nameof(CanEnableAll));
                SelectMods();

            }
            catch (IOException io)
            {
                await DisplayErrors.HandleIOExceptionWhenDownloading(io, "toggle", item);
            }
            catch (Exception e)
            {
                await DisplayErrors.DisplayGenericError("toggling", item.Name, e);
            }
        }

        private async Task UpdateApiAsync()
        {
            try
            {
                await _installer.InstallApi();
            }
            catch (HashMismatchException e)
            {
                await DisplayErrors.DisplayHashMismatch(e);
            }
            catch (Exception e)
            {
                await DisplayErrors.DisplayGenericError("updating", name: "the API", e);
            }

            RaisePropertyChanged(nameof(ApiOutOfDate));
            RaisePropertyChanged(nameof(ApiButtonText));
        }

        private async Task InternalModDownload(ModItem item, Func<IInstaller, Action<ModProgressArgs>, Task> downloader)
        {
            static bool IsHollowKnight(Process p) => (
                   p.ProcessName.StartsWith("hollow_knight")
                || p.ProcessName.StartsWith("Hollow Knight")
            );
            
            if (Process.GetProcesses().FirstOrDefault(IsHollowKnight) is { } proc)
            {
                var res = await MessageBoxUtil.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                    ContentTitle = Resources.MLVM_InternalUpdateInstallAsync_Msgbox_W_Title,
                    ContentMessage = Resources.MLVM_InternalUpdateInstallAsync_Msgbox_W_Text,
                    ButtonDefinitions = ButtonEnum.YesNo,
                    MinHeight = 200,
                    SizeToContent = SizeToContent.WidthAndHeight,
                }).Show();

                if (res == ButtonResult.Yes)
                    proc.Kill();
            }

            try
            {
                // ensure no duplicate mods
                if (item.State is NotInstalledState)
                {
                    foreach (var similarItem in _items
                                 .Where(x => x.Name == item.Name && x.State is InstalledState or NotInModLinksState)
                                 .ToList())
                    {
                        await _installer.Uninstall(similarItem);
                    }
                }

                await downloader
                (
                    _installer,
                    progress =>
                    {
                        ProgressBarVisible = !progress.Completed;

                        if (progress.Download?.PercentComplete is not { } percent)
                        {
                            ProgressBarIndeterminate = true;
                            return;
                        }

                        ProgressBarIndeterminate = false;
                        Progress = percent;
                    }
                );
            }
            catch (HashMismatchException e)
            {
                Trace.WriteLine($"Mod {item.Name} had a hash mismatch! Expected: {e.Expected}, got {e.Actual}");
                await DisplayErrors.DisplayHashMismatch(e);
            }
            catch (HttpRequestException e)
            {
                await DisplayErrors.DisplayNetworkError(item.Name, e);
            }
            catch (IOException io)
            {
                Trace.WriteLine($"Failed to install mod {item.Name}. State = {item.State}, Link = {item.Link}");
                await DisplayErrors.HandleIOExceptionWhenDownloading(io, "installing or uninstalling", item);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to install mod {item.Name}. State = {item.State}, Link = {item.Link}");
                await DisplayErrors.DisplayGenericError("installing or uninstalling", item.Name, e);
            }

            // Even if we threw, stop the progress bar.
            ProgressBarVisible = false;

            RaisePropertyChanged(nameof(ApiButtonText));
            item.CallOnPropertyChanged(nameof(ModItem.HasSettings));

            FixupModList();
        }

        //TODO: dont use normal sorting
        private void FixupModList(ModItem? itemToAdd = null)
        {
            static int Comparer(ModItem x, ModItem y) => ModToOrderedTuple(x).CompareTo(ModToOrderedTuple(y));

            var removeList = _items.Where(x => x.State is NotInModLinksState { Installed: false }).ToList();
            foreach (var _item in removeList)
            {
                _items.Remove(_item);
                SelectedItems = SelectedItems.Where(x => x != _item);
            }

            if (itemToAdd != null)
            {
                // we add notinmodlinks mods so no need to actually save to disk because
                // next time we open scarab, moddatabase will handle it
                _db.Items.Add(itemToAdd);
                _items.Add(itemToAdd);
            }

            _items.SortBy(Comparer);

            RaisePropertyChanged(nameof(CanUninstallAll));
            RaisePropertyChanged(nameof(CanDisableAll));
            RaisePropertyChanged(nameof(CanEnableAll));

            SelectMods();

            _items.SortBy(Comparer);
        }

        [UsedImplicitly]
        private async Task OnUpdate(ModItem item) => await InternalModDownload(item, item.OnUpdate);

        [UsedImplicitly]
        private async Task OnInstall(ModItem item)
        {
            var dependents = _reverseDependencySearch.GetAllEnabledDependents(item).ToList();
            
            await DisplayErrors.DoActionAfterConfirmation(
                shouldAskForConfirmation: item.Installed && dependents.Count > 0, // if its installed rn and has dependents
                warningPopupDisplayer: () => DisplayErrors.DisplayHasDependentsWarning(item.Name, dependents),
                action: () => InternalModDownload(item, item.OnInstall));
        }

        [UsedImplicitly]
        private async Task ManuallyInstallModAsync()
        {
            Window parent = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
                            ?? throw new InvalidOperationException();
            
            var dialog = new OpenFileDialog
            {
                Title = Resources.MLVM_Select_Mod,
                Filters = new List<FileDialogFilter> { new () { Extensions = new List<string>() {"dll", "zip"} } }
            };

            string[]? paths = await dialog.ShowAsync(parent);
            if (paths is null || paths.Length == 0)
                return;

            foreach (var path in paths)
            {
                try
                {
                    var mod = ModItem.Empty(
                        name: Path.GetFileNameWithoutExtension(path),
                        description: "This mod was manually installed and is not from official modlinks");
                    
                    await _installer.PlaceMod(
                    mod,
                    true,
                    Path.GetFileName(path),
                    await File.ReadAllBytesAsync(path));

                    FixupModList(mod);
                    
                }
                catch(Exception e)
                {
                    await DisplayErrors.DisplayGenericError("Manually installing", Path.GetFileName(path), e);
                }
            }
        }

        private static (int priority, string name) ModToOrderedTuple(ModItem m) =>
        (
            m.State is InstalledState { Updated : false } ? -1 : 1,
            m.Name
        );
    }
}
