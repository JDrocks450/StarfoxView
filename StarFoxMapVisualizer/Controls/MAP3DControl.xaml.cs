using Starfox.Editor.Settings;
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
        private static double Editor_ZScrunchPercentage = 1;

        private static double Editor_ZFarPlaneRenderDistance => UserSettings.ViewingDistance3D.Value;
        private static GraphicsUserSettings UserSettings => 
            AppResources.ImportedProject.GetSettings<GraphicsUserSettings>(SFCodeProjectSettingsTypes.Graphics);

        /// <summary>
        /// A basic port of Glider Game Object 3D with basic Positional, Scale, etc. parameters and render code
        /// </summary>
        private class MAPViewerObject3D
        {
            public MAPViewerObject3D(BSPShape shape, Vector3D InitialPosition = default)
            {
                AssetName = shape.Header.Name;
                Shape = shape;
                Position = InitialPosition;
                Scale = new Vector3D(shape.XScaleFactor, shape.YScaleFactor, shape.ZScaleFactor);                
            }
            public MAPViewerObject3D(string AssetName, Vector3D InitialPosition = default)
            {
                this.AssetName = AssetName;
                Position = InitialPosition;
                Scale = new Vector3D(16, 16, 1);
            }

            public Vector3D Position { get; set; }
            public Vector3D Rotation { get; set; }
            public Vector3D Scale { get; set; }
            public BSPShape? Shape { get; set; }
            public Vector3D ColSize => new Vector3D(Shape?.Header.XMax ?? 0, Shape?.Header.YMax ?? 0, Shape?.Header.ZMax ?? 0);
            public string AssetName { get; }

            /// <summary>
            /// Renders to a Model3DGroup and transforms to the parameters provided
            /// </summary>
            /// <returns></returns>
            public Model3DGroup Render(bool DrawHeightLine = true)
            {
                List<GeometryModel3D> modelContent = new();
                if (Shape != default)
                    modelContent = SHAPEStandard.MakeBSPShapeMeshGeometry(Shape);
                else
                {
                    //MSprites
                    var defSpr = FILEStandard.EnsureMSpritesDefinitionOpen().Result;
                    if (defSpr != default && defSpr.TryGetSpriteByName(AssetName, out var Sprite))
                    {
                        var mSprite = SHAPEStandard.Make3DMSpriteGeometry(Sprite).Result;
                        if (mSprite == default) throw new Exception();
                        modelContent.Add(mSprite);
                    }
                }
                var transform = new Transform3DGroup();
                var scale = new ScaleTransform3D(Scale);
                var fooPos = Position;                
                var translation = new TranslateTransform3D(fooPos);
                var rotationX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), Rotation.X));
                var rotationY = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), Rotation.Y));
                var rotationZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), Rotation.Z));                
                transform.Children.Add(scale);
                transform.Children.Add(rotationZ);
                transform.Children.Add(rotationY);
                transform.Children.Add(rotationX);
                transform.Children.Add(translation);
                var modelGroup = new Model3DGroup()
                {
                    Transform = transform,
                };
                foreach (var face in modelContent)
                    modelGroup.Children.Add(face);
                if (DrawHeightLine)
                {                    
                    var editorLine1 = new Point3D(0, fooPos.Y / Scale.Y, 0);
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

        public MAPScript? SelectedFile { get; set; }

        //STARFOX CONSTANTS
        private ASMFile STRATEQU_Constants { get; set; }
        private int ShipMedSpeed => STRATEQU_Constants["medPspeed"];
        private double MapZFar = 0;
        private int TranslateDelayToZDepth(int Delay)
        {
            return (int)(Delay);
            //delay is measured in units travelled for the arwing
            //arwing moves at (ShipMedSpeed * 10) / sec
            double arwingSpeedPerSec = ShipMedSpeed * 10;
            double arwingSpeedPerFrame = (arwingSpeedPerSec / 15.0);
            return (int)(Delay * arwingSpeedPerFrame);
        }

        Dictionary<string, int> int_alVar = new();

        //--

        //map 3D collections and vars
        private List<MAPViewerObject3D> mapObjects = new();        
        private Dictionary<MAPEvent, MAPViewerObject3D> mapToEventMap = new();
        private Dictionary<MAPEvent, bool> mapEventsSpawnedChecklist = new();
        private Dictionary<string,BSPShape> referencedShapes = new();
        //--

        /// <summary>
        /// Creates a new control, optionally with the <see cref="SelectedFile"/> property set
        /// </summary>
        /// <param name="MapFile"></param>
        public MAP3DControl(MAPScript? MapFile = default)
        {
            InitializeComponent();
            SelectedFile = MapFile;
            ScrunchSlider.ValueChanged += ScrunchValueChanged;

            //**SETTINGS
            UserSettings.SettingsChanged += delegate
            { // Subscribes to the Settings Changed event
                InvalidateUserSettings();
            };
            InvalidateUserSettings();
        }

        public void InvalidateUserSettings()
        {
            Camera.FarPlaneDistance = Editor_ZFarPlaneRenderDistance;
            Camera.FieldOfView = UserSettings.Scene3DFieldOfView.Value;
        }

        private void ClearMap()
        {
            int_alVar.Clear();
            MainSceneGroup.Children.Clear();
            mapEventsSpawnedChecklist.Clear();
            mapToEventMap.Clear();
            mapObjects.Clear();
        }
        /// <summary>
        /// Invalidates this control using the <see cref="SelectedFile"/> property to poll information from
        /// </summary>
        public async Task ShowMapContents()
        {
            //RESET THE MAP
            ClearMap();
            //**LOAD CONTENT
            if (!await LoadContent())
                return;
            //**CREATE MAP CONTENT (up until wherever the camera is!)
            CreateMap(Camera.Position.Z + Editor_ZFarPlaneRenderDistance);            
            await UpdateUI();
        }

        public void CameraTransitionToPoint(Point3D ToPosition, Vector3D LookAt)
        {
            var toPos = ToPosition;
            var lookAtDirection = LookAt;
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
            Camera.BeginAnimation(ProjectionCamera.PositionProperty, positionAnim);
            Camera.BeginAnimation(ProjectionCamera.LookDirectionProperty, lookAnim);
        }

        public bool CameraTransitionToObject(MAPEvent mapEvent)
        {
            //Try to find the referenced MAPEvent
            if (!mapToEventMap.TryGetValue(mapEvent, out var mapObj))
            {
                CameraTransitionToPoint(new Point3D(0, 100, mapEvent.LevelDelay), new Vector3D(0, 0, 1));
                return false;
            }
            var vecToPos = mapObj.Position;
            var linearAdj = -(10 * mapObj.ColSize.Y);
            var toPos = new Point3D(vecToPos.X, vecToPos.Y + 50 + mapObj.ColSize.Y, vecToPos.Z + linearAdj);
            var lookAtDirection = vecToPos - new Vector3D(toPos.X, toPos.Y, toPos.Z);
            lookAtDirection.Normalize();
            CameraTransitionToPoint(toPos, lookAtDirection);
            _ = this;
            return true;
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
                ScrunchPercentageBlock.Text = ((1 - Editor_ZScrunchPercentage) * 100).ToString("0.#");
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void ScrunchValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Editor_ZScrunchPercentage = ScrunchSlider.Value;
            ScrunchPercentageBlock.Text = ((1 - Editor_ZScrunchPercentage) * 100).ToString("0.#");
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
            STRATEQU_Constants = await FILEStandard.IncludeFile<ASMFile>(new FileInfo(result.FilePath), StarFox.Interop.SFFileType.ASMFileTypes.ASM);
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
            HashSet<string> attempted = new();
            int loaded = 0, eligible = 0;

            //MSprites
            var defSpr = await FILEStandard.EnsureMSpritesDefinitionOpen();

            foreach (var asset in assetsReferenced)
            {
                var shapeName = asset.ShapeName;
                if (referencedShapes.ContainsKey(shapeName)) continue;
                if (attempted.Contains(shapeName)) continue;
                attempted.Add(shapeName);
                eligible++;
                var shapes = await SHAPEStandard.GetShapesByHeaderNameOrDefault(shapeName);
                if (shapes == default || shapes.Count() == 0)
                {
                    if (defSpr != default && defSpr.TryGetSpriteByName(shapeName, out _))
                        loaded++; // MSPRITE!!
                    continue;
                }
                var shape = shapes.First();
                referencedShapes.Add(shapeName, shape);
                loaded++;
            }
            MessageBox.Show($"Loaded {loaded} of {eligible} shapes. ({(double)loaded / eligible:P})");
            return true;
        }
        /// <summary>
        /// Creates the map objects on screen
        /// <para>Will spawn objects up until SpawnZDepthMax then stop</para>
        /// </summary>
        private void CreateMap(double SpawnZDepthMax)
        {
            double currentZDepth = 0;
            double mapFar = 0;
            int runningIndex = -1;
            do
            {
                runningIndex++;
                if (!SelectedFile.LevelData.EventsByDelay.TryGetValue(runningIndex, out var delay))
                    break; // can't load, probably end the end of objects list or one was skipped.                
                var nodeData = SelectedFile.LevelData.Events.ElementAtOrDefault(runningIndex);                
                if (nodeData == null) continue;
                var depth = TranslateDelayToZDepth(delay);
                currentZDepth = depth;
                if (currentZDepth * Editor_ZScrunchPercentage > SpawnZDepthMax) 
                    break;
                if (mapEventsSpawnedChecklist.TryGetValue(nodeData, out var spawned) && spawned)
                    continue; // ALREADY SPAWNED!!!     
                var createdObject = SpawnEventObject(nodeData, (int)depth, out var drawLoc);
                mapFar = Math.Max(mapFar, drawLoc.Z);
                if (createdObject != default)
                    Render(createdObject); // render the new object
            }
            while (currentZDepth * Editor_ZScrunchPercentage < SpawnZDepthMax);
            MapZFar = Math.Max(MapZFar, mapFar);
        }
        /// <summary>
        /// Renders the control and all map elements contained within
        /// </summary>
        private void Render(MAPViewerObject3D node)
        {
            MainSceneGroup.Children.Add(node.Render());
        }
        private void AlSet(string name, int value = 0)
        {
            name = $"al_{name}";
            if (!int_alVar.ContainsKey(name))
                int_alVar.Add(name, value);
            int_alVar[name] = value;
        }
        private void AlInc(string name, int value = 0)
        {
            name = $"al_{name}";
            if (!int_alVar.ContainsKey(name))
                int_alVar.Add(name, value);
            int_alVar[name] += value;
        }
        private int AlGet(string name)
        {
            name = $"al_{name}";
            if (!int_alVar.ContainsKey(name))
                return 0;
            return int_alVar[name];
        }
        private void PerformBasicIStrat(string name, MAPViewerObject3D Object)
        {
            switch (name.ToLower())
            {
                case "rockhard_istrat":
                    goto case "hard180yr_istrat";
                case "hard180yr_istrat":                    
                    AlSet("roty",180);
                    break;
            }
        }
        /// <summary>
        /// Spawns an event object from the given event data
        /// </summary>
        /// <param name="Evt"></param>
        /// <param name="Depth"></param>
        private MAPViewerObject3D? SpawnEventObject(MAPEvent Evt, int Depth, out Vector3D Location)
        {
            Location = new Vector3D(0,0,0);
            if (mapEventsSpawnedChecklist.TryGetValue(Evt, out var spawned) && spawned)
                return default; // uhh, already spawned?
            mapEventsSpawnedChecklist.Add(Evt, true);
            if (Evt is MAPAlVarEvent alVarEvt) // AL_VAR here
            {
                if (!int.TryParse(alVarEvt.Value.Replace("deg",""), out var alValue)) return default;
                AlSet(alVarEvt.Name, alValue);
                return default;
            }
            string? ShapeName = null;
            Location = new Vector3D(0, 0, Depth);
            if (Evt is IMAPShapeEvent shapeDat) // HAS A SHAPE!
                ShapeName = shapeDat.ShapeName;
            if (Evt is IMAPLocationEvent locationDat)
                Location = new Vector3D(locationDat.X, -locationDat.Y, (Depth + locationDat.Z) * Editor_ZScrunchPercentage);
            string? iStrat = default;
            if (Evt is IMAPStrategyEvent strat)
                iStrat = strat.StrategyName;
            if (ShapeName != null)
            {                
                MAPViewerObject3D newObject = default;
                if (referencedShapes.ContainsKey(ShapeName))
                {
                    var asset = referencedShapes[ShapeName];
                    newObject = new MAPViewerObject3D(asset);
                }
                else newObject = new MAPViewerObject3D(ShapeName);
                newObject.Position = Location;
                if (iStrat != default) PerformBasicIStrat(iStrat, newObject);
                //rotational alvars *******
                var rotx = AlGet("rotx");
                var roty = AlGet("roty");
                var rotz = AlGet("rotz");
                newObject.Rotation = new Vector3D(rotx, roty, rotz);
                //reset them
                AlSet("rotx", 0);
                AlSet("roty", 0);
                AlSet("rotz", 0);
                //*************************
                mapObjects.Add(newObject);
                mapToEventMap.Add(Evt, newObject);
                return newObject;
            }
            return default;
        }

        private void CameraMoved(Point3D NewPosition)
        {
            if (true)
                SetPlayfieldGround();
            else AdjustGroundToCam(NewPosition);
            //**CREATE MAP CONTENT (up until wherever the camera is!)
            CreateMap(Camera.Position.Z + Editor_ZFarPlaneRenderDistance);
        }
        /// <summary>
        /// sets the ground for the real-estate afforded by the current level
        /// </summary>
        /// <param name="Position"></param>
        private void SetPlayfieldGround()
        {
            int w = 10000, h = (int)Math.Max(Math.Max(SelectedFile.LevelData.EventsByDelay.Values.Max(), MapZFar), 5000);
            var transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new ScaleTransform3D(
                w, 1, h));
            transformGroup.Children.Add(new TranslateTransform3D(
                -(w/2), 0, 0));
            GroundGeom.Transform = transformGroup;
        }
        /// <summary>
        /// Moves the ground to wherever the camera is
        /// </summary>
        /// <param name="Position"></param>
        private void AdjustGroundToCam(Point3D Position)
        {
            int w = 17000, h = 17000;
            var transformGroup = new Transform3DGroup();            
            transformGroup.Children.Add(new ScaleTransform3D(
                w, 1, h));
            transformGroup.Children.Add(new TranslateTransform3D(
                Position.X - (w/2), 0, Position.Z - (h/2)));
            GroundGeom.Transform = transformGroup;
        }

        /// <summary>
        /// Event pump when key is pressed for 3D camera movement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool fast = e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift);
            Camera.MoveBy(e.Key, fast ? UserSettings.Scene3DFastSpeed : UserSettings.Scene3DSpeed.Value).RotateBy(e.Key, 3);
            CameraMoved(Camera.Position);
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
                CameraMoved(Camera.Position);
            }            
        }

        private async void ThreeDViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //
        }

        private async void ReloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            await ShowMapContents();
        }

        private void CamJumpStartButton_Click(object sender, RoutedEventArgs e) =>
            CameraTransitionToPoint(new Point3D(0, 100, 0), new Vector3D(0, 0, 1));
    }
}
