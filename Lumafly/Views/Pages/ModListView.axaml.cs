using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ColorTextBlock.Avalonia;
using Lumafly.Util;
using Lumafly.Enums;
using Lumafly.Models;
using Lumafly.ViewModels;

namespace Lumafly.Views.Pages
{
    public partial class ModListView : View<ModListViewModel>
    {
        private readonly List<MenuItem> _flyoutMenus;
        private List<MenuItem> _modFilterItems;
        private WindowNotificationManager? _notify;

        private ModListViewModel ModListViewModel => (((StyledElement)this).DataContext as ModListViewModel)!;

        public ModListView()
        {
            InitializeComponent();

            _modFilterItems = this.GetLogicalDescendants().OfType<MenuItem>()
                .Where(x => x.Name?.StartsWith("ModFilter") ?? false)
                .ToList();

            _flyoutMenus = this.GetLogicalDescendants().OfType<MenuItem>()
                .Where(x => x.Name?.StartsWith("Flyout") ?? false)
                .ToList();

        }
        
        // MenuItem's Popup is not created when ctor is run. I randomly override methods until
        // I found one that is called after Popup is created. There is nothing special about ArrangeCore
        protected override void ArrangeCore(Rect finalRect)
        {
            base.ArrangeCore(finalRect);
            
            SetUpFlyoutPopup();

            ModListViewModel.OnSelectModsWithFilter += ModFilterSelected;
            
            ModListViewModel.OnModDownloaded += (action, modName) => _notify?.Show(
                new Notification($"Mod {action}ed", 
                    $"{modName} has been {action.ToLower()}ed successfully",
                    NotificationType.Success, new TimeSpan(0,0,0,2)));

            ModListViewModel.PaneIsClosed += () => canCloseOnPointerExit = false;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            var topLevel = TopLevel.GetTopLevel(this);
            
            _notify = new WindowNotificationManager(topLevel)
            {
                MaxItems = 3,
                Margin = new Thickness(0, 50),
                Opacity = 0.65,
            };
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            ModFilterSelected(); // when switching tabs
        }

        // I haven't found a way to set these properties normally
        private void SetUpFlyoutPopup()
        {
            foreach (var flyoutMenu in _flyoutMenus)
            {
                var popup = flyoutMenu.GetPopup();

                popup.HorizontalOffset = 2;
                popup.Placement = PlacementMode.Right;
                popup.PlacementAnchor = PopupAnchor.TopRight;
                popup.PlacementGravity = PopupGravity.TopRight;
                popup.OverlayDismissEventPassThrough = true;
                popup.IsLightDismissEnabled = true;
            }
        }
        
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (Search == null) return;
            
            if (!Search.IsFocused && ModListViewModel.IsNormalSearch)
            {
                Search.Focus();
            }
        }

        private void PrepareElement(object? sender, ItemsRepeaterElementPreparedEventArgs e)
        {
            if (!e.Element.GetVisualChildren().Any())
                return;
            
            var expander = e.Element.GetVisualChildren().OfType<Expander>().FirstOrDefault();
            if (expander != null) expander.IsExpanded = false;
            
            // CTextBlock is the element that markdown avalonia uses for the text
            var cTextBlock = e.Element.GetLogicalDescendants().OfType<CTextBlock>().FirstOrDefault();
            if (cTextBlock != null) cTextBlock.FontSize = 12;

            if (e.Element.DataContext is ModItem modItem)
            {
                var modname = e.Element.GetLogicalDescendants().OfType<TextBlock>().First(x => x.Name == "ModName");
                var disclaimer = $" ({Lumafly.Resources.MVVM_NotInModlinks_Disclaimer})";
                if (modItem is { State: NotInModLinksState })
                {
                    if (modname is { Text: not null })
                    {
                        modname.Foreground = new SolidColorBrush(Colors.Orange);
                        if (!modname.Text.EndsWith(disclaimer))
                        {
                            modname.Text += disclaimer;
                        }
                    }
                }
                else
                {
                    if (modname is { Text: not null })
                    {
                        modname.Foreground = Application.Current?.Resources["TextColor"] as IBrush;
                        if (modname.Text.EndsWith(disclaimer))
                        {
                            modname.Text = modname.Text[..disclaimer.Length];
                        }
                    }
                }
            }
        }

        private void ModFilterSelected()
        {
            var selectedMenuItem = ModListViewModel.ModFilterState switch
            {
                ModFilterState.All => ModFilter_All,
                ModFilterState.Installed => ModFilter_Installed,
                ModFilterState.OutOfDate => ModFilter_OutOfDate,
                ModFilterState.Enabled => ModFilter_Enabled,
                ModFilterState.WhatsNew => ModFilter_WhatsNew,
                _ => throw new InvalidOperationException()
            };
            
            foreach (var menuItem in _modFilterItems.Where(x => x.Name != selectedMenuItem.Name))
            {
                menuItem.Background = Brushes.Transparent;
            }

            Debug.WriteLine(selectedMenuItem.Name);
            
            selectedMenuItem.Background = Application.Current?.Resources["HighlightBlue"] as IBrush;

            
            // mod filter all background sometimes gets stuck on blue for no reason. So bandage solution it this ig
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (selectedMenuItem.Name == ModFilter_All.Name) return;
                await Task.Delay(1);
                ModFilter_All.Background = Brushes.Transparent;
                await Task.Delay(10);
                ModFilter_All.Background = Brushes.Transparent;
                await Task.Delay(100);
                ModFilter_All.Background = Brushes.Transparent;
            });
        }

        private bool canCloseOnPointerExit = false;

        private void OpenPane(object? sender, PointerEventArgs e)
        {
            if (!ModListViewModel.PaneOpen)
            {
                ModListViewModel.PaneOpen = true;
                canCloseOnPointerExit = true;
            }
        }

        private void ClosePane(object? sender, PointerEventArgs e)
        {
            if (ModListViewModel.PaneOpen && canCloseOnPointerExit)
            {
                ModListViewModel.PaneOpen = false;
            }
        }
    }
}
