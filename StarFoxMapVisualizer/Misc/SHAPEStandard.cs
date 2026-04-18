using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StarFox.Interop.GFX.CAD;
using System.Windows;
using System.IO;
using System.Text.Json;
using System.Windows.Shapes;
using StarFox.Interop.BSP.SHAPE;
using StarFox.Interop.GFX.COLTAB.DEF;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using StarFox.Interop.ASM;
using Starfox.Editor;
using StarFox.Interop.GFX.DAT;
using StarFox.Interop.GFX.DAT.MSPRITES;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StarFox.Interop.BSP;
using StarFoxMapVisualizer.Dialogs;

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for interacting with Shape files into the editor
    /// </summary>
    internal static class SHAPEStandard
    {
        internal const string DefaultMSpritePalette = "NIGHT.COL";
        static Dictionary<string, SFPalette> SFPaletteCache { get; } = new();
        /// <summary>
        /// Gets the Color Table defined in this project
        /// </summary>
        internal static COLTABFile? ProjectColorTable => AppResources.Includes?.OfType<COLTABFile>().FirstOrDefault();
        /// <summary>
        /// The directory to extract models using the <see cref="ExportShapeToSfShape(BSPShape, out IEnumerable{string})"/> function to
        /// <para>Is Export/Shapes by <see langword="default"/></para>
        /// </summary>
        internal static string DefaultShapeExtractionDirectory { get; set; } = System.IO.Path.Combine(Environment.CurrentDirectory, "export/shapes");

        internal static COL? GetPaltByFileName(string FileName)
        {
            FileName = FileName.ToUpper().Replace(".COL", "");
            return AppResources.ImportedProject.Palettes.FirstOrDefault
                (x => System.IO.Path.GetFileNameWithoutExtension(x.Key).ToUpper() == FileName).Value;
        }

        /// <summary>
        /// Tries to create a new palette using the COLTABFile added to the project and a ColorPalettePtr
        /// </summary>
        /// <param name="ColGroupName"></param>
        /// <returns></returns>
        internal static bool CreateSFPalette(string ColGroupName, out SFPalette Palette, out COLGroup Group, string ColorPaletteName = "BLUE")
        {            
            COL? palette = GetPaltByFileName(ColorPaletteName);
            var group = default(COLGroup);
            if (ProjectColorTable != null)
                ProjectColorTable.TryGetGroup(ColGroupName, out group);
            if (palette == null || group == null)
                throw new Exception($"There was a problem loading the palette: {ColorPaletteName} and/or color table group: {ColGroupName}.");
            Group = group;
            if (SFPaletteCache.TryGetValue(ColGroupName, out Palette)) { return true; }            
            SFPalette sfPalette = new SFPalette(ColorPaletteName, in palette, in group);
            sfPalette.GetPalette();
            Palette = sfPalette;            
            SFPaletteCache.Add(ColGroupName, Palette);
            return true;
        }
        /// <summary>
        /// Clears palettes cached for fast access using the <see cref="CreateSFPalette(string, out SFPalette, out COLGroup, string)"/>
        /// function
        /// </summary>
        internal static void ClearSFPaletteCache() => SFPaletteCache.Clear();

        private static List<FXCGXFile> CGXCache = new List<FXCGXFile>();
        private static string? RenderCachePalette;
        private static Dictionary<MSprite, ImageSource> RenderCache = new();
        private static Dictionary<string, MSprite> MSpriteNameMap = new();

        /// <summary>
        /// Prepares for a <see cref="RenderMSprite(MSprite, string)"/> call
        /// </summary>
        /// <param name="PaletteName"></param>
        /// <param name="SpriteDefFileName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        private static async Task<(MSpritesDefinitionFile SpriteDef, 
            COL Palette, IEnumerable<FXCGXFile> CGXs)> BaseRenderMSprite(string PaletteName, string SpriteDefFileName)
        {
            string defAsmName = SpriteDefFileName;
            //Try to load the MSpriteDefinition added to the project
            var mSpriteDef = await FILEStandard.EnsureMSpritesDefinitionOpen(SpriteDefFileName);

            var optimizer = AppResources.ImportedProject?.GetOptimizerByTypeOrDefault(SFOptimizerTypeSpecifiers.MSprites);
            if (optimizer == null) // try to find the MSPrite optimizer
                throw new SFCodeOptimizerNotFoundException(SFOptimizerTypeSpecifiers.MSprites);

            string bankDirectory = optimizer.BaseDirectory;

            //make a list of all banks added to the optimizer separated into its HIGH and LOW counterpart
            string[] banks = optimizer.OptimizerData.ObjectMap.SelectMany(x =>
                new string[] { System.IO.Path.GetFileNameWithoutExtension(x.Key) + "_low.cgx",
                System.IO.Path.GetFileNameWithoutExtension(x.Key) + "_high.cgx" }).ToArray();

            if (CGXCache.Count < banks.Length) // have we loaded every bank?
            { // no, reload them all -- we need all of them loaded for accuracy
                CGXCache.Clear();
                foreach (var bankName in banks)
                { // load all required banks into CGX files
                    string filePath = System.IO.Path.Combine(bankDirectory, bankName);
                    if (!File.Exists(filePath))
                    { // this high/low bank was not found -- try extracting it
                        string BINFileName = filePath // get the original BIN file name
                            .Replace("_low.cgx", ".BIN").Replace("_high.cgx", ".BIN");
                        if (File.Exists(BINFileName)) // check to make sure this BIN file exists
                            await SFGFXInterface.TranslateDATFile(BINFileName); // extract to HIGH and LOW banks
                        else throw new FileNotFoundException("File does not exist. File: " + BINFileName);
                    }
                    if (!File.Exists(filePath)) // after trying extraction, the file still doesn't exist!
                        throw new FileNotFoundException("CGX File does not exist yet was referenced: " + filePath);
                    var bankData = SFGFXInterface.OpenCGX(filePath);
                    if (bankData == null)
                        throw new InvalidDataException("Could not load the HIGH/LOW bank: " + bankName);
                    CGXCache.Add(bankData);
                }
            }
            // attempt to find the palette provided to us (should be P_COL)
            if (!PaletteName.Contains(".COL")) PaletteName = PaletteName + ".COL";
            var colHit = AppResources.ImportedProject.SearchFile(PaletteName).FirstOrDefault();
            if (colHit == default)
                throw new FileNotFoundException($"Could not find {PaletteName}");
            // attempt to load the palette
            var pCol = await FILEStandard.GetPalette(new FileInfo(colHit.FilePath));
            if (pCol == default)
                throw new InvalidDataException($"Palette: {PaletteName} was not found.");
            pCol = COL.TransmutateByRow(pCol, 7, 0);
            return (mSpriteDef, pCol, CGXCache);
        }

        /// <summary>
        /// Returns the given MSpriteName as a Bitmap and also the <see cref="MSprite"/> itself, if loaded.
        /// </summary>
        /// <param name="MSpriteName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        internal static async Task<(ImageSource Image, MSprite Sprite)> RenderMSprite(string MSpriteName, string PaletteName = DefaultMSpritePalette)
        {
            if (MSpriteNameMap.TryGetValue(MSpriteName, out var cachedSprite))
                if (RenderCache.TryGetValue(cachedSprite, out var render)) return (render, cachedSprite);
            var result = await RenderMSpriteBitmap(MSpriteName, PaletteName);
            var newTuple = (result.Image.Convert(), result.Sprite);
            result.Image.Dispose();
            return newTuple;
        }
        /// <summary>
        /// Returns the given MSpriteName as a Bitmap and also the <see cref="MSprite"/> itself, if loaded.
        /// </summary>
        /// <param name="MSpriteName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        internal static async Task<(System.Drawing.Bitmap Image, MSprite Sprite)> RenderMSpriteBitmap(string MSpriteName, string PaletteName = DefaultMSpritePalette)
        {
            if (MSpriteName.EndsWith("_spr")) MSpriteName = MSpriteName.Replace("_spr", "");
            if (RenderCachePalette == null || PaletteName != RenderCachePalette)
                RenderCache.Clear();
            RenderCachePalette = PaletteName;            
            string defAsmName = "DEFSPR.ASM";
            var (mSpriteDef, pCol, cgxs) = await BaseRenderMSprite(PaletteName, defAsmName);
            //Try to render the sprite
            if (mSpriteDef.TryGetSpriteByName(MSpriteName, out MSprite? Sprite) && Sprite != null)
            {
                var bmp = SFGFXInterface.RenderMSprite(Sprite, pCol, cgxs.ToArray());
                var image = bmp;
                MSpriteNameMap.TryAdd(MSpriteName, Sprite);
                RenderCache.TryAdd(Sprite, image.Convert());
                return (image, Sprite);
            }
            throw new FileNotFoundException($"{MSpriteName} was not found in {defAsmName}.");
        }
        /// <summary>
        /// Returns the given MSpriteName as a Bitmap and also the <see cref="MSprite"/> itself, if loaded.
        /// </summary>
        /// <param name="MSpriteName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        internal static async Task<ImageSource> RenderMSprite(MSprite Sprite, string PaletteName = DefaultMSpritePalette)
        {
            if (RenderCachePalette == null || PaletteName != RenderCachePalette)
                RenderCache.Clear();
            RenderCachePalette = PaletteName;
            if (RenderCache.TryGetValue(Sprite, out var render)) return render;
            string defAsmName = "DEFSPR.ASM";
            if (PaletteName == null) PaletteName = DefaultMSpritePalette;
            var (mSpriteDef, pCol, cgxs) = await BaseRenderMSprite(PaletteName, defAsmName);
            //Try to render the sprite
            using (var bmp = SFGFXInterface.RenderMSprite(Sprite, pCol, cgxs.ToArray()))
            {
                var image =  bmp.Convert();
                RenderCache.TryAdd(Sprite, image);
                return image;
            }
        }

        /// <summary>
        /// Will export the given shape to the <see cref="DefaultShapeExtractionDirectory"/> set before invokation
        /// <para>Returns: Any files created using this function, such as a palette or *.sfshape</para>
        /// </summary>
        /// <param name="Shape">The shape to export</param>
        /// <returns></returns>
        internal static async Task<IEnumerable<String>> ExportShapeToSfShape(BSPShape Shape)
        {
            var filesCreated = new List<string>();
            var fileName = System.IO.Path.Combine(DefaultShapeExtractionDirectory, $"{Shape.Header.Name}.sfshape");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
            using (var modelFile = File.Create(fileName))
            {
                using (Utf8JsonWriter writer = new(modelFile))
                    Shape.Serialize(writer);
                filesCreated.Add(fileName);
            }
            var colorPalPtr = Shape.Header.ColorPalettePtr;
            fileName = System.IO.Path.Combine(DefaultShapeExtractionDirectory, $"{colorPalPtr}.sfpal");                
            if (!File.Exists(fileName))
            {
                CreateSFPalette(colorPalPtr, out var sfPal, out _);
                using (var palFile = File.Create(fileName))
                    await sfPal.SerializeColors(palFile);
            }
            filesCreated.Add(fileName);
            return filesCreated;
        }
        /// <summary>
        /// This uses an SFOptimizer in the node that stores Shapes to map ShapeName to the File it appears in.
        /// <para/>This will look up the shape by it's name as it appears in it's header.
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<BSPShape>?> GetShapesByHeaderNameOrDefault(string HeaderName)
        {
            HeaderName = HeaderName.ToUpper();
            var shapeOptim = FILEStandard.GetOptimizerByType(SFOptimizerTypeSpecifiers.Shapes);
            var shapeMap = shapeOptim.OptimizerData.ObjectMap;
            //Try to find the file that contains the shape we want
            if (!shapeMap.TryGetValue(HeaderName, out var FileName)) return default;
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(shapeOptim.FilePath), FileName);
            //Open the file
            var file = await FILEStandard.OpenBSPFile(new FileInfo(path));
            if (file != null && !AppResources.OpenFiles.ContainsKey(path))
                AppResources.OpenFiles.Add(path, file); // Cache it for later, if needed
            //FIND all shapes whose name matches the provided parameter
            var hits = file.Shapes.Where(x => x.Header.Name.ToLower() == HeaderName.ToLower());
            //Find all shapes that don't point to another shape -- as in they're blank
            var hitsWithoutDataPointer = hits.Where(x => !x.Header.HasDataPointer);
            //If there are none, then pick the first one that does have a data pointer
            //and load the shape it points to
            if (!hitsWithoutDataPointer.Any())
                return file.Shapes.Where(x => x.Header.Name.ToLower() ==
                hits.Where(x => x.Header.HasDataPointer).First().Header.DataPointer);
            else return hitsWithoutDataPointer; // else, return all shapes that don't point anywhere else
        }
        public static GeometryModel3D CreateLine(Point3D Point1, Point3D Point2, Material Material)
        {
            var lineMeshGeom = new MeshGeometry3D();
            PushLine(ref lineMeshGeom, Point1, Point2);
            return new GeometryModel3D(lineMeshGeom, Material);
        }
        public static bool PushLine(ref MeshGeometry3D geometry, Point3D Point1, Point3D Point2)
        {
            double thickness = .5;
            int index = geometry.Positions.Count(); // used to push indices
            geometry.Positions.Add(new(Point1.X, Point1.Y, Point1.Z)); // i
            geometry.Positions.Add(new(Point1.X - thickness, Point1.Y, Point1.Z + thickness)); // i + 1            
            geometry.Positions.Add(new(Point2.X, Point2.Y, Point2.Z)); // i + 2
            geometry.Positions.Add(new(Point2.X + thickness, Point2.Y, Point2.Z - thickness)); // i + 3
            geometry.TriangleIndices.Add(index);
            geometry.TriangleIndices.Add(index + 1);
            geometry.TriangleIndices.Add(index + 2);
            geometry.TriangleIndices.Add(index);
            geometry.TriangleIndices.Add(index + 3);
            geometry.TriangleIndices.Add(index + 2);
            //RECAP: We made a rectangle that looks like a line. It has some depth to be viewable at oblique angles
            return true;
        }
        /// <summary>
        /// Puts a point into the specified Geometry
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="Shape"></param>
        /// <param name="Face"></param>
        /// <param name="Frame"></param>
        /// <returns></returns>
        public static bool PushLine(ref MeshGeometry3D geometry, BSPShape Shape, in BSPFace Face, int Frame)
        { // Pushes a line to the mesh geometry provided to this function
            var ModelPoints = Face.PointIndices.Select(x => Shape.GetPointOrDefault(x.PointIndex, Frame)).Where(y => y != default).ToArray();
            if (ModelPoints.Length != 2) return false; // not a line!!                        
            return PushLine(ref geometry, new Point3D(ModelPoints[0].X, ModelPoints[0].Y, ModelPoints[0].Z),
                new Point3D(ModelPoints[1].X, ModelPoints[1].Y, ModelPoints[1].Z));
        }
        /// <summary>
        /// Turns a <see cref="BSPShape"/> into a GeometryModel3D collection which makes up the supplied model
        /// </summary>
        /// <param name="Shape">The supplied shape to render</param>
        /// <param name="ColorPalettePtr">The name of the ColorPalettePtr property on this shape</param>
        /// <param name="Frame">The frame of animation to use as the rendered frame</param>
        /// <returns></returns>
        public static List<GeometryModel3D> MakeBSPShapeMeshGeometry(
            BSPShape Shape, int Frame = -1, int MaterialAnimationFrame = -1, bool HandleColGroupErrors = true)
        {
            bool TexturesActivated = AppResources.ImportedProject?.
                GetOptimizerByTypeOrDefault(SFOptimizerTypeSpecifiers.MSprites) != default;
            try
            {
                CreateSFPalette(Shape.Header.ColorPalettePtr, out var palette, out var group);
                return MakeBSPShapeMeshGeometry(Shape, in group, in palette, Frame, MaterialAnimationFrame, TexturesActivated);
            }
            catch (Exception ex)
            {
                if (HandleColGroupErrors)
                {
                    CreateSFPalette("id_0_c", out var palette, out var group);
                    return MakeBSPShapeMeshGeometry(Shape, in group, in palette, Frame, MaterialAnimationFrame, TexturesActivated);
                }
                else throw ex;
            }            
        }

        public static GeometryModel3D Make3DTexturedPlaneGeometry(ImageSource image)
        {
            var material = GetNearestNeighborTextureMaterial(image);
            Geometry3D geom = new MeshGeometry3D()
            {
                Positions = new Point3DCollection
                {
                    new Point3D(0, 0, 0),                    
                    new Point3D(1, 0, 0),
                    new Point3D(0, 1, 0),
                    new Point3D(1, 1, 0),
                },
                TriangleIndices = new Int32Collection { 0, 1, 3, 0, 2, 3 },
                TextureCoordinates = new PointCollection
                {
                    new Point(1, 0),
                    new Point(0, 0),
                    new Point(1, 1),                     
                    new Point(0, 1),
                    new Point(0, 0),
                    new Point(1, 1)
                }
            };
            //Make the model that uses this geom we made
            GeometryModel3D model = new()
            {
                Material = material, // front-face color
                BackMaterial = material, // back-face color (CullMode None)                    
                Geometry = geom,
                Transform = new ScaleTransform3D(10,10,10)
            };
            return model;
        }

        public static async Task<GeometryModel3D?> Make3DMSpriteGeometry(string MSpriteName, string? PaletteName = default)
        {
            var mSprite = await RenderMSprite(MSpriteName, PaletteName);
            if (mSprite == default) return default;
            return Make3DTexturedPlaneGeometry(mSprite.Image);
        }
        public static async Task<GeometryModel3D?> Make3DMSpriteGeometry(MSprite MSprite, string? PaletteName = default)
        {
            var mSprite = await RenderMSprite(MSprite, PaletteName);
            if (mSprite == default) return default;
            return Make3DTexturedPlaneGeometry(mSprite);
        }

        /// <summary>
        /// Turns a <see cref="BSPShape"/> into a GeometryModel3D collection which makes up the supplied model
        /// </summary>
        /// <param name="Shape">The shape to draw</param>
        /// <param name="Group">The COLGroup instance to use to find color data</param>
        /// <param name="Palette">The palette that contains the colors to use</param>
        /// <param name="Frame">The frame of Animation to render</param>
        /// <param name="HighlightFace">Optionally, a face to highlight over all others. The remaining ones will be semi-opaque.</param>
        /// <returns></returns>
        public static List<GeometryModel3D> MakeBSPShapeMeshGeometry(
            BSPShape Shape, in COLGroup Group, in SFPalette Palette, int Frame, int MaterialAnimationFrame,
            bool TexturesActivated = false, BSPFace? HighlightFace = default, Brush? HighlightColor = default)            
        {
            //SET VARS
            var models = new List<GeometryModel3D>();
            var shape = Shape;
            var group = Group;
            var currentSFPalette = Palette;
            var EDITOR_SelectedFace = HighlightFace;
            HighlightColor = HighlightColor ?? Brushes.Yellow;
            //---

            Color GetColor(COLDefinition.CallTypes Type, int colIndex, SFPalette palette)
            { // Get a color for a COLDefinition from the sfPalette
                var fooColor = System.Drawing.Color.Blue;
                switch (Type)
                {
                    case COLDefinition.CallTypes.Collite: // diffuse                    
                        fooColor = palette.Collites[colIndex];
                        break;
                    case COLDefinition.CallTypes.Colnorm: // normal? not sure.
                        fooColor = palette.Colnorms[colIndex];
                        break;
                    case COLDefinition.CallTypes.Colsmooth: // not implemented
                    case COLDefinition.CallTypes.Coldepth: // No reaction to angle
                        fooColor = palette.Coldepths.ElementAtOrDefault(colIndex).Value;
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
            foreach (var face in shape.Faces)
            { // find all faces
                MeshGeometry3D geom = new(); // create geometry
                Material material = new DiffuseMaterial()
                {
                    Brush = new SolidColorBrush(Colors.Blue),
                }; // basic material in case of errors
                var definition = group.Definitions.ElementAtOrDefault(face.Color); // find the color definition for this face
                double drawingOpacity = 1;
                bool hasTextureProperties = false;
                if (definition != default) // did we find it?
                {
                    int colIndex = 0;
                    Color? color = default; // default color
                    switch (definition.CallType)
                    { // depending on call type we handle this material differently
                        case COLDefinition.CallTypes.Collite: // diffuse
                        case COLDefinition.CallTypes.Coldepth: // emissive, kinda
                        case COLDefinition.CallTypes.Colnorm: // normal? not sure.
                        case COLDefinition.CallTypes.Colsmooth: // not implemented
                            { // default, push the color to the model
                                if (definition is COLTexture)
                                    goto case COLDefinition.CallTypes.Texture;
                                colIndex = ((ICOLColorIndexDefinition)definition).ColorByte;
                                color = GetColor(definition.CallType, colIndex, currentSFPalette);
                            }
                            break;
                        case COLDefinition.CallTypes.Animation: // color animation
                            {
                                var animDef = definition as COLAnimationReference; // find anim definition
                                //attempt to make a palette for this animation
                                if (!CreateSFPalette(animDef.TableName, out var animSFPal, out var animGroup)) break;
                                shape.UsingColGroups.Add(animDef.TableName);
                                int index = MaterialAnimationFrame > -1 ? MaterialAnimationFrame % 
                                    animGroup.Definitions.Count : 0; // adjust color based on MatAnimFrame parameter
                                var animMemberDefinition = animGroup.Definitions.ElementAt(index); // jump to color
                                color = GetColor(animMemberDefinition.CallType, // finally get the color from the animPalette
                                    ((ICOLColorIndexDefinition)animMemberDefinition).ColorByte,
                                    animSFPal);
                            }
                            break;
                        case COLDefinition.CallTypes.Texture:
                            {
                                if (!TexturesActivated) break; // textures disabled
                                var textDef = definition as COLTexture;
                                try
                                {
                                    var (bmp, sprite) = RenderMSprite(textDef.Reference, Palette.Name).Result;                                
                                    material = GetNearestNeighborTextureMaterial(bmp);
                                    shape.UsingTextures.Add(textDef.Reference);
                                    hasTextureProperties = true;
                                }
                                catch { } // This is caught later in the SHAPEControl                                
                            }
                            break;
                    }
                    if (color != default)
                    {
                        material = new DiffuseMaterial()
                        {
                            Brush = new SolidColorBrush(color ?? Colors.Blue),
                        }; // create the material, all of them use Diffuse in editor
                    }
                }
                //Do we have a selected face to highlight?
                if (EDITOR_SelectedFace != default)
                {
                    drawingOpacity = .5; // we do, make all of them semi-opaque
                    (material as DiffuseMaterial).Brush.Opacity = drawingOpacity; // current material is set to this opacity
                    if (EDITOR_SelectedFace == face)
                    { // oops, this material is the one we want to highlight!
                        material = new EmissiveMaterial()
                        {
                            Brush = HighlightColor, // make it stand out
                        };
                        drawingOpacity = 1; // put the opacity back!
                    }
                }
                //Make the model that uses this geom we made
                GeometryModel3D model = new()
                {
                    Material = material, // front-face color
                    BackMaterial = material, // back-face color (CullMode None)                    
                    Geometry = geom,
                };
                models.Add(model);
                var remainder = face.PointIndices.Count() % 3; // used for debugging, check to make sure this is a TRI
                var vector3 = new Vector3D()
                {
                    X = face.Normal.X,
                    Y = face.Normal.Y,
                    Z = face.Normal.Z
                }; // calculate the normal
                vector3.Normalize(); // normalize the vector is important considering Starfox is all integral numbers
                geom.Normals.Add(vector3);
                if (face.PointIndices.Count() < 3) // STRAY! ( a line )
                {
                    PushLine(ref geom, shape, in face, Frame); // push a line to the geom
                    continue;
                }
                var orderedIndicies = face.PointIndices.OrderBy(x => x.Position).ToArray();
                for (int i = 0; i < face.PointIndices.Count(); i++)
                {
                    var pointRefd = orderedIndicies[i]; // get the PointReference
                    var point = shape.GetPointOrDefault(pointRefd.PointIndex, Frame); // find the referenced point itself
                    if (point == null) break; // shit, we didn't find it.
                    geom.Positions.Add(new Point3D(point.X, point.Y, point.Z)); // sweet found it, push it to our Vertex Buffer                    
                    geom.TriangleIndices.Add(i); // add the index
                }
                if (hasTextureProperties)
                {                                      
                    geom.TextureCoordinates.Add(new Point(0, 1));                 
                    geom.TextureCoordinates.Add(new Point(0, 0));   
                    geom.TextureCoordinates.Add(new Point(1, 0)); 
                    geom.TextureCoordinates.Add(new Point(1, 1)); 
                    geom.TextureCoordinates.Add(new Point(0, 1));
                    geom.TextureCoordinates.Add(new Point(1, 0));                     
                }
            }
            return models;
        }

        private static Material GetNearestNeighborTextureMaterial(ImageSource bmp)
        {
            var image = new Image() { Source = bmp };
            RenderOptions.SetCachingHint(image, CachingHint.Cache);
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            return new DiffuseMaterial()
            {
                Brush = new VisualBrush(image)
            };
        }

        internal static BSPExporter.BSPIOWriteResult ExportShapeTo3DMeshFormat(BSPShape? currentShape, COLGroup Group, SFPalette Palette, out string? FilePath, int Frame = 0)
        {
            SaveFileDialog saveDialog = new()
            {
                Title = "Save 3D Object File",
                AddExtension = true,
                Filter = BSPExporter.FILE_EXTENSION.ToUpper() + " Files|*" + BSPExporter.FILE_EXTENSION,
                CheckPathExists = true,
                FileName = currentShape.Header.Name,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };
            FilePath = null;
            if (!saveDialog.ShowDialog() ?? true)
                return BSPExporter.BSPIOWriteResult.Cancelled;
            FilePath = saveDialog.FileName;
            BSPExporter.BSPExportOptions options = BSPExporter.BSPExportOptions.Default;
            try
            { // try invoking the BSP exporter
                return BSPExporter.ExportShape(saveDialog.FileName, currentShape, Group, Palette, Frame, 
                    ProjectColorTable, GetPaltByFileName("BLUE.COL"), options);
            }
            catch (Exception ex)
            { // an error has occurred
                return BSPExporter.BSPIOWriteResult.Faulted(ex);
            }
        }
    }
}
