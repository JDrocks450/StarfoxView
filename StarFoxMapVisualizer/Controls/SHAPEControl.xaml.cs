using StarFox.Interop.BSP;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX.COL;
using StarFox.Interop.MAP;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private BSPShape? animatingShape;
        private BSPFile CurrentFile;
        private Timer? animationClock;
        private bool animating = false;
        private bool canShowShape = true;

        private COLTABFile? ColTabs => AppResources.Includes?.OfType<COLTABFile>().FirstOrDefault();

        public SHAPEControl()
        {
            InitializeComponent();
            RedoLineWork();

            var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            var transform = new RotateTransform3D()
            {
                Rotation = rotation
            };
            rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(30))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });
            MainSceneGroup.Transform = transform;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) =>
            Camera.MoveBy(e.Key,10).RotateBy(e.Key,3);

        Point from;
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

        public void ChangeFrame(object? state)
        {
            void Show()
            {
                ShowShape(animatingShape, SelectedFrame);
            }
            SelectedFrame++;
            if (SelectedFrame >= animatingShape.Frames.Count)
                SelectedFrame = 0;
            Dispatcher.Invoke(delegate
            {
                Show();
            });
        }

        private void StartAnimationFrames(BSPShape shape)
        {
            if (shape.Frames.Count <= 0) return;
            animating = true;
            if (animationClock != null)
                EndAnimatingFrames();
            animatingShape = shape;
            animationClock = new Timer(ChangeFrame, null, 1000 / 30, 1000);
        }

        private void EndAnimatingFrames()
        {
            animating = false;
            animationClock?.Dispose();
            animationClock = null;
        }

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
            if (ColTabs == default) {
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
            foreach (var file in AppResources.OpenFiles.OfType<BSPFile>())
                OpenFile(file);
        }

        private void OpenFile(BSPFile file)
        {
            CurrentFile = file;
            ShapeSelector.SelectionChanged -= ShapeSelector_SelectionChanged;
            ShapeSelector.ItemsSource = file.Shapes.Select(x => x.Header.Name);
            ShapeSelector.SelectionChanged += ShapeSelector_SelectionChanged;
            ShapeSelector.SelectedIndex = SelectedShape;                    
        }

        private void ShowShape(BSPShape? shape, int Frame = -1)
        {           
            if (!canShowShape) return;
            if (shape == null) return;
            
            var colors = new System.Drawing.Color[0];
            Color GetColor(int colIndex)
            {
                if (colIndex >= colors.Length)
                    ;
                var fooColor = colors.ElementAtOrDefault(colIndex);
                return new System.Windows.Media.Color()
                {
                    A = fooColor.A,
                    B = fooColor.B,
                    G = fooColor.G,
                    R = fooColor.R,
                };
            }
            COL? palette = AppResources.ImportedProject.Palettes.Values.FirstOrDefault();
            if (palette != null)            
                colors = palette.GetPalette(0,0);                  
            var colTable = AppResources.Includes.OfType<COLTABFile>().FirstOrDefault();
            var group = default(COLGroup);
            if (colTable != null)
                colTable.TryGetGroup(shape.Header.ColorPalettePtr, out group);                
            if(palette == null || group == null)
            {
                MessageBox.Show("The palette or the color table were not imported correctly. Execution cannot proceed.");
                return;
            }

            canShowShape = false;

            EndAnimatingFrames();

            MainSceneGroup.Children.Clear();
            MainSceneGroup.Children.Add(MainLight);

            var models = new List<GeometryModel3D>();

            //foreach (BSPPoint point in shape.GetPoints(Frame).OrderBy(x => x.ActualIndex))
            //points.Add(new Point3D(point.X, point.Y, point.Z));
            foreach (var face in shape.Faces)
            {
                if (face.PointIndices.Count() < 3) // this is a line                
                    continue;
                MeshGeometry3D geom = new();                
                Material material = new DiffuseMaterial()
                {
                    Brush = new SolidColorBrush(Colors.Blue),
                };
                if (palette!= null)
                {
                    var definition = group.Definitions.ElementAtOrDefault(face.Color);
                    if (definition != default)
                    {
                        int colIndex = 0;
                        switch (definition.CallType)
                        {
                            case COLDefinition.CallTypes.Collite:
                                {
                                    colIndex = ((COLLite)definition).ColorByte;
                                    var color = GetColor(colIndex);
                                    material = new DiffuseMaterial()
                                    {
                                        Brush = new SolidColorBrush(color),
                                        Color = color
                                    };
                                }
                                break;
                            case COLDefinition.CallTypes.Coldepth:
                                {
                                    colIndex = ((COLDepth)definition).ColorByte;
                                    var color = GetColor(colIndex);
                                    material = new EmissiveMaterial()
                                    {
                                        Brush = new SolidColorBrush(color),
                                        Color = color
                                    };
                                }
                                break;
                            case COLDefinition.CallTypes.Colnorm:
                                {
                                    colIndex = ((COLNorm)definition).ColorByte;
                                    var color = GetColor(colIndex);
                                    material = new DiffuseMaterial()
                                    {
                                        Brush = new SolidColorBrush(color),
                                        Color = color
                                    };
                                }
                                break;
                            case COLDefinition.CallTypes.Colsmooth:
                                {
                                    colIndex = ((COLSmooth)definition).ColorByte;
                                    var color = GetColor(colIndex);
                                    material = new EmissiveMaterial()
                                    {
                                        Brush = new SolidColorBrush(color),
                                        Color = color
                                    };
                                }
                                break;
                        }                        
                    }
                }
                GeometryModel3D model = new()
                {
                    Material = material,
                    BackMaterial = material,
                    Geometry = geom
                };
                models.Add(model);
                var remainder = face.PointIndices.Count() % 3;
                if (remainder != 0)
                    ;
                var vector3 = new Vector3D()
                {
                    X = face.Normal.X,
                    Y = face.Normal.Y,
                    Z = face.Normal.Z
                };
                vector3.Normalize(); // normalize the vector is important considering Starfox is all integral numbers
                geom.Normals.Add(vector3);
                var orderedIndicies = face.PointIndices.OrderBy(x => x.Position).ToArray();
                for (int i = 0; i < face.PointIndices.Count(); i++)
                {
                    var pointRefd = orderedIndicies[i];
                    var point = shape.GetPoint(pointRefd.PointIndex, Frame);
                    geom.Positions.Add(new Point3D(point.X, point.Y, point.Z));
                    geom.TriangleIndices.Add(i);
                }
            }
            if (shape.Frames.Count > 0)
            {
                StartAnimationFrames(shape);
                FrameSelector.SelectionChanged -= FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = Frame;
                FrameSelector.SelectionChanged += FrameSelector_SelectionChanged;
            }
            canShowShape = true;
            foreach (var model in models)
                MainSceneGroup.Children.Add(model);
        }

        private void ShapeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var shapes = CurrentFile.Shapes;                        
            if (ShapeSelector.SelectedIndex > 0 && ShapeSelector.SelectedIndex < shapes.Count)
            {
                var selectedShape = shapes.ElementAtOrDefault(ShapeSelector.SelectedIndex);
                FrameSelector.SelectionChanged -= FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = -1;
                FrameSelector.ItemsSource = selectedShape.Frames.Select(x => x.Value.Name);
                FrameSelector.SelectionChanged += FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = selectedShape.Frames.Any() ? 0 : -1;
                if (!selectedShape.Frames.Any())
                    ShowShape(selectedShape);
            }
        }

        private void FrameSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var shapes = CurrentFile.Shapes;
            if (ShapeSelector.SelectedIndex > 0 && ShapeSelector.SelectedIndex < shapes.Count)
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
    }
}
