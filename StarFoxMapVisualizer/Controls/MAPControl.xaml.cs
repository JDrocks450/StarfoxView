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

        private Dictionary<MAPFile, MAP_FINST> tabMap = new();
        private MAP_FINST? CurrentState {
            get
            {
                if (selectedFile == null) return null;
                tabMap.TryGetValue(selectedFile, out var val);
                return val;
            }
        }
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

        private void SetupPlayFieldHorizontal(Panel PanCanvas, int Layer, int Time, Brush Foreground, double LevelEnd = 100000000,
            string StartText = "DELAY", string EndText = "FINISH")
        {
            TextBlock AddFieldText(string Text, double X, double Y, bool major = false)
            {
                var textControl = new TextBlock()
                {
                    Text = Text,
                    FontSize = major ? 28 : 22,
                    FontFamily = new FontFamily("Consolas"),
                    Padding = new Thickness(10, 5, 10, 5),
                    Foreground = Foreground
                };
                Canvas.SetTop(textControl, Y);
                Canvas.SetLeft(textControl, X);
                Panel.SetZIndex(textControl, 0);
                return textControl;
            }

            double YPosition = Layer * 100;

            Line delayLine = new Line()
            {
                X1 = Time,
                X2 = LevelEnd,
                Y1 = YPosition,
                Y2 = YPosition,
                StrokeThickness = 4,
                Stroke = Foreground,
                Fill = Foreground,
            };
            double TextYPosition = YPosition + 25;
            
            PanCanvas.Children.Add(delayLine);
            PanCanvas.Children.Add(AddFieldText(StartText, Time + 50, TextYPosition-50));
            
            for (double x = Time; x <= LevelEnd; x += 100)
            {
                bool major = x % 1000 == 0;
                PanCanvas.Children.Add(AddFieldText(Time.ToString(), x, TextYPosition, major));
                Time += 100;
            }
            PanCanvas.Children.Add(AddFieldText(EndText, LevelEnd + 50, TextYPosition-50));
        }

        private void SetupPlayFieldVertical(Panel PanCanvas, double CenterFieldX, double LevelYStart, Brush Foreground, double LevelYEnd = -100000000,
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
                StrokeThickness = 4,
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
        double expandedX = 0;
        int treeLayer = 0, lastTime = 0;
        double lastX = 0, lastY;

        private void EnumerateEvents(MAPFile File, Panel EventCanvas, ref int currentTime, bool autoDereference = true, int Layer = 0)            
        {
            double LayerShift = (Layer * 100);

            if (Layer == 0) MapNodeLineBrush = Brushes.Yellow;
            else MapNodeLineBrush = Brushes.DeepSkyBlue;
                        
            double YOffset() => treeLayer * 75;            
            lastY = LayerShift;
            lastX = currentTime;

            foreach (var evt in File.LevelData.Events)
            {
                var control = new MapEventNodeControl(evt);
                EventCanvas.Children.Add(control);               

                control.Measure(new Size(5000, 5000));                
                
                if (evt is IMAPDelayEvent delay)                
                    currentTime += delay.Delay;

                double middleX = currentTime;                
                double rightEdge = middleX + (control.DesiredSize.Width / 2);
                double leftEdge = middleX - (control.DesiredSize.Width / 2);

                treeLayer++;

                if (leftEdge > expandedX)
                {
                    treeLayer = 0;
                    lastX = currentTime;
                    lastY = LayerShift;
                }
                if (rightEdge > expandedX) expandedX = rightEdge;

                if (Math.Abs(lastX - middleX) > 40)
                {
                    lastX = currentTime;
                    lastY = LayerShift;
                }

                double drawX = middleX - (control.DesiredSize.Width / 2);
                Canvas.SetLeft(control, drawX);

                double drawY = (200 + LayerShift + YOffset());
                Canvas.SetTop(control, drawY);

                Panel.SetZIndex(control, 1);

                Line delayLine = new Line()
                {
                    X1 = lastX,
                    X2 = middleX,
                    Y1 = lastY,
                    Y2 = drawY,
                    StrokeThickness = 2,
                    Stroke = MapNodeLineBrush,
                    Fill = MapNodeLineBrush,
                };

                lastX = middleX;
                lastY = drawY;

                EventCanvas.Children.Add(delayLine);

                if(evt is MAPJSREvent mapjsr && autoDereference) // SUBSECTION FOUND!!
                { // WERE ALLOWED TO INCLUDE IT
                    if (!FILEStandard.SearchProjectForFile($"{mapjsr.SubroutineName}.ASM", out var MAPInfo, false))
                        continue; // FAILED! Couldn't find the map.
                    var sub_map = FILEStandard.OpenMAPFile(MAPInfo).Result as MAPFile;
                    if (sub_map == default) continue; // FAILED! Couldn't open the map.                    
                    CurrentState.StateObject.Subsections.Add(sub_map);
                    int section_StartTime = currentTime;
                    EnumerateEvents(sub_map, EventCanvas, ref currentTime, autoDereference, Layer+1);
                    SetupPlayFieldHorizontal(EventCanvas, Layer+1, section_StartTime, Brushes.DeepSkyBlue, currentTime, 
                        mapjsr.SubroutineName, "RETURN");
                    selectedFile.MergeSubsection(sub_map.LevelData);
                }
            }
            MapNodeLineBrush = Brushes.Yellow;
        }

        private async Task OpenFile(MAPFile File)
        {
            if (File == null) return;

            //ASYNC DISABLE
            IsEnabled = false;

            MAP_FINST state = new MAP_FINST();

            if (tabMap.ContainsKey(File))
                state = tabMap[File];

            if (state.Tab == default)
            {
                state.Tab = new TabItem()
                {
                    Header = System.IO.Path.GetFileNameWithoutExtension(File.OriginalFilePath),
                    Tag = File
                };                
                MAPTabViewer.Items.Add(state.Tab);
            }

            var tabItem = state.Tab;
            MAPTabViewer.SelectionChanged -= ChangeFile;
            MAPTabViewer.SelectedItem = tabItem;
            MAPTabViewer.SelectionChanged += ChangeFile;
            tabMap.TryAdd(File, state);

            bool autoDereference = File.ReferencedSubSections.Any();
            if (!AppResources.MapImporterAutoDereferenceMode && autoDereference)
            {
                if (MessageBox.Show("Automatically include referenced sub-sections of Map files is OFF.\n" +
                    $"This level contains {File.ReferencedSubSections.Count} subsections, include them anyway?\n" +
                    $"\n(this may take some time)",
                    "Auto-Include Sub-Sections?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    autoDereference = false;
            }

            Panel EventCanvas = default;

            if (!state.StateObject.Loaded)
            {
                state.StateObject.ContentControl = new PanAndZoomCanvas()
                {
                    Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255))
                };
                EventCanvas = state.StateObject.ContentControl;  
                int Width = await SetupEditor(File, EventCanvas, autoDereference) + 200;
                state.StateObject.LevelWidth = Width;
            }
            EventCanvas = state.StateObject.ContentControl;

            CurrentEditorControl.Content = EventCanvas;
            //ASYNC DISABLE
            IsEnabled = true;
        }

        private async void ChangeFile(object sender, SelectionChangedEventArgs e)
        {            
            var file = (MAPTabViewer.SelectedItem as TabItem).Tag as MAPFile;
            await OpenFile(file);
        }

        private async Task<int> SetupEditor(MAPFile File, Panel EventCanvas, bool autoDereference)
        {
            int Time = 0;
            expandedX = 0;
            treeLayer = 0; lastTime = 0;
            lastX = 0;
            EnumerateEvents(File, EventCanvas, ref Time, autoDereference);
            SetupPlayFieldHorizontal(EventCanvas, 0, 0, Brushes.Yellow, Time);
            await SwitchEditorBackground(File.LevelContext);
            return Time;
        }        

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BackgroundRender.ResizeViewports((int)ActualWidth, (int)ActualHeight);
        }

        private async void RefreshEditorButton_Click(object sender, RoutedEventArgs e)
        {
            //await SetupEditor(selectedFile, EventCanvas, true);
        }

        private async Task SwitchEditorBackground(MAPContextDefinition? Definition)
        {
            await BackgroundRender.Attach(Definition);
            BackgroundRender.ResizeViewports((int)ActualWidth, (int)ActualHeight);
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

        double oldScrollValue = 0;
        private void ChronologySlider_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            double delta = (ChronologySlider.Value - oldScrollValue);
            oldScrollValue = ChronologySlider.Value;
            double value = CurrentState.StateObject.LevelWidth * delta;
            ((PanAndZoomCanvas)CurrentState.StateObject.ContentControl).MoveCanvas(new Vector(-value, 0));

        }
    }
}
