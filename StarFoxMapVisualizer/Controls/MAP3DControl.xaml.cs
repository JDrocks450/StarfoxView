using StarFox.Interop.ASM;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.MAP;
using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
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
using System.Windows.Threading;
using static System.Formats.Asn1.AsnWriter;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for MAP3DControl.xaml
    /// </summary>
    public partial class MAP3DControl : Window
    {
        private static double Editor_ZScrunchPercentage = .25;

        /// <summary>
        /// A basic port of Glider Game Object 3D with basic Positional, Scale, etc. parameters and render code
        /// </summary>
        private class MAPViewerObject3D
        {
            public MAPViewerObject3D(BSPShape shape, Vector3D InitialPosition = default)
            {
                Shape = shape;
                Position = InitialPosition;
                Scale = new Vector3D(shape.XScaleFactor, shape.YScaleFactor, shape.ZScaleFactor);                
            }
            public Vector3D Position { get; set; }
            public Vector3D Scale { get; set; }
            public BSPShape Shape { get; set; }
            public Vector3D ColSize => new Vector3D(Shape.Header.XMax, Shape.Header.YMax, Shape.Header.ZMax);
            /// <summary>
            /// Renders to a Model3DGroup and transforms to the parameters provided
            /// </summary>
            /// <returns></returns>
            public Model3DGroup Render(bool DrawHeightLine = true)
            {
                var modelContent = SHAPEStandard.MakeBSPShapeMeshGeometry(Shape);
                var transform = new Transform3DGroup();
                var scale = new ScaleTransform3D(Scale);
                var fooPos = Position;                
                var translation = new TranslateTransform3D(fooPos);
                transform.Children.Add(scale);
                transform.Children.Add(translation);
                var modelGroup = new Model3DGroup()
                {
                    Transform = transform,
                };
                foreach (var face in modelContent)
                    modelGroup.Children.Add(face);
                if (DrawHeightLine)
                {                    
                    var editorLine1 = new Point3D(0, fooPos.Y, 0);
                    var editorLine2 = new Point3D(0, 0, 0);
                    var linemodel = SHAPEStandard.CreateLine(editorLine1, editorLine2,
                        new DiffuseMaterial()
                        {
                            Brush = Brushes.Yellow
                        });
                    modelGroup.Children.Add(linemodel);
                }
                return modelGroup;
            }
        }

        public MAPFile? SelectedFile { get; set; }
        //STARFOX CONSTANTS
        private ASMFile STRATEQU_Constants { get; set; }
        private int ShipMedSpeed => STRATEQU_Constants["medPspeed"];
        private int TranslateDelayToZDepth(int Delay)
        {
            return (int)(Delay * Editor_ZScrunchPercentage);
            //delay is measured in units travelled for the arwing
            //arwing moves at (ShipMedSpeed * 10) / sec
            double arwingSpeedPerSec = ShipMedSpeed * 10;
            double arwingSpeedPerFrame = (arwingSpeedPerSec / 15.0);
            return (int)(Delay * arwingSpeedPerFrame);
        }
        //--

        //map 3D collections and vars
        private List<MAPViewerObject3D> mapObjects = new();
        private Dictionary<MAPEvent, MAPViewerObject3D> mapToEventMap = new();
        private Dictionary<string,BSPShape> referencedShapes = new();
        //--

        /// <summary>
        /// Creates a new control, optionally with the <see cref="SelectedFile"/> property set
        /// </summary>
        /// <param name="MapFile"></param>
        public MAP3DControl(MAPFile? MapFile = default)
        {
            InitializeComponent();
            SelectedFile = MapFile;
            ScrunchSlider.ValueChanged += ScrunchValueChanged;
        }
        public Task ShowMapContents(MAPFile MapFile)
        {
            SelectedFile = MapFile;
            return ShowMapContents(MapFile);
        }
        /// <summary>
        /// Invalidates this control using the <see cref="SelectedFile"/> property to poll information from
        /// </summary>
        public async Task ShowMapContents()
        {
            //**LOAD CONTENT
            if (!await LoadContent())
                return;
            //**CREATE MAP CONTENT
            CreateMap();
            //**RENDER THE RESULT
            Render();
            await UpdateUI();
        }

        public void CameraTransitionToObject(MAPEvent mapEvent)
        {
            //Try to find the referenced MAPEvent
            if (!mapToEventMap.TryGetValue(mapEvent, out var mapObj)) return;
            var vecToPos = mapObj.Position;
            var toPos = new Point3D(vecToPos.X, vecToPos.Y + 100 + mapObj.ColSize.Y, vecToPos.Z-700);
            var lookAtDirection = vecToPos - new Vector3D(toPos.X, toPos.Y, toPos.Z);
            lookAtDirection.Normalize();
            var positionAnim = new Point3DAnimation(toPos, TimeSpan.FromSeconds(1))
            {
                AccelerationRatio = .5,
                DecelerationRatio = .5,
                FillBehavior = FillBehavior.Stop
            };
            positionAnim.Completed += delegate
            {
                Camera.Position = toPos;
            };
            var lookAnim = new Vector3DAnimation(lookAtDirection, TimeSpan.FromSeconds(1))
            {
                AccelerationRatio = .5,
                DecelerationRatio = .5,
                FillBehavior = FillBehavior.Stop
            };
            lookAnim.Completed += delegate
            {
                Camera.LookDirection = lookAtDirection;
            };
            Camera.BeginAnimation(ProjectionCamera.PositionProperty,positionAnim);
            Camera.BeginAnimation(ProjectionCamera.LookDirectionProperty,lookAnim);
                
        }

        private DispatcherOperation UpdateUI()
        {
            return Dispatcher.InvokeAsync(delegate
            {
                //UPDATE UI
                double TotalWidth = UnitsCanvas.ActualWidth;
                if (Editor_ZScrunchPercentage > 1) // NEGATIVE SCRUNCH
                {
                    var percentage = 1 - (Editor_ZScrunchPercentage - 1);
                    SFUnits.Width = percentage * TotalWidth;
                    EUnits.Width = TotalWidth;
                }
                else
                {
                    var percentage = Editor_ZScrunchPercentage;
                    EUnits.Width = percentage * TotalWidth;
                    SFUnits.Width = TotalWidth;
                }
                ScrunchSlider.Value = Editor_ZScrunchPercentage;
                ScrunchPercentageBlock.Text = ((int)(Editor_ZScrunchPercentage * 100)).ToString();
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void ScrunchValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Editor_ZScrunchPercentage = ScrunchSlider.Value;
            ScrunchPercentageBlock.Text = ((int)(Editor_ZScrunchPercentage * 100)).ToString();
        }

        /// <summary>
        /// Loads the STRATEQU file and tests the constants contained within
        /// </summary>
        /// <returns></returns>
        private async Task<bool> LoadConstants()
        {
            //** INCLUDE STRATEQUs            
            var results = AppResources.ImportedProject.SearchFile("STRATEQU.INC");
            if (!results.Any())
            {
                MessageBox.Show("STRATEQU.INC was not found in the project. This file is needed to " +
                    "create the 3D Map Viewer environment. Please ensure this file is in the project.");
                return false;
            }
            var result = results.First();
            //INCLUDE the file -- if it's already included, it will be loaded from cache
            STRATEQU_Constants = await FILEStandard.IncludeFile<ASMFile>(new FileInfo(result.FilePath));
            if (STRATEQU_Constants == default)
            {
                MessageBox.Show("STRATEQU.INC was found in the project, yet not able to be opened.");
                return false;
            }
            //TEST A CONSTANT TO MAKE SURE THE FILE IS VALID.
            try
            {
                _ = ShipMedSpeed;
            }
            catch
            {
                MessageBox.Show("STRATEQU.INC was found in the project, and opened successfully!\n" +
                    "But, it doesn't have medPspeed as a constant integer so I cannot use it.");
                return false;
            }
            return true;
            //---
        }
        /// <summary>
        /// Loads the content required for this control
        /// </summary>
        /// <returns></returns>
        private async Task<bool> LoadContent()
        {
            if (!await LoadConstants()) return false;
            var assetsReferenced = SelectedFile.LevelData.ShapeEvents;
            foreach(var asset in assetsReferenced)
            {
                var shapeName = asset.ShapeName;
                if (referencedShapes.ContainsKey(shapeName)) continue;
                var shapes = await SHAPEStandard.GetShapesByNameOrDefault(shapeName);
                if (shapes == default || shapes.Count() == 0) 
                    continue;
                var shape = shapes.First();
                referencedShapes.Add(shapeName, shape);
            }
            return true;
        }
        /// <summary>
        /// Creates the map objects on screen
        /// </summary>
        private void CreateMap()
        {
            mapToEventMap.Clear();
            foreach (var node in SelectedFile.LevelData.EventsByDelay)
            {
                var delay = node.Value;
                var nodeData = SelectedFile.LevelData.Events.ElementAtOrDefault(node.Key);
                if (nodeData == null) continue;
                var depth = TranslateDelayToZDepth(delay);
                SpawnEventObject(nodeData, depth);
            }            
        }
        /// <summary>
        /// Renders the control and all map elements contained within
        /// </summary>
        private void Render()
        {
            MainSceneGroup.Children.Clear();
            foreach (var node in mapObjects)
                MainSceneGroup.Children.Add(node.Render());
        }
        /// <summary>
        /// Spawns an event object from the given event data
        /// </summary>
        /// <param name="Evt"></param>
        /// <param name="Depth"></param>
        private void SpawnEventObject(MAPEvent Evt, int Depth)
        {
            string? ShapeName = null;
            Vector3D Location = new Vector3D(0, 0, Depth);
            if (Evt is IMAPShapeEvent shapeDat) // HAS A SHAPE!
                ShapeName = shapeDat.ShapeName;
            if (Evt is IMAPLocationEvent locationDat)
                Location = new Vector3D(locationDat.X, -locationDat.Y, Depth + locationDat.Z);
            if (ShapeName != null && referencedShapes.ContainsKey(ShapeName))
            {
                var asset = referencedShapes[ShapeName];
                var newObject = new MAPViewerObject3D(asset)
                {
                    Position = Location,
                };
                mapObjects.Add(newObject);
                mapToEventMap.Add(Evt, newObject);
            }
        }

        private void AdjustGroundToCam()
        {
            int w = 17000, h = 17000;
            var transformGroup = new Transform3DGroup();            
            transformGroup.Children.Add(new ScaleTransform3D(
                w, 1, h));
            transformGroup.Children.Add(new TranslateTransform3D(
                Camera.Position.X - (w/2), 0, Camera.Position.Z - (h/2)));
            GroundGeom.Transform = transformGroup;
        }

        /// <summary>
        /// Event pump when key is pressed for 3D camera movement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Camera.MoveBy(e.Key, 100).RotateBy(e.Key, 3);
            AdjustGroundToCam();
        }

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
            AdjustGroundToCam();
        }

        private async void ThreeDViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //
        }

        private async void ReloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            await ShowMapContents();
        }
    }
}
