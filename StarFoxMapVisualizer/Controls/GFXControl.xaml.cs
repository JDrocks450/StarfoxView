using StarFox.Interop;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.CONVERT;
using StarFox.Interop.GFX.DAT;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Dialogs;
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
        private Dictionary<string, GFXEditorState> stateMap = new();
        private class GFXEditorState
        {
            public string SelectedObjectPath;
            /// <summary>
            /// The canvas settings to use while viewing a CGX file
            /// </summary>
            public CanvasSizeDefinition CGXGraphicsCanvas { get; set; } = FXConvertConstraints.GetDefinition(FXConvertConstraints.FXCanvasTemplates.CGX);
            /// <summary>
            /// Specifies the file path to use for finding the CGX tiles that make up this screen
            /// </summary>
            public string? ManualSCRCGXFilePath;
            public int ScreenQuadrant = -1;
        }
        /// <summary>
        /// The currently open CGX file
        /// </summary>
        public string SelectedGraphic { get; private set; }
        private FXCGXFile? sGraphic {
            get {
                if (SelectedGraphic == null) return null;
                AppResources.OpenFiles.TryGetValue(SelectedGraphic, out var sgfx);
                return sgfx as FXCGXFile;
            }
        }
        /// <summary>
        /// The currently open CGX file
        /// </summary>
        public string SelectedScreen { get; private set; }
        private FXSCRFile? sScreen
        {
            get
            {
                if (SelectedScreen == null) return null;
                AppResources.OpenFiles.TryGetValue(SelectedScreen, out var sgfx);
                return sgfx as FXSCRFile;
            }
        }

        /// <summary>
        /// The currently selected Palette
        /// </summary>
        public COL? SelectedPalette => PaletteSelection.SelectedPalette?.Palette;       
        private CopyableImage DragImage;
        private GFXEditorState? CurrentState => ((TabItem)FileSelectorTabViewer.SelectedItem)?.Tag as GFXEditorState;
        private string? CurrentCGXFromState => CurrentState?.SelectedObjectPath;
        private string? CurrentSCRFromState => CurrentState?.SelectedObjectPath;
        private bool ScreenRendered = false;
        private CanvasSizeDefinition CurrentCanvasSizeFromState => 
            CurrentState?.CGXGraphicsCanvas as CanvasSizeDefinition
            ?? FXConvertConstraints.GetDefinition(FXConvertConstraints.FXCanvasTemplates.CGX);

        public GFXControl()
        {
            InitializeComponent();
            DragImage= new CopyableImage();

            GraphicDragView.Children.Add(DragImage);
            Loaded += OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            ModalMissingCGXDialog.Visibility = Visibility.Collapsed;
            RefreshFiles();
        }

        public async void RefreshFiles()
        {            
            TabItem GetTab(IImporterObject Object, Brush Background = default)
            {                
                if (!stateMap.TryGetValue(Object.OriginalFilePath, out var state))
                {
                    state = new GFXEditorState()
                    {
                        SelectedObjectPath = Object.OriginalFilePath,
                    };
                    stateMap.Add(Object.OriginalFilePath, state);
                }
                var item = new TabItem()
                {
                    Header = System.IO.Path.GetFileName(Object.OriginalFilePath),
                    Tag = state,
                };                
                if (Background != default) item.Background = Background;
                item.MouseDoubleClick += delegate
                {
                    int selectedIndex = FileSelectorTabViewer.SelectedIndex;
                    AppResources.ImportedProject.CloseFile(Object);
                    stateMap.Remove(Object.OriginalFilePath);
                    if (FileSelectorTabViewer.Items.Count <= 1) // more tabs, switch to the next one to the left
                        selectedIndex = -1;
                    else if (selectedIndex > 0)
                        selectedIndex--;
                    RefreshFiles();
                    if (selectedIndex > -1)
                        FileSelectorTabViewer.SelectedIndex = selectedIndex;
                    else DragImage.Source = null;
                };
                FileSelectorTabViewer.Items.Add(item);
                return item;
            }
            void FillCGXFiles()
            {
                var CGXFiles = AppResources.OpenFiles.Values.OfType<FXCGXFile>();
                foreach (var cgx in CGXFiles)
                {
                    var item = GetTab(cgx);
                    if (SelectedGraphic != null && cgx.OriginalFilePath == SelectedGraphic)
                        FileSelectorTabViewer.SelectedItem = item;
                }
            }
            void FillSCRFiles()
            {
                var SCRFiles = AppResources.OpenFiles.Values.OfType<FXSCRFile>();
                foreach (var scr in SCRFiles)
                {
                    var item = GetTab(scr, Brushes.DarkRed);
                    if (SelectedScreen != null && scr.OriginalFilePath == SelectedScreen)
                        FileSelectorTabViewer.SelectedItem = item;
                }
            }
            void FillPalettes() => PaletteSelection.InvalidatePalettes();

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
            if (FileSelectorTabViewer.SelectedIndex == -1 && FileSelectorTabViewer.Items.Count > 0)
                FileSelectorTabViewer.SelectedIndex = 0;
            PaletteSelection.SelectionChanged += PaletteChanged; ;
            await RenderOne();
        }

        private async void PaletteChanged(object sender, SelectionChangedEventArgs e) => await RenderOne();

        private async void FileChanged(object sender, SelectionChangedEventArgs e)
        {
            ModalMissingCGXDialog.Visibility = Visibility.Collapsed;
            SelectedGraphic = CurrentCGXFromState;
            if (sGraphic == null)
            {
                SelectedScreen = CurrentSCRFromState;
                if (sScreen == null) return;
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
            ScreenRendered = false;
            if (CurrentState == null) return;
            if (DragImage == null) return;
            DragImage.Source = null;
            if (SelectedPalette == null)
                return;
            if (sGraphic != null)
            {
                var canvas = CurrentCanvasSizeFromState;
                using (var palette = sGraphic.Render(SelectedPalette, -1, canvas.Width, canvas.Height))
                    DragImage.Source = palette.Convert();
            }
            else if (sScreen != null)
            {
                //FIND GFX
                if (!FindCGXForSCR(out var filePath))
                {
                    PromptSCRforCGX();
                    return;
                }
                ModalMissingCGXDialog.Visibility = Visibility.Collapsed;
                var graphics = AppResources.OpenFiles[filePath] as FXCGXFile;
                using (var palette = sScreen.Render(graphics,SelectedPalette,true,CurrentState.ScreenQuadrant))
                    DragImage.Source = palette.Convert();
                ScreenRendered = true;
            }
        }

        private bool FindCGXForSCR(out string? SelectedFile)
        {
            SelectedFile = default;
            if (CurrentState== null) return false; // INTERNAL ERROR
            if (CurrentState?.ManualSCRCGXFilePath == null)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(SelectedScreen);
                var results = AppResources.ImportedProject.SearchFile(fileName + ".CGX");
                if (!results.Any() || results.Count() > 1 || !AppResources.OpenFiles.ContainsKey(results.First().FilePath))
                    return false; // AMBIGUOUS  
                SelectedFile = results.First().FilePath;
                return true;
            }
            SelectedFile = CurrentState.ManualSCRCGXFilePath;
            return true;
        }

        private void DrawPalette()
        {
            if (SelectedPalette == null) return;
            using (var palette = SelectedPalette.RenderPalette())
                PaletteViewImage.Source = palette.Convert();            
        }

        private async void CanvasSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState == default) return;
            if (!ScreenRendered)
                PromptCanvasSizeDialog();
            else PromptScreenViewOptionsDialog();
            await RenderOne();
        }

        private void PromptScreenViewOptionsDialog()
        {
            var canvasDialog = new ScreenCanvasSettingsDialog()
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (!canvasDialog.ShowDialog() ?? true)
                return;
            var nCanvasSize = canvasDialog.SelectedQuadrant;
            if (nCanvasSize == null) return;
            CurrentState.ScreenQuadrant = nCanvasSize;
        }

        private void PromptCanvasSizeDialog()
        {
            var canvasDialog = new GFXCanvasSizeDialog(CurrentCanvasSizeFromState)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (!canvasDialog.ShowDialog() ?? true)
                return;
            var nCanvasSize = canvasDialog.SelectedCanvasSize;
            if (nCanvasSize == null) return;
            CurrentState.CGXGraphicsCanvas = nCanvasSize;
        }

        private void PromptSCRforCGX()
        {
            ModalMissingCGXDialog.Visibility = Visibility.Visible;
            OpenFilesComboBox.SelectionChanged -= OpenFilesComboBox_SelectionChanged;
            OpenFilesComboBox.Items.Clear();
            foreach (var CGXfile in AppResources.OpenFiles.Values.OfType<FXCGXFile>()) {
                OpenFilesComboBox.Items.Add(new ComboBoxItem()
                {
                    Content = CGXfile.ToString(),
                    Tag = CGXfile.OriginalFilePath
                });
            }
            OpenFilesComboBox.SelectionChanged+= OpenFilesComboBox_SelectionChanged;
        }

        private async void OpenFilesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentState == default) return;
            var selectedItem = (OpenFilesComboBox.SelectedItem as ComboBoxItem)?.Tag as string;
            if (selectedItem == default) return;
            CurrentState.ManualSCRCGXFilePath = selectedItem;
            await RenderOne();
        }
    }
}
