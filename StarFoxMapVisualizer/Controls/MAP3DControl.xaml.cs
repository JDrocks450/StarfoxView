using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.MAP;
using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Formats.Asn1.AsnWriter;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for MAP3DControl.xaml
    /// </summary>
    public partial class MAP3DControl : Window
    {
        private class MAPViewerObject3D
        {
            public MAPViewerObject3D(BSPShape shape, Vector3D InitialPosition = default)
            {
                Shape = shape;
                Position = InitialPosition;
                Scale = new Vector3D(1, -1, 1);
                if (shape.Points.Any())
                {
                    var largestX = shape.Points.Select(x => x.X).Max();
                    var largestY = shape.Points.Select(x => x.Y).Max();
                    var largestZ = shape.Points.Select(x => x.Z).Max();
                    var modelMaxX = (float)Shape.Header.XMax;
                    var modelMaxY = (float)Shape.Header.YMax;
                    var modelMaxZ = (float)Shape.Header.ZMax;
                    if (modelMaxX != 0 && modelMaxY != 0 && modelMaxZ != 0)
                        Scale = new Vector3D(modelMaxX / largestX, -(modelMaxY / largestY), modelMaxZ / largestZ);
                }
            }
            public Vector3D Position { get; set; }
            public Vector3D Scale { get; set; }
            public BSPShape Shape { get; set; }

            public Model3DGroup Render()
            {
                var modelContent = SHAPEStandard.MakeBSPShapeMeshGeometry(Shape);
                var transform = new Transform3DGroup();
                var scale = new ScaleTransform3D(Scale);
                var translation = new TranslateTransform3D(Position * .25);
                transform.Children.Add(scale);
                transform.Children.Add(translation);
                var modelGroup = new Model3DGroup()
                {
                    Transform = transform,
                };
                foreach (var face in modelContent)
                    modelGroup.Children.Add(face);
                return modelGroup;
            }
        }

        public MAPFile? SelectedFile { get; set; }

        private List<MAPViewerObject3D> mapObjects = new();
        private Dictionary<string,BSPShape> referencedShapes = new();

        /// <summary>
        /// Creates a new control, optionally with the <see cref="SelectedFile"/> property set
        /// </summary>
        /// <param name="MapFile"></param>
        public MAP3DControl(MAPFile? MapFile = default)
        {
            InitializeComponent();
            SelectedFile = MapFile;
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
            await LoadContent();
            //**CREATE MAP CONTENT
            CreateMap();
            //**RENDER THE RESULT
            Render();
        }

        private async Task LoadContent()
        {
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
        }

        private void CreateMap()
        {
            foreach (var node in SelectedFile.LevelData.EventsByDelay)
            {
                var delay = node.Value;
                var nodeData = SelectedFile.LevelData.Events.ElementAtOrDefault(node.Key);
                if (nodeData == null) continue;
                var depth = delay;
                SpawnEventObject(nodeData, depth);
            }
        }

        private void Render()
        {
            MainSceneGroup.Children.Clear();
            foreach (var node in mapObjects)
                MainSceneGroup.Children.Add(node.Render());
        }

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
            }
        }

        /// <summary>
        /// Event pump when key is pressed for 3D camera movement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) =>
            Camera.MoveBy(e.Key, 10).RotateBy(e.Key, 3);

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

        private async void ThreeDViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await ShowMapContents();
        }
    }
}
