using StarFox.Interop.GFX.DAT;
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
using static StarFox.Interop.GFX.CAD;
using static StarFox.Interop.GFX.Render;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for GFXControl.xaml
    /// </summary>
    public partial class GFXControl : UserControl
    {
        /// <summary>
        /// The currently open CGX file
        /// </summary>
        public FXCGXFile SelectedGraphic { get; private set; }
        /// <summary>
        /// The currently open CGX file
        /// </summary>
        public FXSCRFile SelectedScreen { get; private set; }
        /// <summary>
        /// The currently selected Palette
        /// </summary>
        public COL SelectedPalette { get; private set; }
        private Image DragImage { get; set; }

        public GFXControl()
        {
            InitializeComponent();
            DragImage = new Image();
            GraphicDragView.Children.Add(DragImage);
            Loaded += OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {            
            RefreshFiles();
        }

        public async void RefreshFiles()
        {
            void FillCGXFiles()
            {
                var CGXFiles = AppResources.OpenFiles.Values.OfType<FXCGXFile>();
                foreach (var cgx in CGXFiles)
                {
                    var item = new TabItem()
                    {
                        Header = System.IO.Path.GetFileName(cgx.OriginalFilePath),                        
                        Tag = cgx
                    };
                    FileSelectorTabViewer.Items.Add(item);
                    if (SelectedGraphic != null && cgx == SelectedGraphic)
                        FileSelectorTabViewer.SelectedItem = item;
                }
            }
            void FillSCRFiles()
            {
                var SCRFiles = AppResources.OpenFiles.Values.OfType<FXSCRFile>();
                foreach (var scr in SCRFiles)
                {
                    var item = new TabItem()
                    {
                        Header = System.IO.Path.GetFileName(scr.OriginalFilePath),
                        Tag = scr,
                        Background = Brushes.DarkRed,
                    };
                    FileSelectorTabViewer.Items.Add(item);
                    if (SelectedScreen != null && scr == SelectedScreen)
                        FileSelectorTabViewer.SelectedItem = item;
                }
            }
            void FillPalettes()
            {
                var COLFiles = AppResources.ImportedProject.Palettes;
                foreach (var col in COLFiles)
                {
                    var item = new ListViewItem()
                    {
                        Content = System.IO.Path.GetFileNameWithoutExtension(col.Key),
                        Tag = col.Value
                    };
                    PaletteSelection.Items.Add(item);
                    if (col.Value == SelectedPalette)
                        PaletteSelection.SelectedItem = item;
                }
            }
            FileSelectorTabViewer.SelectionChanged -= FileChanged;
            PaletteSelection.SelectionChanged -= PaletteChanged;
            FileSelectorTabViewer.Items.Clear();
            PaletteSelection.Items.Clear();
            //GET CGX FILES
            FillCGXFiles();
            //GET CGX FILES
            FillSCRFiles();
            //THEN PALETTES
            FillPalettes();
            FileSelectorTabViewer.SelectionChanged += FileChanged;
            PaletteSelection.SelectionChanged += PaletteChanged; ;
            await RenderOne();
        }

        private async void PaletteChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedPalette = ((ListViewItem)PaletteSelection.SelectedItem).Tag as COL;
            await RenderOne();
        }

        private async void FileChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedGraphic = ((TabItem)FileSelectorTabViewer.SelectedItem).Tag as FXCGXFile;
            if (SelectedGraphic == null)
            {
                SelectedScreen = ((TabItem)FileSelectorTabViewer.SelectedItem).Tag as FXSCRFile;
                if (SelectedScreen == null) return;
            }
            await RenderOne();
        }

        private async Task RenderOne()
        {                        
            //PALETTE RENDER
            DrawPalette();
            //GFX RENDER
            DrawGraphics();
        }

        private void DrawGraphics()
        {
            DragImage.Source = null;
            if (SelectedPalette == null)
                return;
            if (SelectedGraphic != null)
            {
                using (var palette = SelectedGraphic.Render(SelectedPalette))
                    DragImage.Source = palette.Convert();
            }
            else if (SelectedScreen != null)
            {
                //FIND GFX
                var fileName = System.IO.Path.GetFileNameWithoutExtension(SelectedScreen.OriginalFilePath);
                var results = AppResources.ImportedProject.SearchFile(fileName + ".CGX");
                if (!results.Any() || results.Count() > 1 || !AppResources.OpenFiles.ContainsKey(results.First().FilePath))
                    MessageBox.Show("Hey there! In order to view this Screen, include the corresponding *.CGX file. \n" +
                        "It needs to share the same name as this screen, it just ends with *.CGX!\n" +
                        "Come back here once you include that file.", "Woah there");
                var graphics = AppResources.OpenFiles[results.First().FilePath] as FXCGXFile;
                using (var palette = SelectedScreen.Render(graphics,SelectedPalette,true))
                    DragImage.Source = palette.Convert();
            }
        }

        private void DrawPalette()
        {
            if (SelectedPalette == null) return;
            using (var palette = SelectedPalette.RenderPalette())
                PaletteViewImage.Source = palette.Convert();            
        }
    }
}
