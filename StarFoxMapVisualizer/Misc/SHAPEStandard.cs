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

namespace StarFoxMapVisualizer.Misc
{
    /// <summary>
    /// Common helper functions for interacting with Shape files into the editor
    /// </summary>
    internal static class SHAPEStandard
    {
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
            if (ColorPaletteName.ToUpper() != "ID_0_C")
                throw new Exception($"Any palettes that aren't ID_0_C have been disabled in this build. This one is: {ColorPaletteName}");
            COL? palette =
                AppResources.ImportedProject.Palettes.FirstOrDefault
                (x => System.IO.Path.GetFileNameWithoutExtension(x.Key).ToUpper() == "NIGHT").Value;
            var group = default(COLGroup);
            if (ProjectColorTable != null)
                ProjectColorTable.TryGetGroup(ColorPaletteName, out group);
            if (palette == null || group == null)
                throw new Exception("There was a problem loading the palette and/or color table group.");
            SFPalette sfPalette = new SFPalette(in palette, in group);
            sfPalette.GetPalette();
            Palette = sfPalette;
            Group = group;
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
    }
}
