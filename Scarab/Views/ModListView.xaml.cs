using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        private readonly Stopwatch _bulkActionsMenuStopwatch = new();

        public ModListView()
        {
            InitializeComponent();

            this.FindControl<UserControl>(nameof(UserControl)).KeyDown += OnKeyDown;
            
            _search = this.FindControl<TextBox>("Search");
            
            var _bulkActions = this.FindControl<MenuItem>("BulkActions");
            var flyout = FlyoutBase.GetAttachedFlyout(_bulkActions)!;
            flyout.Opened += (sender, _) =>
            {
                _bulkActions.Background = SolidColorBrush.Parse("#505050");
            };
            flyout.Closed += (sender, _) =>
            {
                _bulkActions.Background = null;
                _bulkActionsMenuStopwatch.Start();
            };

            _bulkActions.PointerPressed += (sender, _) =>
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
            };
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

        [UsedImplicitly]
        private void RepositoryTextClick(object? sender, PointerReleasedEventArgs _)
        {
            if (sender is not TextBlock txt)
            {
                Trace.TraceWarning($"{nameof(RepositoryTextClick)} called with non TextBlock sender!");
                return;
            }

            Trace.WriteLine(txt.Text);

            try
            {

                Process.Start
                (
                    new ProcessStartInfo(txt.Text)
                    {
                        UseShellExecute = true
                    }
                );
            }
            catch (Exception e)
            {
                Trace.TraceError($"{nameof(RepositoryTextClick)} process spawn failed with error {e}");
            }
        }
    }
}
