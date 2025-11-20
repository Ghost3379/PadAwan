using Avalonia.Controls;
using Avalonia.Interactivity;
using PadAwan_Force.ViewModels;

namespace PadAwan_Force.Views
{
    public partial class UpdateWindow : Window
    {
        public UpdateWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

