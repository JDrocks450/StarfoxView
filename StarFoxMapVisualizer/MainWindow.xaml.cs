using Microsoft.Win32;
using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarFoxMapVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //**CAD SET CONTEXT
            StarFox.Interop.GFX.CAD.CGX.GlobalContext.HandlePaletteIndex0AsTransparent = true;
            //**

            Loaded += OnLoad;
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            Title = AppResources.GetTitleLabel;
            await EDITORStandard.ShowNotification("Welcome to SFView!", delegate { }, TimeSpan.FromSeconds(5)); 
            return;            
        }

        internal void PushNotification(Notification Notification)
        {            
            var obj = (ContentControl)Template.FindName("UI_PARENT_NOTIFICATION", this);
            obj.Content = Notification;
            Notification.Show();
            Notification.Dismissed += delegate
            {
                obj.Content = null;
            };
        }
    }
}
