using StarFox.Interop.MAP;
using StarFox.Interop.MAP.CONTEXT;
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
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for LevelContextViewer.xaml
    /// </summary>
    public partial class LevelContextViewer : Window
    {
        public LevelContextViewer()
        {
            InitializeComponent();
            ViewBar.Visibility = Visibility.Collapsed;
        }
        public LevelContextViewer(MAPContextDefinition? levelContext) : this()
        {
            if (levelContext != null)
                Loaded += async delegate { 
                    await Attach(levelContext); 
                };
        }
        public LevelContextViewer(MAPContextFile ContextFile) : this()
        {
            if (ContextFile != default)
                AttachMany(ContextFile);
        }
        public MAPContextDefinition? SelectedLevelContext { get; private set; }
        public MAPContextFile? SelectedFile { get; private set; }

        public async Task Attach(MAPContextDefinition levelContext, bool ExtractCCR = false, bool ExtractPCR = false)
        {
            IsEnabled = false;
            SelectedLevelContext = levelContext;
            if (SelectedLevelContext == null)
            {
                IsEnabled= true;
                return;
            }
            Title = SelectedLevelContext.MapInitName;
            await LevelViewerControl.Attach(levelContext, ExtractCCR, ExtractPCR);
            IsEnabled = true;
        }
        public void AttachMany(MAPContextFile contextFile)
        {
            SelectedFile = contextFile;
            if (SelectedFile == null || !SelectedFile.Definitions.Any()) return;
            ViewBar.Visibility = Visibility.Visible;
            ViewSwitcher.SelectionChanged -= ChangeDefinition;
            ViewSwitcher.ItemsSource = contextFile.Definitions.Values;
            ViewSwitcher.SelectionChanged += ChangeDefinition;
            if (ViewSwitcher.HasItems)
                ViewSwitcher.SelectedIndex = 1;
        }

        private async void ChangeDefinition(object sender, SelectionChangedEventArgs e)
        {
            await Attach((MAPContextDefinition)ViewSwitcher.SelectedItem);
        }

        private async void ReextractButton_Click(object sender, RoutedEventArgs e)
        {
            await Attach((MAPContextDefinition)ViewSwitcher.SelectedItem, true, true);
        }

        private void HOST_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void HOST_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            var image = LevelViewerControl.BG2Render.Source as BitmapImage;
            if (image != null) Clipboard.SetImage(image);
        }
    }
}
