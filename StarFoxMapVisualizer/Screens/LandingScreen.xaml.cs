using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using StarFox.Interop.ASM;
using StarFoxMapVisualizer.Controls2;
using System;
using System.Collections.Generic;
using System.IO;
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
        const string RecentTXTFileName = "recent.txt";
        bool RecentExists => File.Exists(RecentTXTFileName);

        private NavigationService _service;

        public LandingScreen()
        {
            InitializeComponent();

            if (!RecentExists)
                ClearRecentFile.Visibility = Visibility.Collapsed;
        }

        private void LandingScreen_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void GetStartedButton_Click(object sender, RoutedEventArgs e)
        {
            GetStartedButton.IsEnabled = false;
            string fileLoc = default;
            if (RecentExists)
                fileLoc = File.ReadAllText(RecentTXTFileName);

            bool result = false;
            for (int retries = 0; retries < 1; retries++)
            {
                if (fileLoc == default)
                { // SHOW FILE BROWSER
                    CommonOpenFileDialog dialog = new()
                    {
                        IsFolderPicker = true,
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    };
                    if (dialog.ShowDialog() is not CommonFileDialogResult.Ok)
                    {
                        GetStartedButton.IsEnabled = true;
                        return; // USER CANCELLED
                    }
                    fileLoc = dialog.FileName;
                }

                //TRY TO LOAD THE PROJECT
                if (await AppResources.TryImportProject(new System.IO.DirectoryInfo(fileLoc)))
                {
                    result = true;
                    break; // loading the project success, break out
                }
                fileLoc = default;
            }

            if (!result) return;

            //SET NEW RECENT FILE
            await File.WriteAllTextAsync(RecentTXTFileName, fileLoc);

            EditScreen screen = new EditScreen();
            //InstrumentPackerControl screen = new();
            ((MainWindow)Application.Current.MainWindow).Content = screen;
        }

        private void ClearRecentFile_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(RecentTXTFileName);
            ClearRecentFile.Visibility = Visibility.Collapsed;
        }
    }
}
