using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Scarab.Interfaces;
using Scarab.ViewModels;

namespace Scarab.Views
{
    public class NewProfileWindow : Window
    {
        private NewProfileWindowViewModel? _viewModel;

        public NewProfileWindow()
        {
            InitializeComponent();
        }

        public NewProfileWindow(IModDatabase db)
        {
            InitializeComponent();

            DataContext = new NewProfileWindowViewModel(db);
            _viewModel = DataContext as NewProfileWindowViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        [UsedImplicitly]
        public void CreateProfile(object? sender, RoutedEventArgs routedEventArgs)
        {
            _viewModel?.CreateProfile();
            Close();
        }
    }
}
