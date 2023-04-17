using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
        private readonly Stopwatch _bulkActionsMenuStopwatch = new();

        public ModListView()
        {
            InitializeComponent();

            this.FindControl<UserControl>(nameof(UserControl)).KeyDown += OnKeyDown;
            
            _search = this.FindControl<TextBox>("Search");
            _bulkActions = this.FindControl<MenuItem>("BulkActions");
        }

        [UsedImplicitly]
        private void BulkActionsFlyoutOpened(object? sender, EventArgs e)
        {
            _bulkActions.Background = SolidColorBrush.Parse("#505050");
        }
        
        [UsedImplicitly]
        private void BulkActionsFlyoutClosed(object? sender, EventArgs e)
        {
            _bulkActions.Background = null;
            _bulkActionsMenuStopwatch.Start();
        }
        
        [UsedImplicitly]
        private void OpenBulkActionsMenu(object? sender, PointerPressedEventArgs e)
        {
            // any pointer pressed event outside the flyout will close it (including this button)
            // so if a close did happen less than 100ms before, then dont open it. 
            // if it causes problems, it can be reduced to 10 ms
            if (_bulkActionsMenuStopwatch.IsRunning)
            {
                _bulkActionsMenuStopwatch.Stop();
                var lastClosedTime = _bulkActionsMenuStopwatch.ElapsedMilliseconds;
                _bulkActionsMenuStopwatch.Reset();
                    
                if (lastClosedTime < 100)
                {
                    return;
                }
            }

            FlyoutBase.ShowAttachedFlyout((sender as Control)!);
        }
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_search.IsFocused)
                _search.Focus();
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
    }
}
