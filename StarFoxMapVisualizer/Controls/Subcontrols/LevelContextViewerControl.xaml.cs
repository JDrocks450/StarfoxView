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
        public LevelContextViewerControl()
        {
            InitializeComponent();
        }
        //FILE PATH -> FILE TYPE
        Dictionary<string, string> ReferencedFiles = new();

        public MAPContextDefinition LevelContext { get; private set; }
        public async Task Attach(MAPContextDefinition levelContext, bool ExtractCCR = false, bool ExtractPCR = false)
        {
            ApplyButton.IsEnabled = false;
            LevelContext = levelContext;
            ContextDataGrid.ItemsSource = new[] { levelContext };
            await SetBGS(ExtractCCR, ExtractPCR);
        }
        private async Task<Bitmap> RenderSCR(string ColorPaletteName, string SCRName, string CHRName,  bool ExtractCCR = false, bool ExtractPCR = false)
        {
            var palette = GFXStandard.MAPContext_GetPaletteByName(ColorPaletteName, out var palettePath);
            if (palette == default) throw new FileNotFoundException($"{ColorPaletteName} was not found as" +
                $" an included Palette in this project."); // NOPE IT WASN'T
            ReferencedFiles.Add(palettePath, "Palette");
            //SET THE CHRName TO BE THE SCR NAME IF DEFAULT
            if (CHRName == null) CHRName = SCRName;
            //MAKE SURE BOTH OF THESE FILES ARE EXTRACTED AND EXIST
            //SEARCH AND EXTRACT CGX FIRST
            var CGXFileInfo = await GFXStandard.FindProjectCGXByName(CHRName, ExtractCCR);
            ReferencedFiles.Add(CGXFileInfo.FullName, "CGX");
            //THEN SCR
            var SCRFileInfo = await GFXStandard.FindProjectSCRByName(SCRName, ExtractPCR);
            ReferencedFiles.Add(SCRFileInfo.FullName, "SCR");
            return await GFXStandard.RenderSCR(palette, CGXFileInfo, SCRFileInfo);
        }
        private async Task SetBGS(bool ExtractCCR = false, bool ExtractPCR = false)
        {
            ReferencedFiles.Clear();
            //RENDER BG2
            if (LevelContext.BG2ChrFile != null && LevelContext.BG2ScrFile != null)
            {
                BG2Render.Visibility = Visibility.Visible;
                try
                {
                    using (var source = await RenderSCR(
                        LevelContext.BackgroundPalette,
                        LevelContext.BG2ScrFile,
                        LevelContext.BG2ChrFile,
                        ExtractCCR, ExtractPCR))
                        await Dispatcher.InvokeAsync(delegate
                        {                            
                            BG2Render.Source = source.Convert(true);
                        });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else BG2Render.Visibility = Visibility.Collapsed;
            //RENDER BG3
            if (LevelContext.BG3ChrFile != null && LevelContext.BG3ScrFile != null)
            {
                BG3Render.Visibility = Visibility.Visible;
                try
                {
                    using (var source = await GFXStandard.RenderSCR(
                        LevelContext.BackgroundPalette,
                        LevelContext.BG3ScrFile,
                        LevelContext.BG3ChrFile,
                        ExtractCCR, ExtractPCR))
                        await Dispatcher.InvokeAsync(delegate
                        {                            
                            BG3Render.Source = source.Convert();
                        });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else BG3Render.Visibility = Visibility.Collapsed;
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
            ImageContent.SetResourceReference(BackgroundProperty, "TransparentImageKey");
            hwnd.SetResourceReference(BackgroundProperty, "WindowBackgroundColor");
            hwnd.Closed += delegate
            {
                open = false;
                ImageContent.Background = null;
                ImageContentHost.Content = ImageContent;
            };
            open = true;
            hwnd.Show();
        }

        private void BreakdownButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Join("\n",ReferencedFiles.Select(x => $"{x.Value}: {x.Key}")));
        }
    }
}
