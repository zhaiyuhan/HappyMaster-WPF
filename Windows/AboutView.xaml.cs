using System;
using System.Collections.Generic;
using System.Linq;
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
using MaterialDesignThemes.Wpf;

namespace FramelessWPF.View
{
    /// <summary>
    /// AboutView.xaml 的交互逻辑
    /// </summary>
    public partial class AboutView : Window
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;

            LoginGrid.OpacityMask = this.Resources["ClosedBrush"] as LinearGradientBrush;
            System.Windows.Media.Animation.Storyboard std = Resources["ClosedStoryboard"] as System.Windows.Media.Animation.Storyboard;
            std.Completed += delegate { this.Close(); };
            std.Begin();
        }
    }
}
