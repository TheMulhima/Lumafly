using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using ColorTextBlock.Avalonia;
using JetBrains.Annotations;
using Scarab.Extensions;
using Scarab.Models;
using Scarab.ViewModels;

namespace Scarab.Views.Pages
{
    public partial class ModListView : View<ModListViewModel>
    {
        private readonly List<MenuItem> _flyoutMenus;
        private List<MenuItem> _modFilterItems;

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
            RemoveNotVisibleModFilters();
        }

        private void RemoveNotVisibleModFilters()
        {
            _modFilterItems = _modFilterItems.Where(x => x.IsVisible).ToList();
            _modFilterItems.First().Background = Application.Current?.Resources["HighlightBlue"] as IBrush;
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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        [UsedImplicitly]
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
                var disclaimer = " (Not from modlinks)";
                if (modItem is { State: NotInModLinksState })
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (modname != null)
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
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (modname != null)
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

        private void ModFilterPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _modFilterItems.ForEach(x => x.Background = new SolidColorBrush(Colors.Transparent));

                menuItem.Background = Application.Current?.Resources["HighlightBlue"] as IBrush;
            }
        }
    }
}
