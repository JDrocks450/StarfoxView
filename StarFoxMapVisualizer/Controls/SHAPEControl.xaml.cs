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

        private bool EDITOR_AnimationPaused = false;

        public SHAPEControl()
        {
            InitializeComponent();
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
            if (currentGroup != null && currentGroup.Name == ColorPaletteName) return true;            
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
            var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
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
            rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(30))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });
            MainSceneGroup.Transform = transformGroup;
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
            animationClock = new Timer(ChangeFrame, null, 1000 / 30, 1000);
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
        private void RedoLineWork()
        {
            int MX_X = 1000, MX_Y = 1000, STEP = 50;
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
            foreach (var file in AppResources.OpenFiles.Values.OfType<BSPFile>())
                OpenFile(file);
        }

        /// <summary>
        /// Opens a file to view the models inside
        /// </summary>
        /// <param name="file"></param>
        private void OpenFile(BSPFile file)
        {
            CurrentFile = file;
            ShapeSelector.SelectionChanged -= ShapeSelector_SelectionChanged;
            ShapeSelector.ItemsSource = file.Shapes.Select(x => x.Header.Name);
            ShapeSelector.SelectedIndex = -1;
            ShapeSelector.SelectionChanged += ShapeSelector_SelectionChanged;
            ShapeSelector.SelectedIndex = SelectedShape;                    
        }

        private void Clear3DScreen()
        {
            MainSceneGroup.Children.Clear();
            //MainSceneGroup.Children.Add(MainLight);
        }

        private bool PushLine(ref MeshGeometry3D geometry, BSPShape Shape, in BSPFace Face, int Frame)
        {
            var ModelPoints = Face.PointIndices.Select(x => Shape.GetPointOrDefault(x.PointIndex, Frame)).Where(y => y != default).ToArray();
            if (ModelPoints.Length != 2) return false; // not a line!!
            int index = geometry.Positions.Count();
            geometry.Positions.Add(new(ModelPoints[0].X, ModelPoints[0].Y, ModelPoints[0].Z)); // i
            geometry.Positions.Add(new(ModelPoints[0].X - 1, ModelPoints[0].Y, ModelPoints[0].Z + 1)); // i + 1            
            geometry.Positions.Add(new(ModelPoints[1].X, ModelPoints[1].Y, ModelPoints[1].Z)); // i + 2
            geometry.Positions.Add(new(ModelPoints[1].X + 1, ModelPoints[1].Y, ModelPoints[1].Z - 1)); // i + 3
            geometry.TriangleIndices.Add(index);
            geometry.TriangleIndices.Add(index + 1);
            geometry.TriangleIndices.Add(index + 2);
            geometry.TriangleIndices.Add(index);
            geometry.TriangleIndices.Add(index + 3);
            geometry.TriangleIndices.Add(index + 2);
            return true;
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

            Color GetColor(COLDefinition.CallTypes Type, int colIndex)
            { // Get a color for a COLDefinition from the sfPalette
                var fooColor = System.Drawing.Color.Pink;
                switch (Type)
                {
                    case COLDefinition.CallTypes.Collite:
                        fooColor = currentSFPalette.Collites[colIndex];
                        break;
                    case COLDefinition.CallTypes.Coldepth:
                        fooColor = currentSFPalette.Coldepths.ElementAtOrDefault(colIndex).Value;
                        break;
                }
                return new System.Windows.Media.Color() //to media color
                {
                    A = 255,
                    B = fooColor.B,
                    G = fooColor.G,
                    R = fooColor.R,
                };
            }
            //Block additional calls to render
            canShowShape = false;
            //Stop animating, please
            EndAnimatingFrames();
            //can we use persistant data?
            Clear3DScreen();

            var group = currentGroup;
            var models = new List<GeometryModel3D>();

            //foreach (BSPPoint point in shape.GetPoints(Frame).OrderBy(x => x.ActualIndex))
            //points.Add(new Point3D(point.X, point.Y, point.Z));
            foreach (var face in shape.Faces)
            {                
                MeshGeometry3D geom = new();                
                Material material = new DiffuseMaterial()
                {
                    Brush = new SolidColorBrush(Colors.Blue),
                };
                var definition = group.Definitions.ElementAtOrDefault(face.Color);
                double _Opacity = 1;
                if (definition != default)
                {
                    int colIndex = 0;                    
                    switch (definition.CallType)
                    {
                        case COLDefinition.CallTypes.Collite:
                        case COLDefinition.CallTypes.Coldepth:
                        case COLDefinition.CallTypes.Colnorm:
                        case COLDefinition.CallTypes.Colsmooth:
                            {
                                colIndex = ((ICOLColorIndexDefinition)definition).ColorByte;
                                var color = GetColor(definition.CallType, colIndex);
                                material = new DiffuseMaterial()
                                {
                                    Brush = new SolidColorBrush(color),
                                };
                            }
                            break;
                    }
                }                
                if (EDITOR_SelectedFace != default)
                {
                    _Opacity = .5;
                    (material as DiffuseMaterial).Brush.Opacity = _Opacity;
                    if (EDITOR_SelectedFace == face)
                    {
                        material = new EmissiveMaterial()
                        {
                            Brush = Brushes.Yellow,
                        };
                        _Opacity = 1;
                    }
                }                
                GeometryModel3D model = new()
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = geom,                    
                };
                models.Add(model);
                var remainder = face.PointIndices.Count() % 3;
                var vector3 = new Vector3D()
                {
                    X = face.Normal.X,
                    Y = face.Normal.Y,
                    Z = face.Normal.Z
                };
                vector3.Normalize(); // normalize the vector is important considering Starfox is all integral numbers
                geom.Normals.Add(vector3);
                if (face.PointIndices.Count() < 3) // STRAY!
                {
                    PushLine(ref geom, shape, in face, Frame);
                    continue;
                }
                var orderedIndicies = face.PointIndices.OrderBy(x => x.Position).ToArray();
                for (int i = 0; i < face.PointIndices.Count(); i++)
                {
                    var pointRefd = orderedIndicies[i];
                    try
                    {
                        var point = shape.GetPointOrDefault(pointRefd.PointIndex, Frame);
                        if (point == null) break;
                        geom.Positions.Add(new Point3D(point.X, point.Y, point.Z));
                        geom.TriangleIndices.Add(i);
                    }
                    catch(Exception ex)
                    {                        
                        MessageBox.Show($"Reticulating Splines resulted in: \n" +
                            $"{ex.Message}\nEnding preview.", "Error Occured");
                        EndAnimatingFrames();
                        canShowShape = true;
                        return;
                    }
                }
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
            var shapes = CurrentFile.Shapes;
            EDITOR_SelectedFace = null;
            if (ShapeSelector.SelectedIndex >= 0 && ShapeSelector.SelectedIndex < shapes.Count)
            {
                var selectedShape = shapes.ElementAtOrDefault(ShapeSelector.SelectedIndex);
                FrameSelector.SelectionChanged -= FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = -1;
                FrameSelector.ItemsSource = selectedShape.Frames.Select(x => x.Value);
                FrameSelector.SelectionChanged += FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = selectedShape.Frames.Any() ? 0 : -1;
                if (!selectedShape.Frames.Any())
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
    }
}
