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

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for interacting with Shape files into the editor
    /// </summary>
    internal static class SHAPEStandard
    {
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
                            
        /// <summary>
        /// Tries to create a new palette using the COLTABFile added to the project and a ColorPalettePtr
        /// </summary>
        /// <param name="ColorPaletteName"></param>
        /// <returns></returns>
        internal static bool CreateSFPalette(string ColorPaletteName, out SFPalette Palette, out COLGroup Group)
        {
            COL? palette =
                AppResources.ImportedProject.Palettes.FirstOrDefault
                (x => System.IO.Path.GetFileNameWithoutExtension(x.Key).ToUpper() == "NIGHT").Value;
            var group = default(COLGroup);
            if (ProjectColorTable != null)
                ProjectColorTable.TryGetGroup(ColorPaletteName, out group);
            if (palette == null || group == null)
                throw new Exception("There was a problem loading the palette and/or color table group.");
            Group = group;
            if (SFPaletteCache.TryGetValue(ColorPaletteName, out Palette)) { return true; }            
            SFPalette sfPalette = new SFPalette(in palette, in group);
            sfPalette.GetPalette();
            Palette = sfPalette;            
            SFPaletteCache.Add(ColorPaletteName, Palette);
            return true;
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
            var colorPalPtr = "id_0_c"; // Shape.Header.ColorPalettePtr
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
            var project = AppResources.ImportedProject;
            //Load the SFOptimizer
            if (!project.Optimizers.Any())
                throw new Exception("There aren't any optimizers added to this project yet.\n" +
                    "Use the Refresh ShapeMap button to create this.");
            //Find the one that is a SHAPE MAP
            var shapeOptim = project.Optimizers.FirstOrDefault(x => 
                x.OptimizerData.TypeSpecifier == Starfox.Editor.SFOptimizerTypeSpecifiers.Shapes);
            if (shapeOptim == default)
                throw new Exception("This project has Optimizers, but none of them are for Shapes.\n" +
                    "Use the Refresh ShapeMap button to create this.");            
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
            int index = geometry.Positions.Count(); // used to push indices
            geometry.Positions.Add(new(Point1.X, Point1.Y, Point1.Z)); // i
            geometry.Positions.Add(new(Point1.X - 1, Point1.Y, Point1.Z + 1)); // i + 1            
            geometry.Positions.Add(new(Point2.X, Point2.Y, Point2.Z)); // i + 2
            geometry.Positions.Add(new(Point2.X + 1, Point2.Y, Point2.Z - 1)); // i + 3
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
            BSPShape Shape, string ColorPalettePtr = "id_0_c", int Frame = -1)
        {
            CreateSFPalette(ColorPalettePtr, out var palette, out var group);
            return MakeBSPShapeMeshGeometry(Shape, in group, in palette, Frame);
        }
        /// <summary>
        /// Turns a <see cref="BSPShape"/> into a GeometryModel3D collection which makes up the supplied model
        /// </summary>
        /// <param name="Shape">The shape to draw</param>
        /// <param name="Group">The COLGroup instance to use to find color data</param>
        /// <param name="Palette">The palette that contains the colors to use</param>
        /// <param name="Frame">The frame of Animation to render</param>
        /// <param name="HighlightFace">Optionally, a face to highlight over all others</param>
        /// <returns></returns>
        public static List<GeometryModel3D> MakeBSPShapeMeshGeometry(
            BSPShape Shape, in COLGroup Group, in SFPalette Palette, int Frame, 
            BSPFace? HighlightFace = default)            
        {
            //SET VARS
            var models = new List<GeometryModel3D>();
            var shape = Shape;
            var group = Group;
            var currentSFPalette = Palette;
            var EDITOR_SelectedFace = HighlightFace;
            //---

            Color GetColor(COLDefinition.CallTypes Type, int colIndex, SFPalette palette)
            { // Get a color for a COLDefinition from the sfPalette
                var fooColor = System.Drawing.Color.Blue;
                switch (Type)
                {
                    case COLDefinition.CallTypes.Collite: // Diffuse
                        fooColor = palette.Collites[colIndex];
                        break;
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
                double _Opacity = 1;
                if (definition != default) // did we find it?
                {
                    int colIndex = 0;
                    Color color = Colors.Blue; // default color
                    switch (definition.CallType)
                    { // depending on call type we handle this material differently
                        case COLDefinition.CallTypes.Collite: // diffuse
                        case COLDefinition.CallTypes.Coldepth: // emissive, kinda
                        case COLDefinition.CallTypes.Colnorm: // normal? not sure.
                        case COLDefinition.CallTypes.Colsmooth: // not implemented
                            { // default, push the color to the model
                                colIndex = ((ICOLColorIndexDefinition)definition).ColorByte;
                                color = GetColor(definition.CallType, colIndex, currentSFPalette);
                            }
                            break;
                        case COLDefinition.CallTypes.Animation: // color animation
                            {
                                break;
                                var animDef = definition as COLAnimationReference; // find anim definition
                                //attempt to make a palette for this animation
                                if (!SHAPEStandard.CreateSFPalette(animDef.TableName, out var animSFPal, out var animGroup)) break;
                                int index = Frame > -1 ? Frame % animGroup.Definitions.Count : 0; // adjust color based on Frame parameter
                                var animMemberDefinition = animGroup.Definitions.ElementAt(index); // jump to color
                                color = GetColor(animMemberDefinition.CallType, // finally get the color from the animPalette
                                    ((ICOLColorIndexDefinition)animMemberDefinition).ColorByte,
                                    animSFPal);
                            }
                            break;
                    }
                    material = new DiffuseMaterial()
                    {
                        Brush = new SolidColorBrush(color),
                    }; // create the material, all of them use Diffuse in editor
                }
                //Do we have a selected face to highlight?
                if (EDITOR_SelectedFace != default)
                {
                    _Opacity = .5; // we do, make all of them semi-opaque
                    (material as DiffuseMaterial).Brush.Opacity = _Opacity; // current material is set to this opacity
                    if (EDITOR_SelectedFace == face)
                    { // oops, this material is the one we want to highlight!
                        material = new EmissiveMaterial()
                        {
                            Brush = Brushes.Yellow, // make it stand out
                        };
                        _Opacity = 1; // put the opacity back!
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
            }
            return models;
        }
    }
}
