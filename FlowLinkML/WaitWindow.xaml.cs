using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlowLinkML
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class WaitWindow : Window
    {
        private bool allowClose = false;

        public WaitWindow()
        {
            InitializeComponent();
        }

        public Task TaskHandler { get; set; }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            while (!TaskHandler.IsCompleted)
            {
                await Task.Delay(100);
            }

            allowClose = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!allowClose)
            {
                e.Cancel = true;
            }
        }
    }
}
