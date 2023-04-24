using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using JetBrains.Annotations;
using Scarab.ViewModels;

namespace Scarab.Views
{
    [UsedImplicitly]
    public class ModListView : View<ModListViewModel>
    {
        private readonly TextBox _search;
        private readonly MenuItem _bulkActions;
        private readonly List<MenuItem> _modFilterItems;

        private ModListViewModel ModListViewModel => (((StyledElement)this).DataContext as ModListViewModel)!;

        public ModListView()
        {
            InitializeComponent();

            this.FindControl<UserControl>(nameof(UserControl)).KeyDown += OnKeyDown;
            
            _search = this.FindControl<TextBox>("Search");
            _bulkActions = this.FindControl<MenuItem>("BulkActions");
            
            _modFilterItems= this.GetLogicalDescendants()
                .Where(x => x is MenuItem menuItem && (menuItem.Name?.StartsWith("ModFilter") ?? false))
                .Select(x => (MenuItem)x).ToList();

        }

        // MenuItem's Popup is not created when ctor is run. I randomly overrided methods until
        // I found one that is called after Popup is created. There is nothing special about ArrangeCore
        protected override void ArrangeCore(Rect finalRect)
        {
            base.ArrangeCore(finalRect);
            SetUpBulkActionsPopup();
        }

        // I havent found a way to set these properties normally
        private void SetUpBulkActionsPopup()
        {
            var menuItem_popup = typeof(MenuItem).GetField("_popup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_bulkActions);

            var popup = menuItem_popup as Popup ?? throw new Exception("Bulk Actions popup not found");

            popup.HorizontalOffset = 2;
            popup.PlacementMode = PlacementMode.Right;
            popup.PlacementAnchor = PopupAnchor.TopRight;
            popup.PlacementGravity = PopupGravity.TopRight;
            popup.OverlayDismissEventPassThrough = true;
            popup.IsLightDismissEnabled = true;
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
            e.Element.VisualChildren.OfType<Expander>().First().IsExpanded = false;
        }

        private void ModFilterPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;
            
            _modFilterItems.ForEach(x => x.Background = new SolidColorBrush(Colors.Transparent));

            menuItem.Background = Application.Current?.Resources["HighlightBlue"] as IBrush;
        }
    }
}
