using StarFox.Interop.MAP;
using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Controls.Subcontrols;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

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

        private Dictionary<MAPScript, MAP_FINST> tabMap = new();
        private MAP_FINST? CurrentState {
            get
            {
                if (selectedScript == null) return null;
                tabMap.TryGetValue(selectedScript, out var val);
                return val;
            }
        }
        private MAPScript? selectedScript => ((TabItem)MAPTabViewer.SelectedItem).Tag as MAPScript;
        

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
            if (selectedScript == null) return;
            var fileName = System.IO.Path.Combine(Environment.CurrentDirectory,
                "export","maps",$"{selectedScript.Header.LevelMacroName}.sfmap");  
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
            using (var file = File.Create(fileName))
            {
                using (Utf8JsonWriter writer = new(file))
                    selectedScript.LevelData.Serialize(writer);
            }
            if (MessageBox.Show($"The map was successfully exported to:\n" +
                $"{fileName}\n" +
                $"Do you want to copy its location to the clipboard?", "Complete",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Clipboard.SetText(fileName);
        }

        private async void View3DButton_Click(object sender, RoutedEventArgs e)
        {
            var map3D = MapWindow = new MAP3DControl(selectedScript);
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
            string StartText = "DELAY", string EndText = "FINISH", bool ShowTimes = true, int YOffset = 0)
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

            double YPosition = Layer * 100 + YOffset;

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

            if (ShowTimes)
            {
                for (double x = Time; x <= LevelEnd; x += 100)
                {
                    bool major = x % 1000 == 0;
                    PanCanvas.Children.Add(AddFieldText(Time.ToString(), x, TextYPosition, major));
                    Time += 100;
                }
            }
            PanCanvas.Children.Add(AddFieldText(EndText, LevelEnd + 50, TextYPosition-50));
        }

        private void SetupMapLoops(MAPData LoopLevelData, Panel PanCanvas, int Layer, int SubMapEnterTime)
        {
            foreach (var loopRegion in LoopLevelData.SectionMarkers.Values)
            {
                if (!loopRegion.IsLooped) continue;
                SetupPlayFieldHorizontal(PanCanvas, Layer, loopRegion.EstimatedTimeStart + SubMapEnterTime, Brushes.Orange,
                    loopRegion.EstimatedTimeEnd + SubMapEnterTime, loopRegion.LabelName, "END LOOP", false, 20);
            }
        }

        private void MapContextButton_Click(object sender, RoutedEventArgs e)
        {
            LevelContextViewer? viewer = default;
            if (!selectedScript.ReferencedContexts.Any())
            {
                viewer = new LevelContextViewer(FILEStandard.MAPImport.LoadedContextDefinitions)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };                
            }
            else
            {
                viewer = new LevelContextViewer(selectedScript.ReferencedContexts.Select(x => x.Key).ToArray())
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

        private void EnumerateEvents(MAPScript File, Panel EventCanvas, ref int currentTime, bool autoDereference = true, int Layer = 0)            
        {
            double LayerShift = (Layer * 100);

            if (Layer == 0) MapNodeLineBrush = Brushes.Yellow;
            else MapNodeLineBrush = Brushes.DeepSkyBlue;
                        
            double YOffset() => treeLayer * 75;            
            lastY = LayerShift;
            lastX = currentTime;

            foreach (var evt in File.LevelData.Events)
            {
                //Adjust level timing
                if (evt is IMAPDelayEvent delay)
                    currentTime += delay.Delay;
                evt.LevelDelay = currentTime;

                var control = new MapEventNodeControl(evt);
                EventCanvas.Children.Add(control);               

                control.Measure(new Size(5000, 5000));                                                

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

                if (evt is MAPJSREvent mapjsr && autoDereference) // SUBSECTION FOUND!!
                { // WERE ALLOWED TO INCLUDE IT
                    MAPFile? sub_map = default;
                    MAPScript? sub_script = default;

                    try
                    {
                        sub_script = FILEStandard.GetMapScriptByMacroName(mapjsr.SubroutineName).Result;
                    }
                    catch { }

                    if (sub_script == default)
                    {
                        //Try to automatically find the subsection without getting the user involved
                        if (FILEStandard.SearchProjectForFile($"{mapjsr.SubroutineName}.ASM", out var MAPInfo, false))
                        {
                            sub_map = FILEStandard.OpenMAPFile(MAPInfo).Result;
                            if (sub_map == default ||
                                !sub_map.Scripts.TryGetValue(mapjsr.SubroutineName, out sub_script)) // FAILED! Couldn't open the map.
                            { // prompt the user for a new file
                                sub_script = null;
                            }
                        }
                        //Loop while user selects the right file
                        while (sub_script == null)
                        {
                            if (MessageBox.Show($"Could not find section: {mapjsr.SubroutineName}\n\n" +
                                        $"Would you like to select the file it's in?", "Subsection Not Found",
                                        MessageBoxButton.YesNo) == MessageBoxResult.No)
                                break; // User gives up
                            var file = FILEStandard.ShowGenericFileBrowser("Select the MAP file that contains this section");
                            if (file == default || !file.Any()) break;

                            sub_map = FILEStandard.OpenMAPFile(new FileInfo(file.First())).Result;
                            if (sub_map == default ||
                                !sub_map.Scripts.TryGetValue(mapjsr.SubroutineName, out sub_script)) // FAILED! Still isn't the right file.
                            { // prompt the user for a new file
                                sub_script = null;
                            }
                        }
                    }
                    else MessageBox.Show("SFOptim worked perfectly!");
                    if (sub_script == default) continue; // Could not load the file at all, move on
                                                         
                    CurrentState.StateObject.Subsections.Add(sub_script);
                    int section_StartTime = currentTime;
                    EnumerateEvents(sub_script, EventCanvas, ref currentTime, autoDereference, Layer + 1);
                    SetupPlayFieldHorizontal(EventCanvas, Layer + 1, section_StartTime, Brushes.DeepSkyBlue, currentTime,
                        mapjsr.SubroutineName, "RETURN");
                    SetupMapLoops(sub_script.LevelData, EventCanvas, Layer + 1, section_StartTime);
                    selectedScript.MergeSubsection(sub_script.LevelData);
                }
            }
            MapNodeLineBrush = Brushes.Yellow;
        }

        private async Task OpenFile(MAPFile File)
        {
            if (File == null) return;

            foreach(var script in File.Scripts.Values)            
                if (tabMap.ContainsKey(script)) return;            

            //ASYNC DISABLE
            IsEnabled = false;
            
            if (File.Scripts.Count == 0) return;
            if (File.Scripts.Count == 1) await OpenScript(File.Scripts.Values.First());
            else
            { // Many scripts here, prompt the user to pick one
                GenericMenuDialog dialog = new GenericMenuDialog("LEVEL SCRIPTS",
                    "The file you selected may have multiple scripts in it.\n" +
                    "Select the one you would like to view.",
                    File.Scripts.Select(x => $"{x.Key} ({x.Value.LevelData.Events.Count} events)").ToArray())
                {
                    Owner = Application.Current.MainWindow
                };
                if(dialog.ShowDialog() ?? false)
                    await OpenScript(File.Scripts.Values.ElementAtOrDefault(dialog.Selection));
            }

            IsEnabled = true;
        }

        private async Task OpenScript(MAPScript? Script)
        {
            if (Script == default) return;

            //ASYNC DISABLE
            IsEnabled = false;

            MAP_FINST state = new MAP_FINST();

            if (tabMap.ContainsKey(Script))
                state = tabMap[Script];

            if (state.Tab == default)
            {
                state.Tab = new TabItem()
                {
                    Header = Script.Header.LevelName ?? Script.Header.LevelMacroName,
                    Tag = Script
                };
                MAPTabViewer.Items.Add(state.Tab);
            }

            var tabItem = state.Tab;
            MAPTabViewer.SelectionChanged -= ChangeFile;
            MAPTabViewer.SelectedItem = tabItem;
            MAPTabViewer.SelectionChanged += ChangeFile;
            tabMap.TryAdd(Script, state);

            bool autoDereference = Script.ReferencedSubSections.Any();
            if (!AppResources.MapImporterAutoDereferenceMode && autoDereference)
            {
                if (MessageBox.Show("Automatically include referenced sub-sections of Map files is OFF.\n" +
                    $"This level contains {Script.ReferencedSubSections.Count} subsections, include them anyway?\n" +
                    $"\n(this may take some time)",
                    "Auto-Include Sub-Sections?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    autoDereference = false;
            }

            Panel EventCanvas = default;

            if (!state.StateObject.Loaded)
            {
                var dragCanvas = new PanAndZoomCanvas()
                {
                    Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255))
                };
                dragCanvas.LocationChanged += CanvasMoved;
                state.StateObject.ContentControl = dragCanvas;
                EventCanvas = state.StateObject.ContentControl;
                int Width = await SetupEditor(Script, EventCanvas, autoDereference) + 200;
                state.StateObject.LevelWidth = Width;
            }
            EventCanvas = state.StateObject.ContentControl;

            CurrentEditorControl.Content = EventCanvas;
            //ASYNC ENABLE
            IsEnabled = true;
        }

        private async void ChangeFile(object sender, SelectionChangedEventArgs e)
        {            
            var file = (MAPTabViewer.SelectedItem as TabItem).Tag as MAPScript;
            await OpenScript(file);
        }

        private async Task<int> SetupEditor(MAPScript File, Panel EventCanvas, bool autoDereference)
        {
            int Time = 0;
            expandedX = 0;
            treeLayer = 0; lastTime = 0;
            lastX = 0;

            EnumerateEvents(File, EventCanvas, ref Time, autoDereference);
            SetupPlayFieldHorizontal(EventCanvas, 0, 0, Brushes.Yellow, Time);
            SetupMapLoops(File.LevelData, EventCanvas, 0, 0);

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
        /// <summary>
        /// This function is used when the user selects a MapNode in the MAPControl.
        /// <para/>Map Nodes can represent many types of information, using the <paramref name="ComponentSelected"/>
        /// can narrow down what the user actually meant to select to get more info on.
        /// <para/><paramref name="ComponentSelected"/> being null indicates it's unclear what they meant to select
        /// and the most generic action should be taken
        /// </summary>
        /// <param name="MapEvent"></param>
        /// <param name="ComponentSelected">Must be of type <see cref="IMAPEventComponent"/></param>
        /// <returns></returns>
        internal async Task<bool> MapNodeSelected(MAPEvent MapEvent, Type? ComponentSelected)
        {
            if (!ComponentSelected.IsAssignableTo(typeof(IMAPEventComponent)))
                throw new ArgumentException("Selected Component Type is not a IMAPEventComponent");
            //SWITCH BACKGROUND TO THIS
            if (MapEvent is IMAPBGEvent BGEvent && ComponentSelected == typeof(IMAPBGEvent))
            {
                await SwitchEditorBackground(FILEStandard.MAPImport.FindContext(BGEvent.Background));
                return true;
            }
            //SWITCH TO SHAPE VIEWER TO VIEW SHAPE SELECTED
            if (MapEvent is IMAPShapeEvent ShapeEvent && ComponentSelected == typeof(IMAPShapeEvent))
                if (!await EDITORStandard.ShapeEditor_ShowShapeByName(ShapeEvent.ShapeName, -1))
                {
                    MessageBox.Show("Couldn't find any shapes by the name of: " + ShapeEvent.ShapeName,
                        "Switch Shape");
                    return false;
                }
                else return true;
            
            //CHECK 3D VIEWER OPENED
            if (MapWindowOpened)
            { // 3D CONTEXT
                return MapWindow.CameraTransitionToObject(MapEvent);
            }
            //NO 3D VIEWER ATTACHED CONTEXT
            await EDITORStandard.AsmEditor_OpenSymbol(MapEvent.Callsite); // open the symbol in the assembly viewer
            return true;
        }

        private void ChronologySlider_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            var control = ((PanAndZoomCanvas)CurrentState.StateObject.ContentControl);
            control.LocationChanged -= CanvasMoved;
            double value = CurrentState.StateObject.LevelWidth * ChronologySlider.Value;
            control.SetCanvasLocation(new Point(value, 0));
            control.LocationChanged += CanvasMoved;
        }

        private void CanvasMoved(object? sender, Point e)
        {
            var control = ((PanAndZoomCanvas)sender);
            ChronologySlider.Scroll -= ChronologySlider_Scroll;
            ChronologySlider.Value = Math.Min(1, Math.Max(0, control.Location.X / CurrentState.StateObject.LevelWidth));
            ChronologySlider.Scroll += ChronologySlider_Scroll;
        }
    }
}
