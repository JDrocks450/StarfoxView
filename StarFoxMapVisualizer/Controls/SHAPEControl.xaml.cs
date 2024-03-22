using StarFox.Interop.BSP;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.COLTAB.DEF;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using static StarFox.Interop.GFX.CAD;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for SHAPEControl.xaml
    /// </summary>
    public partial class SHAPEControl : UserControl, IPausable
    {        
        public int SelectedFrame { get; private set; } = 0;
        public int SelectedShape { get; private set; } = 0;

        public Dictionary<BSPShape, BSPFile> FileMap = new();

        /// <summary>
        /// The selected face in the editor control
        /// </summary>
        private BSPFace? EDITOR_SelectedFace = default;
        /// <summary>
        /// The current shape
        /// </summary>
        private BSPShape? currentShape;
        /// <summary>
        /// The currently open BSPFile
        /// </summary>
        private BSPFile CurrentFile;
        /// <summary>
        /// Invokes the model to update positions
        /// </summary>
        private Timer? animationClock;
        private bool animating = false;
        /// <summary>
        /// Blocks invokes to update the model
        /// </summary>
        private bool canShowShape = true;
        SFPalette? currentSFPalette;
        COLGroup? currentGroup;

        private Storyboard SceneAnimation;

        private bool EDITOR_AnimationPaused = false;

        public SHAPEControl()
        {
            InitializeComponent();            
            NameScope.SetNameScope(this, new NameScope());

            Loaded += SHAPEControl_Loaded;            
        }

        private void SHAPEControl_Loaded(object sender, RoutedEventArgs e)
        {            
            RedoLineWork();
            SetRotation3DAnimation();
            BSPTreeView.Items.Clear();
            PointsView.Items.Clear();
        }

        /// <summary>
        /// Tries to create a new palette using the COLTABFile added and a ColorPalettePtr
        /// </summary>
        /// <param name="ColorPaletteName"></param>
        /// <returns></returns>
        private bool CreateSFPalette(string ColorPaletteName)
        {
            if (currentGroup != null && currentGroup.Name.ToUpper() == ColorPaletteName.ToUpper()) return true;            
            try
            {
                return SHAPEStandard.CreateSFPalette(ColorPaletteName, out currentSFPalette, out currentGroup);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"While trying to make a SFPalette, this error occured: \n{ex.ToString()}\n" +
                    $" Execution cannot proceed.", "Palette Parse Procedure");
                return false;
            }
        }

        /// <summary>
        /// Applies a rotation animation to the main scene
        /// </summary>
        private void SetRotation3DAnimation()
        {
            if (SceneAnimation != default) return;            

            SceneAnimation = new Storyboard()
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);            
            this.RegisterName("SceneRotation", rotation);
            var transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new RotateTransform3D()
            {
                Rotation = rotation
            });
            transformGroup.Children.Add(new ScaleTransform3D()
            {
                ScaleX = 1,
                ScaleY = -1,
                ScaleZ = 1
            });
            var animation = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(30));                      
            SceneAnimation.Children.Add(animation);
            Storyboard.SetTargetName(animation, "SceneRotation");
            Storyboard.SetTargetProperty(animation, new PropertyPath(AxisAngleRotation3D.AngleProperty));
            MainSceneGroup.Transform = transformGroup;

            SceneAnimation.Begin(this, true);
        }
        /// <summary>
        /// Event pump when key is pressed for 3D camera movement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) =>
            Camera.MoveBy(e.Key,10).RotateBy(e.Key,3);

        Point from;
        /// <summary>
        /// 3D camera movement with mouse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var till = e.GetPosition(sender as IInputElement);
            double dx = till.X - from.X;
            double dy = till.Y - from.Y;
            from = till;

            var distance = dx * dx + dy * dy;
            if (distance <= 0d)
                return;

            if (e.MouseDevice.LeftButton is MouseButtonState.Pressed)
            {
                var angle = (distance / Camera.FieldOfView) % 45d;
                Camera.Rotate(new(dy, -dx, 0d), angle);
            }
        }
        /// <summary>
        /// Updates the model to show the next frame of animation
        /// </summary>
        /// <param name="state"></param>
        public void ChangeFrame(object? state)
        {
            void Show()
            {
                ShowShape(currentShape, SelectedFrame);
            }
            SelectedFrame++;
            if (SelectedFrame >= currentShape.Frames.Count)
                SelectedFrame = 0;
            Dispatcher.Invoke(delegate
            {
                Show();
            });
        }
        /// <summary>
        /// Start animating this object
        /// </summary>
        /// <param name="shape"></param>
        private void StartAnimationFrames(BSPShape shape)
        {
            if (shape.Frames.Count <= 0) return;
            animating = true;
            if (animationClock != null)
                EndAnimatingFrames();
            currentShape = shape;
            animationClock = new Timer(ChangeFrame, null, 1000 / 15, 1000);
        }
        /// <summary>
        /// Stop animating this object
        /// </summary>
        private void EndAnimatingFrames()
        {
            animating = false;
            animationClock?.Dispose();
            animationClock = null;
        }
        /// <summary>
        /// Draws lines behind the main 3D canvas
        /// </summary>
        private void RedoLineWork(int SizeX = 1000, int SizeY = 1000, int Step = 50)
        {
            int MX_X = SizeX, MX_Y = SizeY, STEP = Step;
            LineWorkCanvas.Children.Clear();
            for (int X = 0; X < MX_X; X += STEP)
            {
                var line = new Line()
                {
                    X1 = X,
                    X2 = X,
                    Y1 = 0,
                    Y2 = MX_Y,
                    Stroke = Brushes.LawnGreen
                };
                LineWorkCanvas.Children.Add(line);
            }
            for (int Y = 0; Y < MX_Y; Y += STEP)
            {
                var line = new Line()
                {
                    X1 = 0,
                    X2 = MX_X,
                    Y1 = Y,
                    Y2 = Y,
                    Stroke = Brushes.LawnGreen
                };
                LineWorkCanvas.Children.Add(line);
            }
        }

        public bool Paused { get; private set; }
        public void Pause() => Paused = true;
        public void Unpause()
        {
            //NO PALETTES!
            if (!AppResources.ImportedProject?.Palettes.Any() ?? true) {
                MessageBox.Show("Looks like you don't have a Palette included in this project yet.\n" +
                "It is recommended you use night.col by default.", "Missing Palette");
                return;
            }
            if (SHAPEStandard.ProjectColorTable == default) {
                MessageBox.Show("There isn't a Color Table Definition file added to this project.\n" +
                    "Look for COLTABS.asm and include it.", "Missing Color Table Definitions");
                return;
            }
            Paused = false;
            InvalidateFiles();
        }

        /// <summary>
        /// Forces control to check <see cref="AppResources.OpenFiles"/> for any map files
        /// </summary>
        public void InvalidateFiles()
        {
            ShapeSelector.Items.Clear();
            FileMap.Clear();
            ShapeSelector.SelectionChanged -= ShapeSelector_SelectionChanged;
            foreach (var file in AppResources.OpenFiles.Values.OfType<BSPFile>())
                OpenFile(file);
            ShapeSelector.SelectedIndex = -1;
            ShapeSelector.SelectionChanged += ShapeSelector_SelectionChanged;  
            ShapeSelector.SelectedIndex = SelectedShape;
        }

        /// <summary>
        /// Opens a file to view the models inside
        /// </summary>
        /// <param name="file"></param>
        private void OpenFile(BSPFile file)
        {
            SelectedShape = (ShapeSelector.Items.Count - 1) + (file.Shapes.Count > 0 ? 1 : 0);
            foreach (var shape in file.Shapes)
            {
                ShapeSelector.Items.Add(new ComboBoxItem()
                {
                    Content = $"{shape.Header.UniqueName} [{System.IO.Path.GetFileName(file.OriginalFilePath)}]",
                    Tag = shape
                });
                FileMap.Add(shape, file);
            }                                                                  
        }

        private void Clear3DScreen()
        {
            MainSceneGroup.Children.Clear();
            //MainSceneGroup.Children.Add(MainLight);
        }        
        /// <summary>
        /// Invokes the control to draw a model with the current frame
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="Frame"></param>
        private void ShowShape(BSPShape? shape, int Frame = -1)
        {           
            if (!canShowShape) return; // showing shapes is blocked rn
            if (shape == null) return; // there is no shape to speak of            
            // our palette hasn't been rendered or we're forced to update it
            if (!CreateSFPalette("id_0_c")) return;

            currentShape = shape;
            
            //Block additional calls to render
            canShowShape = false;
            //Stop animating, please
            EndAnimatingFrames();
            //can we use persistant data?
            Clear3DScreen();

            var group = currentGroup;
            List<GeometryModel3D> models = new();
            try
            {
                //Use the standard SHAPE library function to render the shape to a MeshGeom
                models = SHAPEStandard.MakeBSPShapeMeshGeometry(
                    shape, in group, in currentSFPalette, Frame, EDITOR_SelectedFace);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reticulating Splines resulted in: \n" +
                    $"{ex.Message}\nEnding preview.", "Error Occured");
                EndAnimatingFrames();
                canShowShape = true;
                return;
            }
            if (shape.Frames.Count > 0)
            {
                if (!EDITOR_AnimationPaused)
                    StartAnimationFrames(shape);
                FrameSelector.SelectionChanged -= FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = Frame;
                FrameSelector.SelectionChanged += FrameSelector_SelectionChanged;
            }
            PopulatePointsView(shape, Frame);
            canShowShape = true;
            foreach (var model in models)
                MainSceneGroup.Children.Add(model);
        }

        /// <summary>
        /// Populates the BSPTreeView Control in the Editor
        /// </summary>
        /// <param name="Shape"></param>
        private void PopulateBSPTreeView(BSPShape Shape)
        {
            BSPTreeView.Items.Clear();
            foreach(var face in Shape.Faces.OrderBy(x => x.Index))
            {
                var item = new TreeViewItem()
                {
                    Foreground = Brushes.White,
                    Header = $"[{face.Index}] FACE::Color={face.Color},Points={face.PointIndices.Count()}"
                };
                item.Selected += delegate
                {
                    EDITOR_SelectedFace = face;
                    if (!animating) // rerender the model
                        ShowShape(currentShape);
                };
                BSPTreeView.Items.Add(item);
            }
            HeaderInformationGrid.ItemsSource = new[] { Shape.Header };
        }
        /// <summary>
        /// Populates points in the PointsView Control based on the current frame.
        /// </summary>
        /// <param name="Shape"></param>
        /// <param name="Frame"></param>
        private void PopulatePointsView(BSPShape Shape, int Frame)
        {
            PointsView.Items.Clear();
            foreach(var point in Shape.GetPoints(Frame))
                PointsView.Items.Add(point.ToString());
        }

        private void ShapeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EDITOR_SelectedFace = null;
            if (ShapeSelector.SelectedIndex >= 0)
            {
                var item = (ComboBoxItem)ShapeSelector.SelectedItem;
                if (item == default) return;
                var selectedShape = (BSPShape)item.Tag;
                CurrentFile = FileMap[selectedShape];                
                FrameSelector.SelectionChanged -= FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = -1;
                FrameSelector.ItemsSource = selectedShape.Frames.Select(x => x.Value);
                FrameSelector.SelectionChanged += FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = selectedShape.Frames.Any() ? 0 : -1;
                //if (selectedShape.Frames.Any())
                    ShowShape(selectedShape);
                PopulateBSPTreeView(selectedShape);
                ErrorText.Text = (CurrentFile as BSPFile).ImportErrors.ToString();
            }
        }

        private void FrameSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var shapes = CurrentFile.Shapes;
            if (ShapeSelector.SelectedIndex >= 0 && ShapeSelector.SelectedIndex < shapes.Count)
            {
                var selectedShape = shapes.ElementAtOrDefault(ShapeSelector.SelectedIndex);
                var currentFrame = FrameSelector.SelectedIndex;
                ShowShape(selectedShape, currentFrame);                
            }
        }

        private void CapKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ThreeDViewer.Focus();
        }

        private void ResetCam_Click(object sender, RoutedEventArgs e)
        {
            Camera.LookDirection = new Vector3D(0, 0, 1);
            Camera.Position = new Point3D(0, 0, -200);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            EDITOR_AnimationPaused= false;
            FrameSelector_SelectionChanged(null, null);
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            EDITOR_AnimationPaused = true;
            EndAnimatingFrames();
        }

        private async void ExportSFShapeButton_Click(object sender, RoutedEventArgs e)
        {
            var files = await SHAPEStandard.ExportShapeToSfShape(currentShape);         
            if (MessageBox.Show($"{files.Count()} file(s) were successfully exported to:\n" +
                $"{SHAPEStandard.DefaultShapeExtractionDirectory}\n" +
                $"Do you want to copy its location to the clipboard?", "Complete", 
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Clipboard.SetText(SHAPEStandard.DefaultShapeExtractionDirectory);
        }

        private void ThreeDViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedoLineWork((int)ThreeDViewer.ActualWidth, 
                (int)ThreeDViewer.ActualHeight, (int)(ThreeDViewer.ActualHeight / 20));            
        }

        private void RotButton_Click(object sender, RoutedEventArgs e)
        {
            bool paused = SceneAnimation.GetIsPaused(this);
            if (paused) 
                SceneAnimation.Resume(this);
            else SceneAnimation.Pause(this);
            paused = !paused;
            RotButton.Content = $"Rotation: {(paused ? "OFF" : "ON")}";
        }
    }
}
