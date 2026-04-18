using Microsoft.SolverFoundation.Services;
using Starfox.Editor.Settings;
using StarFox.Interop.BSP;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml.Linq;

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
        /// <summary>
        /// For COLANIM brushes, this is used by the editor to make the color change between each color in the animation
        /// </summary>
        private int materialAnimationFrame = 0;
        /// <summary>
        /// The FPS the animations will play at in the editor
        /// </summary>
        private int EDITOR_AnimationFPS => (int)AppResources.ImportedProject.
            GetSettings<GraphicsUserSettings>(SFCodeProjectSettingsTypes.Graphics).AnimationFPS.Value;
        private bool CanShowTextures = false;

        SFPalette? currentSFPalette;
        COLGroup? currentGroup;
        string modelPaletteName = "NIGHT";
        string? cachedModelPaletteName;

        private Storyboard SceneAnimation;

        private bool EDITOR_AnimationPaused = false;
        private bool EDITOR_ToolboxOpened = true;

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
        /// Opens/Closes the toolbox on the right side. 
        /// <para/><paramref name="EnsureState"/> can be used to manually set the state to ON/OFF.
        /// </summary>
        /// <param name="EnsureState"></param>
        /// <returns>The new state of if it's opened or not</returns>
        public bool ToggleToolbox(bool? EnsureState = default)
        {
            bool stateChanging = EDITOR_ToolboxOpened;
            EDITOR_ToolboxOpened = EnsureState ?? !EDITOR_ToolboxOpened;
            stateChanging = stateChanging != EDITOR_ToolboxOpened;
            //This code effectively just checks to see if we set the state to the same as it already is.
            if (!stateChanging) return EDITOR_ToolboxOpened;

            double DEST_WIDTH = Toolbox.Width; // toolbox has a manual width set, which makes this fine here.
            Thickness DEST_LOC = new Thickness(0);
            if (!EDITOR_ToolboxOpened)
                DEST_LOC.Right = -DEST_WIDTH;
            var anim = new ThicknessAnimation(DEST_LOC, TimeSpan.FromSeconds(.5))
            {
                AccelerationRatio = .7,
                DecelerationRatio = .3
            }; // half second
            Toolbox.BeginAnimation(MarginProperty, anim); // start the animation

            ExpandToolboxButton.Content = EDITOR_ToolboxOpened ? ">" : "<";

            return EDITOR_ToolboxOpened;
        }

        /// <summary>
        /// Tries to create a new palette using the COLTABFile added and a ColorPalettePtr
        /// </summary>
        /// <param name="ColorPaletteName"></param>
        /// <returns></returns>
        private bool CreateSFPalette(string ColorPaletteName)
        {
            if (cachedModelPaletteName != default && cachedModelPaletteName == modelPaletteName) // check if the palette changed
            {
                if (currentGroup != null && currentGroup.Name.ToUpper() == ColorPaletteName.ToUpper()) return true;
            }
            else SHAPEStandard.ClearSFPaletteCache(); // clear cache and re render palettes
            try
            {
                cachedModelPaletteName = modelPaletteName;
                return SHAPEStandard.CreateSFPalette(ColorPaletteName, out currentSFPalette, out currentGroup, modelPaletteName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"While trying to make a SFPalette, this error occured: \n{ex.ToString()}\n" +
                    $"\n\nWant to try viewing with id_0_c for compatibility?", "Palette Parse Procedure Error");
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
                ScaleY = 1,
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
            //animate colors on the shape
            materialAnimationFrame++;
            if (materialAnimationFrame > 999) materialAnimationFrame = 0;            
            
            void Show()
            {
                ShowShape(currentShape, SelectedFrame);
            }
            //for shapes without frames of animation, bail out here
            if (currentShape.Frames.Count > 0)
            {
                SelectedFrame++;
                if (SelectedFrame >= currentShape.Frames.Count)
                    SelectedFrame = 0;
            }
            Dispatcher.Invoke(delegate
            {
                Show();
            });
        }
        /// <summary>
        /// Start animating this object -- for shapes with frames, this is used. This is also used for COLANIMs
        /// </summary>
        /// <param name="shape"></param>
        private void StartAnimationFrames(BSPShape shape)
        {
            animating = true;
            if (animationClock != null)
                EndAnimatingFrames();
            currentShape = shape;
            int milliseconds = (int)(1000.0 / EDITOR_AnimationFPS);
            animationClock = new Timer(ChangeFrame, null, milliseconds, 1000);
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
            //ShapeSelector.SelectedIndex = SelectedShape;
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
        /// Uses the <see cref="SHAPEStandard.GetShapesByHeaderNameOrDefault(string)"/> to attempt to get the 
        /// shape by the name provided and show it in the viewer
        /// </summary>
        /// <param name="ShapeName"></param>
        /// <param name="Frame"></param>
        public async Task<bool> ShowShape(string ShapeName, int Frame = -1)
        {
            ShapeSelector.SelectionChanged -= ShapeSelector_SelectionChanged;
            ShapeSelector.SelectedValue = ShapeName;
            ShapeSelector.SelectionChanged += ShapeSelector_SelectionChanged;
            
            var results = await SHAPEStandard.GetShapesByHeaderNameOrDefault(ShapeName);

            if (!results?.Any() ?? true)
            {
                var defSpr = await FILEStandard.EnsureMSpritesDefinitionOpen();
                if (defSpr != default && defSpr.TryGetSpriteByName(ShapeName, out var Sprite))
                {
                    Clear3DScreen();
                    var model = await SHAPEStandard.Make3DMSpriteGeometry(Sprite);
                    if (model != null)
                        MainSceneGroup.Children.Add(model); // render in viewer
                    return true;
                }
                if (!results?.Any() ?? true)
                    return false;
            }            

            return ShowShape(results.First(), Frame);
        }

        /// <summary>
        /// Invokes the control to draw a model with the current frame
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="Frame"></param>
        private bool ShowShape(BSPShape? shape, int Frame = -1, bool ForceRefreshViews = false)
        {           
            if (!canShowShape) return false; // showing shapes is blocked rn
            if (shape == null) return false; // there is no shape to speak of            
            // our palette hasn't been rendered or we're forced to update it
            if (!CreateSFPalette(shape.Header.ColorPalettePtr))
            {
                if (MessageBox.Show("Want to try viewing model with Color Group: id_0_c for compatibility?",
                    "Compatibility Mode", MessageBoxButton.YesNo) == MessageBoxResult.No) return false;
                shape.Header.ColorPalettePtr = "id_0_c";
                if (!CreateSFPalette(shape.Header.ColorPalettePtr)) return false;
            }
            if (shape.Frames.Count <= 0) Frame = -1;

            bool shapeChanging = currentShape != null ? currentShape != shape : true;

            currentShape = shape;
            
            //Block additional calls to render
            canShowShape = false;
            //Stop animating, please
            EndAnimatingFrames();
            //can we use persistent data?
            Clear3DScreen();

            var group = currentGroup;
            List<GeometryModel3D> models = new();
            try
            {
                if (shapeChanging)
                    CanShowTextures = AppResources.ImportedProject?.GetOptimizerByTypeOrDefault(
                        Starfox.Editor.SFOptimizerTypeSpecifiers.MSprites) != default; // everytime the shape is refreshed, ensure this optimizer exists
                //Use the standard SHAPE library function to render the shape to a MeshGeom
                models = SHAPEStandard.MakeBSPShapeMeshGeometry(
                    shape, in group, in currentSFPalette, Frame, materialAnimationFrame, CanShowTextures, EDITOR_SelectedFace);
            }
            catch (Exception ex)
            {
                //**REALLY OLD ERROR MESSAGE, MAY REMOVE IF EVER NOTICED.
                MessageBox.Show($"Reticulating Splines resulted in: \n" +
                    $"{ex.Message}\nEnding preview.", "Error Occured");
                EndAnimatingFrames();
                canShowShape = true;
                return false;
            }
                                           
            canShowShape = true;

            foreach (var model in models)
                MainSceneGroup.Children.Add(model); // render in viewer

            // start animation on COLANIMs and frames if applicable
            StartAnimationFrames(shape);
            if (shape.Frames.Count > 0)
            {
                FrameSelector.SelectionChanged -= FrameSelector_SelectionChanged;
                FrameSelector.SelectedIndex = Frame;
                FrameSelector.SelectionChanged += FrameSelector_SelectionChanged;
            }

            if (shapeChanging)
                TransitionCameraToLookAtObject(shape); // shape changed: transition camera
            RefreshEditorInfoViews(shape, currentSFPalette, Frame, shapeChanging || ForceRefreshViews); // safely
                                             // refresh views (if exception occurs, is handled)
            return true;
        }

        public bool ShowShape(bool RefreshViews = false) => ShowShape(currentShape, -1, RefreshViews);

        private void TransitionCameraToLookAtObject(BSPShape Shape)
        {
            if (!Shape.Points.Any()) return;
            var ColSize = new Vector3D(Shape.Header.XMax, Shape.Header.YMax, Shape.Header.ZMax);
            ColSize.Y = (-Shape.Points.Select(x => x.Y).Min()) + Shape.Points.Select(x => x.Y).Max();
            ColSize.X = (-Shape.Points.Select(x => x.X).Min()) + Shape.Points.Select(x => x.X).Max();
            ColSize.Z = (-Shape.Points.Select(x => x.Z).Min()) + Shape.Points.Select(x => x.Z).Max();
            var toPos = new Point3D((-ColSize.X * 2), (ColSize.Y / 3), (-ColSize.Z)*2);
            var vecToPos = new Vector3D(0, (ColSize.Y / 2), 0);
            var lookAtDirection = vecToPos - new Vector3D(toPos.X, toPos.Y, toPos.Z);
            lookAtDirection.Normalize();
            CameraTransitionToPoint(toPos, lookAtDirection);
        }

        public void CameraTransitionToPoint(Point3D ToPosition, Vector3D LookAt)
        {
            if (double.IsNaN(LookAt.X) || double.IsNaN(LookAt.Y) || double.IsNaN(LookAt.Z)) return;
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

        /// <summary>
        /// Refreshes the information shown in the Faces, Points, Header, and Palette views on the left
        /// </summary>
        /// <param name="Shape"></param>
        /// <param name="Frame"></param>
        /// <param name="FullReload">Full Reload will refresh the Palettes entries -- something that doesn't change between frames</param>
        private void RefreshEditorInfoViews(BSPShape Shape, SFPalette Palette, int Frame, bool FullReload = false)
        {
            if (Shape == null) return;

            void prompt(Exception Exception, string Message) => AppResources.ShowCrash(Exception, false, Message);

            if (FullReload)
            {
                try
                {
                    PopulatePointsView(Shape, Frame);
                }
                catch (Exception Ex)
                {
                    prompt(Ex, "Populating the points view");
                }
                try
                {
                    PopulateBSPTreeView(Shape);
                }
                catch (Exception Ex)
                {
                    prompt(Ex, "Populating the BSP view");
                }

                try
                {
                    PopulatePaletteView(Shape, Palette);
                }
                catch (Exception Ex)
                {
                    prompt(Ex, "Populating the palettes view");
                }
                ErrorText.Text = CurrentFile?.ImportErrors?.ToString();
            }            
        }

        private void PopulatePaletteView(BSPShape Shape, SFPalette Palette)
        {
            void AddOne(string PaletteName)
            {
                bool loaded = false;
                SHAPEStandard.CreateSFPalette(PaletteName, out var otherPalette, out _);
                loaded = otherPalette != null;

                AddColGroup(PaletteName, otherPalette, loaded);
            }
            void AddColGroup(string PaletteName, SFPalette otherPalette, bool Loaded = true)
            {
                TextBlock block = new TextBlock()
                {
                    Text = PaletteName + (!Loaded ? " - FAILED" : ""),
                    Foreground = Loaded ? Brushes.White : Brushes.Red
                };
                PalettesViewer.Children.Add(block);
                if (Loaded)
                {
                    CopyableImage img = new CopyableImage()
                    {
                        Source = otherPalette.RenderPalette().Convert()
                    };
                    PalettesViewer.Children.Add(img);
                }
                PalettesViewer.Children.Add(new Separator() { Margin = new Thickness(0, 10, 0, 10) });
            }
            async void AddMSprite(string MSpriteName)
            {
                try
                {
                    var (bmp, sprite) = await SHAPEStandard.RenderMSprite(MSpriteName, modelPaletteName);
                    bool Loaded = bmp != default;

                    TextBlock block = new TextBlock()
                    {
                        Text = MSpriteName + (!Loaded ? " - FAILED" : ""),
                        Foreground = Loaded ? Brushes.White : Brushes.Red
                    };
                    PalettesViewer.Children.Add(block);
                    if (Loaded)
                    {
                        CopyableImage img = new CopyableImage()
                        {
                            Source = bmp
                        };
                        PalettesViewer.Children.Add(img);
                    }
                }
                catch (Exception ex)
                {
                    AppResources.ShowCrash(ex, false, "Rendering MSprite: " + MSpriteName);
                }
                PalettesViewer.Children.Add(new Separator() { Margin = new Thickness(0, 10, 0, 10) });
            }

            PalettesViewer.Children.Clear();
            //Add main col group
            AddColGroup(Shape.Header.ColorPalettePtr, Palette);

            foreach (var referencedPalette in Shape.UsingColGroups)
            { // add any color animations                                
                AddOne(referencedPalette);
            }
            foreach (var referencedPalette in Shape.UsingTextures)
            { // add any textures                                
                AddMSprite(referencedPalette);
            }            
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
            try
            {
                foreach (var point in Shape.GetPoints(Frame))
                    PointsView.Items.Add(point.ToString());
            }
            catch { }
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

        /// <summary>
        /// Expands or contracts the toolbox on the right side of the control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpandToolboxButton_Click(object sender, RoutedEventArgs e) => ToggleToolbox();

        private void Render2Clipboard()
        {
            var element = ThreeDViewer;

            var rect = new Rect(element.RenderSize);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                var brush = new VisualBrush(element)
                {
                    Stretch = Stretch.None
                };
                dc.DrawRectangle(brush, null, rect);
            }

            var bitmap = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);            
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (MemoryStream stm = new MemoryStream()) {
                encoder.Save(stm);
                
                var path = System.IO.Path.Combine(Environment.CurrentDirectory, "clipboard.png");
                File.WriteAllBytes(path, stm.ToArray());

                var collection = new System.Collections.Specialized.StringCollection
                {
                    path
                };
                Clipboard.SetFileDropList(collection);
            }
        }

        private void CopyImageButton_Click(object sender, RoutedEventArgs e)
        {
            Render2Clipboard();
        }

        private void PaletteWindowButton_Click(object sender, RoutedEventArgs e)
        {
            PaletteSelectionWindow window = new PaletteSelectionWindow()
            {
                Owner = Application.Current.MainWindow
            };
            window.Closed += delegate
            {
                if (window.SelectedPalette == default) return;
                modelPaletteName = System.IO.Path.GetFileNameWithoutExtension(window.SelectedPalette?.Name) ?? "NIGHT";
                ShowShape(true);
            };
            window.Show();
        }

        private async void ExportMeshButton_Click(object sender, RoutedEventArgs e)
        {
            var result = SHAPEStandard.ExportShapeTo3DMeshFormat(currentShape, currentGroup, currentSFPalette, out string FilePath, SelectedFrame);
            await EDITORStandard.ShowNotification($" {(!result.Successful ? "MODEL WAS NOT EXPORTED! " : "MODEL EXPORTED SUCCESSFULLY! ") + result.Message}",  
                delegate {
                    using var proc = Process.Start("explorer", FilePath);
                }, TimeSpan.FromSeconds(5));
        }
    }
}
