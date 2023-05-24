using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls2;
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

namespace StarFoxMapVisualizer.Screens
{
    /// <summary>
    /// Interaction logic for LandingScreen.xaml
    /// </summary>
    public partial class LandingScreen : Page
    {
        private NavigationService _service;

        public LandingScreen()
        {
            InitializeComponent();
        }

        private void LandingScreen_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void GetStartedButton_Click(object sender, RoutedEventArgs e)
        {
            GetStartedButton.IsEnabled = false;
            string fileLoc = default;
#if DEBUG
            fileLoc = @"E:\Solutions\repos\Starfox Source Code\ultrastarfox-master\SF";
#endif
            if (fileLoc == default)
            {
                CommonOpenFileDialog dialog = new()
                {
                    IsFolderPicker = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                if (dialog.ShowDialog() is not CommonFileDialogResult.Ok)
                {
                    GetStartedButton.IsEnabled = true;
                    return;
                }
                fileLoc = dialog.FileName;
            }
            //TRY TO LOAD THE PROJECT
            if (!await AppResources.TryImportProject(new System.IO.DirectoryInfo(fileLoc))) 
                return; // loading the project failed, bail out of this function

            //EditScreen screen = new EditScreen();
            InstrumentPackerControl screen = new();
            ((MainWindow)Application.Current.MainWindow).Content = screen;
        }
    }
}
