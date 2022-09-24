using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PlexDownloader.ViewModels;

namespace PlexDownloader.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InputTextBox.AddHandler(KeyDownEvent, KeyDown, RoutingStrategies.Tunnel);
        }

        protected override bool HandleClosing()
        {
            (DataContext as MainWindowViewModel).Closing();
            return base.HandleClosing();
        }

        private void KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                (DataContext as MainWindowViewModel).EnterClickedInBox();
            }
        }
    }
}