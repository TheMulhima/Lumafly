using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MsBox.Avalonia.Enums;
using PropertyChanged.SourceGenerator;
using ReactiveUI;
using Scarab.Enums;
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
        private readonly IGlobalSettingsFinder _settingsFinder;
        private readonly IInstaller _installer;
        private readonly IModSource _mods;
        private readonly IModDatabase _db;
        private readonly IReverseDependencySearch _reverseDependencySearch;
        private readonly IModLinksChanges _modlinksChanges;
        private readonly IUrlSchemeHandler _urlSchemeHandler;
        private readonly ScarabMode _scarabMode;
        
        [Notify("ProgressBarVisible")]
        public bool _pbVisible;

        [Notify("ProgressBarIndeterminate")]
        public bool _pbIndeterminate;

        [Notify("Progress")]
        public double _pbProgress;

        [Notify]
        public IEnumerable<ModItem> _selectedItems;

        [Notify]
        public string? _search;
        
        public bool _updating;

        [Notify]
        public bool _isExactSearch;

        [Notify]
        public bool _isNormalSearch = true;
        
        [Notify]
        public string _dependencySearchItem;

        [Notify]
        public HowRecentModChanged _howRecentModChanged_NewMods = HowRecentModChanged.Week, _howRecentModChanged_UpdatedMods = HowRecentModChanged.Week;
        [Notify]
        public bool _whatsNew_UpdatedMods, _whatsNew_NewMods = true;

        [Notify]
        public ModFilterState _modFilterState = ModFilterState.All;
        public IEnumerable<string> ModNames { get; }
        public SortableObservableCollection<SelectableItem<string>> TagList { get; }
        public SortableObservableCollection<SelectableItem<string>> AuthorList { get; }
        public ReactiveCommand<Unit, Unit> ToggleApi { get; }
        public ReactiveCommand<Unit, Unit> UpdateApi { get; } 
        public ReactiveCommand<Unit, Unit> ManuallyInstallMod { get; }

        public ModListViewModel(
            ISettings settings, 
            IModDatabase db, 
            IInstaller inst, 
            IModSource mods,
            IGlobalSettingsFinder settingsFinder, 
            IUrlSchemeHandler urlSchemeHandler, 
            ScarabMode scarabMode)
        {
            Trace.WriteLine("Initializing ModListViewModel");
            _settings = settings;
            _installer = inst;
            _mods = mods;
            _db = db;
            _settingsFinder = settingsFinder;
            _urlSchemeHandler = urlSchemeHandler;
            _scarabMode = scarabMode; 

            Trace.WriteLine("Creating Items");
            _items = new SortableObservableCollection<ModItem>(db.Items.OrderBy(ModToOrderedTuple));
            Trace.WriteLine("Items Created");
            
            SelectedItems = _selectedItems = _items;
            Trace.WriteLine("Items Selected");
            
            _reverseDependencySearch = new ReverseDependencySearch(_items);

            _modlinksChanges = new ModLinksChanges(_items, _settings, _scarabMode);

            _dependencySearchItem = "";

            ModNames = _items.Where(x => x.State is not NotInModLinksState { ModlinksMod:false }).Select(x => x.Name);

            ToggleApi = ReactiveCommand.CreateFromTask(ToggleApiCommand);
            UpdateApi = ReactiveCommand.CreateFromTask(UpdateApiAsync);
            ManuallyInstallMod = ReactiveCommand.CreateFromTask(ManuallyInstallModAsync);
            Trace.WriteLine("Reactive commands created");

            HashSet<string> tagsInModlinks = new();
            HashSet<string> authorsInModlinks = new();
            foreach (var mod in _items)
            {
                if (mod.HasTags) mod.Tags.ToList().ForEach(tag => tagsInModlinks.Add(tag));
                if (mod.HasAuthors) mod.Authors.ToList().ForEach(author => authorsInModlinks.Add(author));
            }

            TagList = new SortableObservableCollection<SelectableItem<string>>(tagsInModlinks.Select(x => 
                new SelectableItem<string>(
                    x,
                    GetTagLocalizedName(x),
                    false)));

            AuthorList = new SortableObservableCollection<SelectableItem<string>>(authorsInModlinks.Select(x => 
                new SelectableItem<string>(
                    x, 
                    x,
                    false)));
            
            TagList.SortBy(AlphabeticalSelectableItem);
            AuthorList.SortBy(AlphabeticalSelectableItem);
            
            Trace.WriteLine("Created tag and author list");

            Task.Run(async () =>
            {
                Trace.WriteLine("Started getting modlinks changes");
                await _modlinksChanges.LoadChanges();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    RaisePropertyChanged(nameof(LoadedWhatsNew));
                    RaisePropertyChanged(nameof(IsLoadingWhatsNew));
                    RaisePropertyChanged(nameof(ShouldShowWhatsNewInfoText));
                    RaisePropertyChanged(nameof(WhatsNewLoadingText));
                    RaisePropertyChanged(nameof(ShouldShowWhatsNewErrorIcon));
                    if (IsInWhatsNew) SelectMods();
                    Trace.WriteLine("Finished getting modlinks changes");
                });
            });

            // we set isvisible of "all", "out of date", and "whats new" filters to false when offline
            // so only "installed" and "enabled" are shown so we force set the filter state to installed
            if (_scarabMode == ScarabMode.Offline) SelectModsWithFilter(ModFilterState.Installed);

            Dispatcher.UIThread.InvokeAsync(async () => await HandleDownloadUrlScheme());
            Dispatcher.UIThread.InvokeAsync(async () => await HandleForceUpdateAllScheme());
            Dispatcher.UIThread.InvokeAsync(async () => await HandleRemoveGlobalSettingScheme());
        }

        private async Task HandleDownloadUrlScheme()
        {
            if (_urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.download })
            {
                Trace.WriteLine("Handling download url scheme");
                var modNamesAndUrls = _urlSchemeHandler.ParseDownloadCommand(_urlSchemeHandler.Data);

                if (modNamesAndUrls.Count == 0)
                {
                    await _urlSchemeHandler.ShowConfirmation(
                        title: Resources.MVVM_DownloadModUrlScheme_Header, 
                        message: Resources.MVVM_DownloadModUrlScheme_Invalid, 
                        Icon.Warning);
                    return;
                }

                List<string> successfulDownloads = new List<string>();
                List<string> failedDownloads = new List<string>();

                foreach (var (modName, url) in modNamesAndUrls)
                {
                    bool isCustomInstall = url != null;
                    bool isModlinksMod = true;
                    string? originalUrl = null;

                    var correspondingMod = _items.FirstOrDefault(x => x.Name == modName);
                    
                    // delete the corresponding mod if a custom link is provided
                    if (isCustomInstall && correspondingMod != null)
                        await InternalModDownload(correspondingMod, correspondingMod.OnInstall);

                    // re get the corresponding mod because the mod might have been manually installed and hence when uninstalled removed from list
                    correspondingMod = _items.FirstOrDefault(x => x.Name == modName);
                    
                    if (correspondingMod == null) // if its a manually installed mod
                    {
                        isModlinksMod = false;
                        if (url == null)
                        {
                            Trace.TraceError($"{UrlSchemeCommands.download}:{_urlSchemeHandler.Data} not found");
                            failedDownloads.Add(modName);
                            continue;
                        }

                        // create a new ModItem for our manually installed mod
                        correspondingMod = ModItem.Empty(
                            name: modName,
                            link: url,
                            description: Resources.MVVM_NotInModlinks_Description, 
                            state: new NotInstalledState());
                    }
                    else // This is a mod that exists in modlinks
                    {
                        isModlinksMod = true;
                        originalUrl = correspondingMod.Link;
                        if (isCustomInstall)
                        {
                            correspondingMod.Link = url ?? correspondingMod.Link; // replace with custom link if it exists
                            
                            // change the state from NotInModLinksState to NotInModlinks so it can skip hash check
                            correspondingMod.State = new NotInModLinksState(ModlinksMod: true);
                            
                        }
                    }

                    switch (correspondingMod.State)
                    {
                        case NotInstalledState:
                            await InternalModDownload(correspondingMod, correspondingMod.OnInstall);
                            break;
                        case InstalledState { Updated: false }:
                        case NotInModLinksState { ModlinksMod: true }:
                            await InternalModDownload(correspondingMod, correspondingMod.OnUpdate);
                            break;
                        case InstalledState { Enabled: false }:
                            await OnEnable(correspondingMod);
                            break;
                    }

                    if (isCustomInstall)
                    {
                        correspondingMod.Link = originalUrl ?? "";
                        if (correspondingMod.State is ExistsModState state)
                        {
                            correspondingMod.State = new NotInModLinksState(
                                ModlinksMod: isModlinksMod,
                                Enabled: state.Enabled,
                                Pinned: state.Pinned);
                            
                            // ensure the state is correctly recorded
                            await _mods.RecordInstalledState(correspondingMod);
                            correspondingMod.FindSettingsFile(_settingsFinder);
                        
                            FixupModList(correspondingMod);
                        }
                        else
                        {
                            // re-install the mod if it was not installed (in case of bad link)
                            await InternalModDownload(correspondingMod, correspondingMod.OnInstall);
                            failedDownloads.Add(modName);
                            continue;
                        }
                    }

                    successfulDownloads.Add(modName);
                }


                string message = string.Empty;

                if (successfulDownloads.Count > 0)
                    message +=
                        string.Format(Resources.MVVM_DownloadModUrlScheme_Body_Success, string.Join(Resources.Array_Sep, successfulDownloads));

                if (failedDownloads.Count > 0)
                {
                    message += string.Format(Resources.MVVM_DownloadModUrlScheme_Body_Failure, string.Join(Resources.Array_Sep, failedDownloads));
                }

                await _urlSchemeHandler.ShowConfirmation(
                    title: Resources.MVVM_DownloadModUrlScheme_Header,
                    message, 
                    failedDownloads.Count > 0 ? Icon.Warning : Icon.Success);
                Trace.WriteLine("Handled download url scheme");
            }
        }
        private async Task HandleForceUpdateAllScheme()
        {
            if (_urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.forceUpdateAll })
            {
                Trace.WriteLine("Handling force update url scheme");
                await ForceUpdateAll();
                
                await _urlSchemeHandler.ShowConfirmation(
                    title: Resources.MVVM_ForceUpdateUrlScheme_Header, 
                    message: Resources.MVVM_ForceUpdateUrlScheme_Body_Success);
            }
                
        }
        
        private async Task HandleRemoveGlobalSettingScheme()
        {
            if (_urlSchemeHandler is { Handled: false, UrlSchemeCommand: UrlSchemeCommands.removeGlobalSettings })
            {
                Trace.WriteLine("Handling remove global settings url scheme");
                var modNames = _urlSchemeHandler.Data.Split('/');

                if (modNames.Length == 0)
                {
                    await _urlSchemeHandler.ShowConfirmation(
                        title: Resources.MVVM_RemoveModGlobalSettingUrlScheme_Header, 
                        message: Resources.MVVM_RemoveModGlobalSettingUrlScheme_Invalid,
                        Icon.Warning);
                    return;
                }

                List<string> successfulDownloads = new List<string>();
                List<string> failedDownloads = new List<string>();

                foreach (var modName in modNames)
                {
                    var correspondingMod = _items.FirstOrDefault(x =>
                        x.Name == modName && x.State is not NotInModLinksState { ModlinksMod: false });
                    if (correspondingMod == null)
                    {
                        Trace.TraceError($"{UrlSchemeCommands.download}:{_urlSchemeHandler.Data} not found");
                        failedDownloads.Add(modName);
                        continue;
                    }

                    var file = _settingsFinder.GetSettingsFileLocation(correspondingMod);

                    if (file is null)
                    {
                        // if mod exists and gs is not found, then it is doesn't exist 
                        if (correspondingMod.State is not ExistsModState) failedDownloads.Add(modName);
                        else successfulDownloads.Add(modName);
                        continue;
                    }

                    if (File.Exists(file)) 
                        File.Delete(file);
                    
                    if (File.Exists(file + ".bak")) 
                        File.Delete(file + ".bak");

                    successfulDownloads.Add(modName);
                    correspondingMod.FindSettingsFile(_settingsFinder);
                }
                
                string message = string.Empty;

                if (successfulDownloads.Count > 0)
                    message += String.Format(Resources.MVVM_RemoveModGlobalSettingUrlScheme_Success, string.Join(Resources.Array_Sep, successfulDownloads));

                if (failedDownloads.Count > 0)
                {
                    message += string.Format(Resources.MVVM_RemoveModGlobalSettingUrlScheme_Failure, string.Join(Resources.Array_Sep, failedDownloads));
                }

                await _urlSchemeHandler.ShowConfirmation(
                    title: Resources.MVVM_RemoveModGlobalSettingUrlScheme_Header, 
                    message,
                    failedDownloads.Count > 0 ? Icon.Warning : Icon.Success);
                
                Trace.WriteLine("Handled remove global settings url scheme");
            }
        }
        public void ClearSearch()
        {
            Search = "";
            DependencySearchItem = "";
        }
        
        public bool NoFilteredItems => !FilteredItems.Any() && !IsInWhatsNew;
        
        public bool IsInWhatsNew => ModFilterState == ModFilterState.WhatsNew;

        public string WhatsNewLoadingText => _modlinksChanges.IsLoaded is null
            ? Resources.MVVM_LoadingWhatsNew 
            : (!_modlinksChanges.IsLoaded.Value ? Resources.MVVM_NotAbleToLoadWhatsNew : "");

        public bool IsLoadingWhatsNew => IsInWhatsNew && _modlinksChanges.IsLoaded is null;
        public bool ShouldShowWhatsNewInfoText => IsInWhatsNew && (_modlinksChanges.IsLoaded is null || !_modlinksChanges.IsLoaded.Value);
        public bool ShouldShowWhatsNewErrorIcon => IsInWhatsNew && (!_modlinksChanges.IsLoaded ?? false);
        public bool IsInOnlineMode => _scarabMode == ScarabMode.Online;
        public bool ShouldShowWhatsNew => IsInOnlineMode &&
                                          _settings.BaseLink == ModDatabase.DEFAULT_LINKS_BASE &&
                                          !_settings.UseCustomModlinks;

        public bool LoadedWhatsNew => IsInWhatsNew && (_modlinksChanges.IsLoaded ?? false);
        public bool ClearSearchVisible => !string.IsNullOrEmpty(Search) || !string.IsNullOrEmpty(DependencySearchItem);

        public IEnumerable<ModItem> FilteredItems
        {
            get
            {
                if (IsInWhatsNew)
                {
                    return SelectedItems
                        .Where(x =>
                            WhatsNew_UpdatedMods &&
                            x.RecentChangeInfo.ShouldBeShown(ModChangeState.Updated, HowRecentModChanged_UpdatedMods)
                            ||
                            WhatsNew_NewMods &&
                            x.RecentChangeInfo.ShouldBeShown(ModChangeState.New, HowRecentModChanged_NewMods));
                }
                
                if (IsNormalSearch)
                {
                    if (string.IsNullOrEmpty(Search)) 
                        return SelectedItems;
                    
                    string RemoveSpace(string s) => s.Replace(" ", string.Empty);
                    
                    if (IsExactSearch)
                        return SelectedItems.Where(x => x.Name.Contains(Search, StringComparison.OrdinalIgnoreCase));
                    else 
                        return SelectedItems.Where(x => RemoveSpace(x.Name).Contains(RemoveSpace(Search), StringComparison.OrdinalIgnoreCase) ||
                                                        RemoveSpace(x.Description).Contains(RemoveSpace(Search), StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    if (string.IsNullOrEmpty(DependencySearchItem))
                        return SelectedItems;
                    
                    // this isnt user input so we can do normal comparison
                    var mod = _items.First(x => x.Name == DependencySearchItem && x.State is not NotInModLinksState { ModlinksMod:false } );
                    
                    return SelectedItems
                        .Intersect(_reverseDependencySearch.GetAllDependentAndIntegratedMods(mod));
                }
            }
        }

        private void OnWhatsNew_UpdatedModsChanged() => FixupModList();
        private void OnWhatsNew_NewModsChanged() => FixupModList();
        private void OnHowRecentModChanged_NewModsChanged() => FixupModList();
        private void OnHowRecentModChanged_UpdatedModsChanged() => FixupModList();

        public string ApiButtonText => _mods.ApiInstall is InstalledState { Enabled: var enabled } 
            ? (
                enabled ? Resources.MLVM_ApiButtonText_DisableAPI 
                    : Resources.MLVM_ApiButtonText_EnableAPI 
            )
            : Resources.MLVM_ApiButtonText_ToggleAPI;

        public bool ApiOutOfDate => _mods.ApiInstall is InstalledState { Version: var v } && v.Major < _db.Api.Version;
        public bool CanUpdateAll => _items.Any(x => x.State is InstalledState { Updated: false }) && !_updating;
        public bool CanUninstallAll => _items.Any(x => x.State is ExistsModState);
        public bool CanDisableAll => _items.Any(x => x.State is ExistsModState { Enabled: true });
        public bool CanEnableAll => _items.Any(x => x.State is ExistsModState {Enabled: false});

        public static async Task ToggleApiCommand(IModSource _mods, IInstaller _installer)
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
        }

        public async Task ToggleApiCommand()
        {
            await ToggleApiCommand(_mods, _installer);
            RaisePropertyChanged(nameof(ApiButtonText));
            RaisePropertyChanged(nameof(ApiOutOfDate));
        }

        public void OpenModsDirectory()
        {
            var modsFolder = Path.Combine(_settings.ManagedFolder, "Mods");

            // Create the directory if it doesn't exist,
            // so we don't open a non-existent folder.
            Directory.CreateDirectory(modsFolder);
            
            Process.Start(new ProcessStartInfo(modsFolder) { UseShellExecute = true });
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

        public void Donate() => Process.Start(new ProcessStartInfo("https://ko-fi.com/mulhima") { UseShellExecute = true });


        public event Action? OnSelectModsWithFilter;
        
        public void SelectModsWithFilter(ModFilterState modFilterState)
        {
            ModFilterState = modFilterState;
            OnSelectModsWithFilter?.Invoke();
            SelectMods();
        }
        
        public void SelectMods()
        {
            SelectedItems = _modFilterState switch
            {
                ModFilterState.All => _items,
                ModFilterState.Installed => _items.Where(x => x.Installed),
                ModFilterState.Enabled => _items.Where(x => x.State is ExistsModState { Enabled: true }),
                ModFilterState.OutOfDate => _items.Where(x => x.State is InstalledState { Updated: false }),
                ModFilterState.WhatsNew when LoadedWhatsNew => _items.Where(x => x.RecentChangeInfo.IsUpdatedRecently || x.RecentChangeInfo.IsCreatedRecently),
                ModFilterState.WhatsNew when !LoadedWhatsNew => Array.Empty<ModItem>(),
                _ => throw new InvalidOperationException("Invalid mod filter state")
            };

            var selectedTags = TagList
                .Where(x => x.IsSelected)
                .Select(x => x.Item)
                .ToList();
            
            var selectedAuthors = AuthorList
                .Where(x => x.IsSelected)
                .Select(x => x.Item)
                .ToList();

            if (selectedTags.Count > 0)
            {
                SelectedItems = SelectedItems
                    .Where(x => x.HasTags &&
                                x.Tags.Any(tagsDefined => selectedTags
                                    .Any(tagsSelected => tagsSelected == tagsDefined)));
            }
            if (selectedAuthors.Count > 0)
            {
                SelectedItems = SelectedItems
                    .Where(x => x.HasAuthors &&
                                x.Authors.Any(authorsDefined => selectedAuthors
                                    .Any(authorsSelected => authorsSelected == authorsDefined)));
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

        public async Task UninstallAll()
        {
            await DisplayErrors.DoActionAfterConfirmation(true,
                () => DisplayErrors.DisplayAreYouSureWarning("Are you sure you want to uninstall all mods?"),
                async () =>
                {
                    var toUninstall = _items.Where(x => x.State is ExistsModState { Pinned: false }).ToList();
                    foreach (var mod in toUninstall)
                    {
                        if (mod.State is not ExistsModState)
                            continue;
                        
                        if (!HasPinnedDependents(mod))
                            await InternalModDownload(mod, mod.OnInstall);
                    }
                });
        }

        public async Task DisableAllInstalled()
        {
            var toDisable = _items.Where(x => x.State is ExistsModState { Enabled:true, Pinned: false }).ToList();

            foreach (ModItem mod in toDisable)
            {
                if (mod.State is not ExistsModState { Enabled:true })
                    continue;

                if (!HasPinnedDependents(mod))
                    await OnEnable(mod);
            }

            RaisePropertyChanged(nameof(CanDisableAll));
            RaisePropertyChanged(nameof(CanEnableAll));
            await Task.Delay(100); // sometimes installed buttons lose icons idk why
            FixupModList();
        }

        public async Task EnableAllInstalled()
        {
            var toEnable = _items.Where(x => x.State is ExistsModState { Enabled: false }).ToList();

            foreach (ModItem mod in toEnable)
            {
                if (mod.State is not ExistsModState { Enabled: false })
                    continue;

                await OnEnable(mod);
            }
            await Task.Delay(100); // sometimes installed buttons lose icons idk why
            FixupModList();
        }
        
        public async Task ForceUpdateAll()
        {
            // force update all will ignore pinned mods and force install the modlinks versions of mods
            var toUpdate = _items
                .Where(x => x.State is InstalledState or NotInModLinksState { ModlinksMod: true })
                .ToList();
            
            foreach (ModItem mod in toUpdate)
            {
                var state = (ExistsModState) mod.State;
                mod.State = new InstalledState(state.Enabled, new Version(0,0,0,0), false, state.Pinned);
                await _mods.RecordInstalledState(mod);
                mod.CallOnPropertyChanged(nameof(mod.UpdateAvailable));
                mod.CallOnPropertyChanged(nameof(mod.VersionText));
            }
            
            RaisePropertyChanged(nameof(FilteredItems));
            RaisePropertyChanged(nameof(SelectedItems));
            await UpdateUnupdated();
        }

        /// <summary>
        /// Enables or disables a mod and handles errors. Does other checks like warning about dependents and ensuring
        /// its dependencies are installed
        /// </summary>
        /// <param name="itemObj">The mod to enable/disable</param>
        public async Task OnEnable(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to enable an object which isn't a mod");
            try
            {
                // fix issues with dependencies:
                // if wants to disable make sure no mods dep on it
                // if wants to enable ensure all deps exist

                if (item.EnabledIsChecked)
                {
                    var dependents = _reverseDependencySearch.GetAllEnabledDependents(item).ToList();
                    
                    if (_settings.WarnBeforeRemovingDependents && dependents.Count > 0)
                    {
                        bool shouldContinue = await DisplayErrors.DisplayHasDependentsWarning(item.Name, dependents);
                        if (!shouldContinue)
                        {
                            item.CallOnPropertyChanged(nameof(item.EnabledIsChecked));
                            return;
                        }
                    }

                    await ResetPinned(item);
                }
                else
                {
                    var dependencies = item.Dependencies
                        .Select(x => _db.Items.First(i => i.Name == x))
                        .Where(x => x.State is NotInstalledState).ToList();

                    if (dependencies.Count > 0)
                    {
                        bool shouldDownload = await DisplayErrors.DisplayHasNotInstalledDependenciesWarning(item.Name, dependencies);
                        if (shouldDownload)
                        {
                            foreach (var dependency in dependencies)
                            {
                                if (dependency.State is NotInstalledState)
                                    await InternalModDownload(dependency, dependency.OnInstall);
                            }
                        }
                    }
                }
                
                await _installer.Toggle(item);
                item.FindSettingsFile(_settingsFinder);
            }
            catch (IOException io)
            {
                await DisplayErrors.HandleIOExceptionWhenDownloading(io, "toggle", item);
            }
            catch (Exception e)
            {
                await DisplayErrors.DisplayGenericError("toggling", item.Name, e);
            }
            
            // to reset the visuals of the toggle to the correct value
            item.CallOnPropertyChanged(nameof(item.EnabledIsChecked));
            RaisePropertyChanged(nameof(CanDisableAll));
            RaisePropertyChanged(nameof(CanEnableAll));
            SelectMods();
        }

        public async Task UpdateApiAsync()
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

        /// <summary>
        /// Installs/Uninstalls or Updates a mod, Also shows progress bar for it, and handles any errors 
        /// </summary>
        /// <param name="item">The mod to download</param>
        /// <param name="downloader">The task that downloads the mod, will be from the ModItem class</param>
        public async Task InternalModDownload(ModItem item, Func<IInstaller, Action<ModProgressArgs>, Task> downloader)
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
                }).ShowAsPopupAsync(AvaloniaUtils.GetMainWindow());

                if (res == ButtonResult.Yes)
                    proc.Kill();
            }

            try
            {
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
            item.FindSettingsFile(_settingsFinder);

            FixupModList();
        }
        
        /// <summary>
        /// Fixes up the mod list by removing mods that are not installed and not in mod links and then sorting the list.
        /// </summary>
        /// <param name="itemToAdd">A not in modlinks mod to be appended to the mod list display</param>
        public void FixupModList(ModItem? itemToAdd = null)
        {
            var removeList = _items.Where(x => x.State is NotInModLinksState { Installed: false }).ToList();
            foreach (var _item in removeList)
            {
                _items.Remove(_item);
                SelectedItems = SelectedItems.Where(x => x != _item);
            }

            if (itemToAdd != null && itemToAdd.State is NotInModLinksState {ModlinksMod: false})
            {
                // we add notinmodlinks mods to db but no need to actually save to disk because
                // next time we open scarab, moddatabase will handle it
                _db.Items.Add(itemToAdd);
                _items.Add(itemToAdd);
            }

            Sort();

            RaisePropertyChanged(nameof(CanUninstallAll));
            RaisePropertyChanged(nameof(CanDisableAll));
            RaisePropertyChanged(nameof(CanEnableAll));

            SelectMods();

            Sort();
        }

        public void Sort()
        {
            int Comparer(ModItem x, ModItem y)
            {
                // we use a special sort for whats new based on last changed
                if (ModFilterState == ModFilterState.WhatsNew)
                    return x.RecentChangeInfo.CompareTo(y.RecentChangeInfo);
                
                return ModToOrderedTuple(x).CompareTo(ModToOrderedTuple(y));
            }

            _items.SortBy(Comparer);
        }

        /// <summary>
        /// Updates a mod
        /// </summary>
        /// <param name="itemObj">The mod to update</param>
        public async Task OnUpdate(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to update an object which isn't a mod");
            await InternalModDownload(item, item.OnUpdate);
        }

        /// <summary>
        /// Installs or Uninstalls a mod based on its current state. If its uninstalled, warn if it has dependents and
        /// remove its dependencies if the settings ask for it
        /// </summary>
        /// <param name="itemObj"></param>
        /// <exception cref="Exception"></exception>
        public async Task OnInstall(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to install an object which isn't a mod");
            var dependents = _reverseDependencySearch.GetAllEnabledDependents(item).ToList();
            
            await DisplayErrors.DoActionAfterConfirmation(
                shouldAskForConfirmation: _settings.WarnBeforeRemovingDependents &&
                                          item.Installed &&
                                          dependents.Count > 0, // if its installed rn and has dependents
                warningPopupDisplayer: () => DisplayErrors.DisplayHasDependentsWarning(item.Name, dependents),
                action: async () =>
                {
                    await InternalModDownload(item, item.OnInstall);

                    if (!item.Installed)
                    {
                        await ResetPinned(item);
                        await RemoveUnusedDependencies(item);
                    }
                });
        }

        private async Task ManuallyInstallModAsync()
        {
            Window parent = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
                            ?? throw new InvalidOperationException();
            
            var files = await parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = Resources.MLVM_Select_Mod,
                AllowMultiple = true,
                FileTypeFilter = new []
                {
                    new FilePickerFileType("Mod")
                    {
                        Patterns = new []
                        {
                            "*.dll",
                            "*.zip"
                        }
                    },
                }
            });

            if (files.Count == 0)
                return;

            foreach (var file in files)
            {
                try
                {
                    var correspondingMod =
                        _items.FirstOrDefault(x => x.Name == Path.GetFileNameWithoutExtension(file.Name));
                    
                    var mod = correspondingMod ?? ModItem.Empty(
                        name: Path.GetFileNameWithoutExtension(file.Name), 
                        description: Resources.MVVM_NotInModlinks_Description);
                    
                    var oldState = mod.State;
                    
                    if (correspondingMod != null && correspondingMod.State is ExistsModState)
                    {
                        await _installer.Uninstall(correspondingMod);
                    }

                    await _installer.PlaceMod(
                    mod,
                    true,
                    file.Name,
                    await File.ReadAllBytesAsync(file.Path.LocalPath));

                    // make sure to only change state if the place is a success
                    if (correspondingMod != null)
                    {
                        mod.State = oldState switch
                        {
                            NotInModLinksState notInModLinksState => notInModLinksState with { Enabled = true },
                            InstalledState installedState => new NotInModLinksState(
                                ModlinksMod: true,
                                Enabled: true,
                                Pinned:installedState.Pinned),
                            NotInstalledState => new NotInModLinksState(ModlinksMod: false),
                            _ => throw new UnreachableException(),
                        };

                        await _mods.RecordInstalledState(correspondingMod);
                        correspondingMod.FindSettingsFile(_settingsFinder);
                    }
                    
                    FixupModList(mod);
                }
                catch(Exception e)
                {
                    await DisplayErrors.DisplayGenericError("Manually installing", file.Name, e);
                }
            }
        }

        public async Task RemoveUnusedDependencies(ModItem item)
        {
            var dependencies = item.Dependencies
                            .Select(x => _items.First(i => i.Name == x))
                            .Where(x => !_reverseDependencySearch.GetAllEnabledDependents(x).Any())
                            .Where(x => x.State is ExistsModState)
                            .ToList();

            if (dependencies.Count > 0)
            {
                var options = dependencies.Select(x => new SelectableItem<ModItem>(x, x.Name, true)).ToList();
                bool hasExternalMods = _items.Any(x => x.State is NotInModLinksState { ModlinksMod:false });

                bool shouldUninstall = await ShouldUnsintall(options, hasExternalMods);

                if (shouldUninstall)
                {
                    foreach (var option in options.Where(x => x.IsSelected))
                    {
                        if (option.Item.State is ExistsModState)
                            await InternalModDownload(option.Item, option.Item.OnInstall);
                    }
                }
            }
        }

        public async Task<bool> ShouldUnsintall(List<SelectableItem<ModItem>> options, bool hasExternalMods)
        {
            return _settings.AutoRemoveUnusedDeps switch
            {
                AutoRemoveUnusedDepsOptions.Never => false,
                AutoRemoveUnusedDepsOptions.Ask => await DisplayErrors.DisplayUninstallDependenciesConfirmation(options, hasExternalMods),
                AutoRemoveUnusedDepsOptions.Always => true,
                _ => false,
            };
        }

        public async Task PinMod(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to pin an object which isn't a mod");
            if (item.State is not ExistsModState state) return;
            
            await _installer.Pin(item, !state.Pinned);
            
            FixupModList();
            await Task.Delay(100); // sometimes installed buttons lose icons idk why
            FixupModList();
        }
        
        public async Task ResetMod(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to reset an object which isn't a mod");
            if (item.State is not ExistsModState) return;

            if (item.State is InstalledState or NotInModLinksState { ModlinksMod: true })
            {
                await InternalModDownload(item, item.OnInstall); // uninstall it
                await InternalModDownload(item, item.OnInstall); // reinstall it
            }
            var file = _settingsFinder.GetSettingsFileLocation(item);

            if (file is null) 
                return;

            if (File.Exists(file)) 
                File.Delete(file);
                    
            if (File.Exists(file + ".bak")) 
                File.Delete(file + ".bak");
            
            item.FindSettingsFile(_settingsFinder);
        }
        
        public void OpenFolder(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to opening an object which isn't a mod");
            if (item.State is not ExistsModState state) return;
            
            string base_folder = state.Enabled
                ? _settings.ModsFolder
                : _settings.DisabledFolder;

            string mod_folder = Path.Combine(base_folder, item.Name);

            Process.Start(new ProcessStartInfo
            {
                FileName = mod_folder,
                UseShellExecute = true,
            });
        }
        
        public async Task RegisterNotInModlinks(object itemObj)
        {
            var item = itemObj as ModItem ?? throw new Exception("Tried to register an object which isn't a mod");
            if (item.State is not ExistsModState state) return;
            
            item.State = new NotInModLinksState(
                ModlinksMod: true,
                Enabled: state.Enabled,
                Pinned: state.Pinned);

            await _mods.RecordUninstall(item); // remove from installed list
            await _mods.RecordInstalledState(item); // add it back as not in modlinks mod

            FixupModList();
            await Task.Delay(100); // sometimes installed buttons lose icons idk why
            FixupModList();
        }

        public bool HasPinnedDependents(ModItem mod)
        {
            return _reverseDependencySearch.GetAllEnabledDependents(mod).Any(x => x.State is InstalledState { Pinned: true });
        }

        public async Task ResetPinned(ModItem mod)
        {
            if (mod.State is ExistsModState { Pinned: true })
            {
                await PinMod(mod);
                Sort();
                SelectMods();
            }
        }

        public static (int pinned, int priority, string name) ModToOrderedTuple(ModItem m) =>
        (
            m.State is ExistsModState { Pinned: true } ? -1 : 1,
            m.State is InstalledState { Updated : false } ? -1 : 1,
            m.Name
        );
        
        public static int AlphabeticalSelectableItem(SelectableItem<string> item1, SelectableItem<string> item2) => 
            string.Compare(item1.Item, item2.Item, StringComparison.Ordinal);

        public string GetTagLocalizedName(string tag)
        {
            switch (tag)
            {
                case "Boss": return Resources.ModLinks_Tags_Boss;
                case "Gameplay": return Resources.ModLinks_Tags_Gameplay;
                case "Utility": return Resources.ModLinks_Tags_Utility;
                case "Cosmetic": return Resources.ModLinks_Tags_Cosmetic;
                case "Library": return Resources.ModLinks_Tags_Library;
                case "Expansion": return Resources.ModLinks_Tags_Expansion;
                default: return tag;
            }
        }

    }
}
