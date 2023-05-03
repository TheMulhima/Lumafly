using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ColorTextBlock.Avalonia;
using JetBrains.Annotations;
using Scarab.ViewModels;

namespace Scarab.Views
{
    [UsedImplicitly]
    public class ModListView : View<ModListViewModel>
    {
        private readonly TextBox _search;
        private readonly List<MenuItem> _flyoutMenus;
        private readonly List<MenuItem> _modFilterItems;

        public static FieldInfo MenuItemPopup =>
            typeof(MenuItem).GetField("_popup", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private ModListViewModel ModListViewModel => (((StyledElement)this).DataContext as ModListViewModel)!;

        public ModListView()
        {
            InitializeComponent();

            this.FindControl<UserControl>(nameof(UserControl)).KeyDown += OnKeyDown;
            
            _search = this.FindControl<TextBox>("Search");

            _modFilterItems = this.GetLogicalDescendants().OfType<MenuItem>()
                .Where(x => x.Name?.StartsWith("ModFilter") ?? false)
                .ToList();
            _flyoutMenus = this.GetLogicalDescendants().OfType<MenuItem>()
                .Where(x => x.Name?.StartsWith("Flyout") ?? false)
                .ToList();

        }
        
        // MenuItem's Popup is not created when ctor is run. I randomly overrided methods until
        // I found one that is called after Popup is created. There is nothing special about ArrangeCore
        protected override void ArrangeCore(Rect finalRect)
        {
            base.ArrangeCore(finalRect);
            SetUpFlyoutPopup();
        }

        // I havent found a way to set these properties normally
        private void SetUpFlyoutPopup()
        {
            foreach (var flyoutMenu in _flyoutMenus)
            {
                var menuItem_popup = MenuItemPopup.GetValue(flyoutMenu);

                var popup = menuItem_popup as Popup ?? throw new Exception("Bulk Actions popup not found");

                popup.HorizontalOffset = 2;
                popup.PlacementMode = PlacementMode.Right;
                popup.PlacementAnchor = PopupAnchor.TopRight;
                popup.PlacementGravity = PopupGravity.TopRight;
                popup.OverlayDismissEventPassThrough = true;
                popup.IsLightDismissEnabled = true;
            }
        }
        
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_search.IsFocused && ModListViewModel.IsNormalSearch)
            {
                _search.Focus();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        [UsedImplicitly]
        private void PrepareElement(object? sender, ItemsRepeaterElementPreparedEventArgs e)
        {
            if (e.Element.VisualChildren.Count == 0)
                return;
            
            var expander = e.Element.VisualChildren.OfType<Expander>().FirstOrDefault();
            if (expander != null) expander.IsExpanded = false;
            
            // CTextBlock is the element that markdown avalonia uses for the text
            var cTextBlock = e.Element.GetLogicalDescendants().OfType<CTextBlock>().FirstOrDefault();
            if (cTextBlock != null) cTextBlock.FontSize = 12;
        }

        private void ModFilterPressed(object? sender, PointerPressedEventArgs e)
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
