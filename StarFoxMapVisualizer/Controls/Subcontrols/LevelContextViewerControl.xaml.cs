using StarFox.Interop.MAP.CONTEXT;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
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

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for LevelContextViewerControl.xaml
    /// </summary>
    public partial class LevelContextViewerControl : UserControl
    {
        private double BG3X, BG3Y, BG2X, BG2Y, ScrWidth, ScrHeight;
        private bool IgnoreUserInput = false;

        public LevelContextViewerControl()
        {
            InitializeComponent();            
        }        

        public MAPContextDefinition LevelContext { get; private set; }
        public async Task Attach(MAPContextDefinition levelContext, bool ExtractCCR = false, bool ExtractPCR = false)
        {
            if (levelContext == default) return;
            LevelContext = levelContext;

            BG3X = LevelContext.BG3.HorizontalOffset;
            BG3Y = LevelContext.BG3.VerticalOffset;
            BG2X = LevelContext.BG2.HorizontalOffset;
            BG2Y = LevelContext.BG2.VerticalOffset;
            IgnoreUserInput = true;
            ViewOptions.XScrollSlider.Value = BG3X;
            ViewOptions.YScrollSlider.Value = BG3Y;
            ViewOptions.XScrollSlider2.Value = BG2X;
            ViewOptions.YScrollSlider2.Value = BG2Y;
            IgnoreUserInput = false;

            ApplyButton.IsEnabled = false;            
            ContextDataGrid.ItemsSource = new[] { levelContext };
            await ImageContent.Attach(LevelContext, ExtractCCR, ExtractPCR);
            ScrWidth = ScrHeight = ImageContent.Width = ImageContent.ActualHeight;
            ResetViewSettings();
        }        

        private void ResetViewSettings()
        {            
            ImageContent.SetViewportsToUniformSize(ScrWidth, ScrHeight, BG2X, BG2Y, BG3X, BG3Y, 1024);                                
        }

        private void ContextDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ApplyButton.IsEnabled = true;   
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyButton.IsEnabled = false;
            await Attach(LevelContext);
        }
        bool open = false;
        private void BG2Render_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (open) return;
            double width = ImageContent.Width;
            ImageContentHost.Content = null;
            //POP OUT LARGE VIEW
            Window hwnd = new()
            {
                Title = "Large Background Image Viewer",                
                Content = ImageContent,
                Width = 512, 
                Height = 512,
                Owner = Application.Current.MainWindow
            };
            hwnd.SizeChanged += delegate
            {
                ScrWidth = hwnd.Width;
                ScrHeight = hwnd.Height;
                ImageContent.Width = ScrWidth;
                ResetViewSettings();
            };
            hwnd.SetResourceReference(BackgroundProperty, "WindowBackgroundColor");
            hwnd.Closed += delegate
            {
                open = false;
                ImageContent.Background = null;
                ImageContentHost.Content = ImageContent;
                ImageContent.Width = width;
            };
            open = true;
            hwnd.Show();
        }

        private void BreakdownButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Join("\n",ImageContent.ReferencedFiles.Select(x => $"{x.Value}: {x.Key}")));
        }

        private void ViewOptions_BG2_ScrollValueChanged(object sender, (bool Horizontal, double Value) e)
        {
            if (IgnoreUserInput) return;
            if (e.Horizontal)
                BG2X = e.Value;
            else BG2Y = e.Value;
            ResetViewSettings();
        }

        private void ViewOptions_BG3_ScrollValueChanged(object sender, (bool Horizontal, double Value) e)
        {
            if (IgnoreUserInput) return;
            if (e.Horizontal)
                BG3X = e.Value;
            else BG3Y = e.Value;
            ResetViewSettings();
        }
    }
}
