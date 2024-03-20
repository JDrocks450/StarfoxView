using StarFox.Interop.MAP;
using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Misc;
using StarFoxMapVisualizer.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using WpfPanAndZoom.CustomControls;

namespace StarFoxMapVisualizer.Controls
{
    public interface IPausable
    {
        bool Paused { get; }
        void Pause();
        void Unpause();
    }
    /// <summary>
    /// Interaction logic for MAPControl.xaml
    /// </summary>
    public partial class MAPControl : UserControl, IPausable
    {
        public bool Paused { get; private set; }

        public MAPControl()
        {
            InitializeComponent();
            MAPTabViewer.Items.Clear();            
        }

        double linePositionX = -600;

        private Dictionary<MAPFile, TabItem> tabMap = new();
        private const double DEF_X = 200;
        private double currentX = DEF_X, currentY = 500, yStep = -1d;
        private MAPFile? selectedFile => ((TabItem)MAPTabViewer.SelectedItem).Tag as MAPFile;

        //3D VIEWER VARS
        private MAP3DControl? MapWindow;
        private bool MapWindowOpened => MapWindow != default;
        //---

        public void Pause() => Paused = true;
        public void Unpause()
        {
            Paused = false;
            _ = InvalidateFiles();
        }
        /// <summary>
        /// Forces control to check <see cref="AppResources.OpenFiles"/> for any map files
        /// </summary>
        public async Task InvalidateFiles()
        {
            foreach (var file in AppResources.OpenMAPFiles)
                await OpenFile(file);
        }

        private void MapExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFile == null) return;
            var fileName = System.IO.Path.Combine(Environment.CurrentDirectory,
                "export","maps",$"{selectedFile.LevelData.Title}.sfmap");  
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
            using (var file = File.Create(fileName))
            {
                using (Utf8JsonWriter writer = new(file))
                    selectedFile.LevelData.Serialize(writer);
            }
            if (MessageBox.Show($"The map was successfully exported to:\n" +
                $"{fileName}\n" +
                $"Do you want to copy its location to the clipboard?", "Complete",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Clipboard.SetText(fileName);
        }

        private async void View3DButton_Click(object sender, RoutedEventArgs e)
        {
            var map3D = MapWindow = new MAP3DControl(selectedFile);
            await map3D.ShowMapContents();
            map3D.Closed += delegate
            { // WINDOW CLOSED
                MapWindow = null;
                View3DButton.Visibility= Visibility.Visible;
            };
            map3D.Show();
            View3DButton.Visibility = Visibility.Collapsed;
        }

        private void SetupPlayField(PanAndZoomCanvas PanCanvas, double CenterFieldX, double LevelYStart, Brush Foreground, double LevelYEnd = -100000000,
            string StartText = "DELAY", string EndText = "FINISH", int currentDelay = 0)
        {
            TextBlock AddFieldText(string Text, double X, double Y, bool major = false)
            {
                var textControl = new TextBlock()
                {
                    Text = Text,
                    FontSize = major ? 28 : 22,
                    FontFamily = new FontFamily("Consolas"),
                    Padding = new Thickness(10,5,10,5),
                    Foreground = Foreground
                };
                textControl.LayoutTransform = new RotateTransform(-45);
                Canvas.SetTop(textControl, Y);
                Canvas.SetLeft(textControl, X);
                Panel.SetZIndex(textControl, 0);
                return textControl;
            }
            Line delayLine = new Line()
            {
                X1 = CenterFieldX,
                X2 = CenterFieldX,
                Y1 = LevelYStart,
                Y2 = LevelYEnd,
                StrokeThickness = 2,
                Stroke = Foreground,
                Fill = Foreground,                
            };
            PanCanvas.Children.Add(delayLine);
            PanCanvas.Children.Add(AddFieldText(StartText, CenterFieldX, LevelYStart - 100));
            for (double y = LevelYStart; y >= LevelYEnd; y-=100)
            {
                bool major = y % 1000 == 0;
                PanCanvas.Children.Add(AddFieldText(currentDelay.ToString(), CenterFieldX, y, major));
                currentDelay += 100;
            }
            PanCanvas.Children.Add(AddFieldText(EndText, CenterFieldX, LevelYEnd - 200));
        }

        private void MapContextButton_Click(object sender, RoutedEventArgs e)
        {
            LevelContextViewer? viewer = default;
            if (!selectedFile.ReferencedContexts.Any())
            {
                viewer = new LevelContextViewer(FILEStandard.MAPImport.LoadedContextDefinitions)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };                
            }
            else
            {
                viewer = new LevelContextViewer(selectedFile.ReferencedContexts.Select(x => x.Key).ToArray())
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };
            }
            viewer.EditorPreviewSelectionChanged += delegate
            {

            };
            viewer.Show();
        }

        Brush MapNodeLineBrush = Brushes.Yellow;

        private void EnumerateEvents(MAPFile File, PanAndZoomCanvas EventCanvas, ref double levelStartY, ref double currentY, ref int runningDelay, bool autoDereference = true, 
            int Layer = 0)
        {            
            int getLayerXShift(int Layer) => Layer * 100; 
            var newDefX = ActualWidth / 2;
            if (newDefX <= 0) newDefX = DEF_X;
            currentX = newDefX;           
            double lastNodeWidth = 0;
            int previousDelay = 0;

            if (Layer == 0) MapNodeLineBrush = Brushes.Yellow;
            else MapNodeLineBrush = Brushes.DeepSkyBlue;

            foreach (var evt in File.LevelData.Events)
            {
                var control = new MapEventNodeControl(evt);
                EventCanvas.Children.Add(control);
                
                previousDelay = runningDelay;

                control.Measure(new Size(5000, 5000));                
                
                if (evt is IMAPDelayEvent delay)
                {
                    runningDelay += delay.Delay;
                    var desiredCurY = levelStartY - runningDelay - (control.DesiredSize.Height / 2);
                    //if (desiredCurY > currentY)
                        currentY = desiredCurY;
                    currentX = newDefX;
                }
                else
                {
                    currentX += lastNodeWidth + 20;
                    currentY -= 100;
                }

                double drawY = currentY;
                Canvas.SetTop(control, drawY);

                lastNodeWidth = control.DesiredSize.Width;
                double drawX = 200 + currentX + getLayerXShift(Layer);
                Canvas.SetLeft(control, drawX);

                Panel.SetZIndex(control, 1);

                Line delayLine = new Line()
                {
                    X1 = linePositionX + getLayerXShift(Layer),
                    X2 = drawX,
                    Y1 = -runningDelay + levelStartY,
                    Y2 = drawY + 50,
                    StrokeThickness = 1,
                    Stroke = MapNodeLineBrush,
                    Fill = MapNodeLineBrush,
                };
                EventCanvas.Children.Add(delayLine);

                if(evt is MAPJSREvent mapjsr && autoDereference) // SUBSECTION FOUND!!
                { // WERE ALLOWED TO INCLUDE IT
                    if (!FILEStandard.SearchProjectForFile($"{mapjsr.SubroutineName}.ASM", out var MAPInfo, false))
                        continue; // FAILED! Couldn't find the map.
                    var sub_map = FILEStandard.OpenMAPFile(MAPInfo).Result as MAPFile;
                    if (sub_map == default) continue; // FAILED! Couldn't open the map.
                    var sub_levelStartY = currentY;
                    var sub_runningDelay = runningDelay;
                    EnumerateEvents(sub_map, EventCanvas, ref sub_levelStartY, ref currentY, ref runningDelay, autoDereference, Layer+1);
                    SetupPlayField(EventCanvas, linePositionX + getLayerXShift(Layer+1), sub_levelStartY, Brushes.DeepSkyBlue, currentY - 200, 
                        mapjsr.SubroutineName, "RETURN", sub_runningDelay);
                }
            }
            MapNodeLineBrush = Brushes.Yellow;
        }

        private async Task OpenFile(MAPFile File)
        {
            if (tabMap.ContainsKey(File))
                return;
            var EventCanvas = new PanAndZoomCanvas()
            {
                Background = new SolidColorBrush(Color.FromArgb(1,255,255,255))
            };
            var tabItem = new TabItem()
            {
                Header = System.IO.Path.GetFileNameWithoutExtension(File.OriginalFilePath),
                Content = EventCanvas,
                Tag = File
            };
            MAPTabViewer.Items.Add(tabItem);
            MAPTabViewer.SelectedItem = tabItem;
            tabMap.Add(File, tabItem);

            bool autoDereference = File.ReferencedSubSections.Any();
            if (!AppResources.MapImporterAutoDereferenceMode && autoDereference)
            {
                if (MessageBox.Show("Automatically include referenced sub-sections of Map files is OFF.\n" +
                    $"This level contains {File.ReferencedSubSections.Count} subsections, include them anyway?\n" +
                    $"\n(this may take some time)",
                    "Auto-Include Sub-Sections?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    autoDereference = false;
            }
            await SetupEditor(File, EventCanvas, autoDereference);
        }

        private async Task SetupEditor(MAPFile File, PanAndZoomCanvas EventCanvas, bool autoDereference)
        {
            double levelStartY = 500;
            currentY = levelStartY;
            int runningDelay = 0;
            EnumerateEvents(File, EventCanvas, ref levelStartY, ref currentY, ref runningDelay, autoDereference);
            SetupPlayField(EventCanvas, linePositionX, levelStartY, Brushes.Yellow, currentY - 200);
            await SwitchEditorBackground(File.LevelContext);
        }        

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BackgroundRender.ResetViewports(new Rect(0,0,ActualWidth, ActualHeight));
        }

        private async void RefreshEditorButton_Click(object sender, RoutedEventArgs e)
        {
            //await SetupEditor(selectedFile, EventCanvas, true);
        }

        private async Task SwitchEditorBackground(MAPContextDefinition? Definition)
        {
            await BackgroundRender.Attach(Definition);
            BackgroundRender.ResetViewports(new Rect(0, 0, ActualWidth, ActualHeight));
        }

        internal async Task MapNodeSelected(MAPEvent MapEvent)
        {
            //SWITCH BACKGROUND TO THIS
            if (MapEvent is IMAPBGEvent BGEvent)
            {
                await SwitchEditorBackground(FILEStandard.MAPImport.FindContext(BGEvent.Background));
                return;
            }
            //CHECK 3D VIEWER OPENED
            if (MapWindowOpened)
            { // 3D CONTEXT
                MapWindow.CameraTransitionToObject(MapEvent);
                return;
            }
            //NO 3D VIEWER ATTACHED CONTEXT
            var screen = EditScreen.Current;
            await screen.ASMViewer.OpenSymbol(MapEvent.Callsite); // open the symbol in the assembly viewer
            screen.CurrentMode = EditScreen.ViewMode.ASM; // switch to this viewer
            await screen.HandleViewModes(); // update the view
        }
    }
}
