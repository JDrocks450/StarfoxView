using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP.SHAPE
{
    public class BSPShapeHeader
    {
        /// <summary>
        /// The line this has been parsed from
        /// </summary>
        [JsonIgnore]
        public ASMLine? Base { get; set; }
        /// <summary>
        /// The macro function used to create this shape header
        /// </summary>
        public string MacroName { get; set; }
        /// <summary>
        /// The pointer to where the points are stored
        /// </summary>
        public string PointPtr { get; set; }
        /// <summary>
        /// The bank to look in for this shape
        /// </summary>
        public int Bank { get; set; }
        public string FacePtr { get; set; }
        /// <summary>
        /// <code>byte</code>
        /// Looks unused in the code
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// <code>ZSort (bitshift left) Shift</code>
        /// Unsure what this currently does
        /// </summary>
        public int ZSort { get; set; }
        /// <summary>
        /// <code>Height (bitshift left) Shift</code>
        /// Unused in ShapeHdr macro
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// <code>View (bitshift left) Shift</code>
        /// Unused in ShapeHdr macro
        /// </summary>
        public int View { get; set; }
        /// <summary>
        /// Shifts left <see cref="ZSort"/>, <see cref="Height"/>, <see cref="View"/>
        /// </summary>
        public int Shift { get; set; }
        /// <summary>
        /// Radius is run through macro function <c>chk1dig</c> for verification
        /// <para><code>word</code></para>
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// XMax, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int XMax { get; set; }
        /// <summary>
        /// YMax, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int YMax { get; set; }
        /// <summary>
        /// ZMax, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int ZMax { get; set; }
        /// <summary>
        /// Size, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int Size { get; set; }
        /// <summary>
        /// Pointer to color palette used for this shape. 
        /// <para>For our purposes we're storing just the name.</para>
        /// </summary>
        public string ColorPalettePtr { get; set; }
        /// <summary>
        /// Shadow reference
        /// </summary>
        public int Shadow { get; set; }
        /// <summary>
        /// Generally unused
        /// </summary>
        public string Simple1 { get; set; }
        /// <summary>
        /// Generally unused
        /// </summary>
        public string Simple2 { get; set; }
        /// <summary>
        /// Generally unused
        /// </summary>
        public string Simple3 { get; set; }
        /// <summary>
        /// Name of this shape as it appears in the Shape Header. 
        /// <para/>The last parameter in the ShapeHdr macro
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// If an inline label appears on the same line as this definition, the label's text is copied here.
        /// </summary>
        public string? InlineLabelName { get; set; }
        /// <summary>
        /// A name that has been given by the importer, unique to any other shape in the file.
        /// <para/>Generally follows the formula: <c><see cref="Name"/>_NumberOfDuplicates</c>
        /// </summary>
        public string UniqueName { get; set; }
        /// <summary>
        /// If this shape is an alternate form of another shape, the place where the data can be found is
        /// pointed to here.
        /// <para/>This is the name of the shape that has this data.
        /// </summary>
        public string? DataPointer { get; set; }
        public bool HasDataPointer => DataPointer != null;

        internal static string[] CompatibleMacroNames =
        {
            "shapehdr", "oshapehdr"
        };

        internal BSPShapeHeader()
        {

        }

        public BSPShapeHeader(string pointPtr, int bank, string facePtr, 
            int type, int zSort, int height, int view, int shift, float radius, int xMax, int yMax, int zMax,
            int size, string colorPalettePtr, int shadow, string simple1, string simple2, string simple3, string name)
        {
            PointPtr = pointPtr;
            Bank = bank>>16;
            FacePtr = facePtr;
            Type = type;
            ZSort = zSort<<shift;
            Height = height<<shift;
            View = view << shift;
            Shift = shift;
            Radius = radius;
            XMax = xMax << shift;
            YMax = yMax << shift;
            ZMax = zMax << shift;
            Size = size << Shift;
            ColorPalettePtr = colorPalettePtr;
            Shadow = shadow;
            Simple1 = simple1;
            Simple2 = simple2;
            Simple3 = simple3;
            Name = name;
            UniqueName = name;
        }

        /// <summary>
        /// Attempts to turn the given macro invoke expression into a shape header function call.
        /// <para>If the given line is not a valid header function call, it will return false.</para>
        /// <para>If it is valid, it will steal as many parameters as it can and put them into the returned object.</para>
        /// </summary>
        /// <param name="Structure"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static bool TryParse(ASMLine Line, out BSPShapeHeader? header, params ASMFile[] Includes)
        {
            header = default;
            if (Line == default) return false; // stop.
            if (!Line.HasStructureApplied) return false;
            var Structure = Line.StructureAsMacroInvokeStructure;
            if (Structure == null) return false;

            if (!CompatibleMacroNames.Contains(Structure.MacroReference.Name.ToLower())) // not found
                return false; // uh oh, leave this line isn't a header
            //found the macro needed (shape header)
            ASMExtensions.BeginConstantsContext(Includes);
            //---- CONST CONTEXT START
            var pptr = Structure.TryGetParameter(0)?.ParameterContent ?? ""; // name of the point ptr
            var bank = Structure.TryGetParameter(1)?.TryParseOrDefault() ?? 0;
            var fptr = Structure.TryGetParameter(2)?.ParameterContent ?? ""; // name of the face ptr
            var type = Structure.TryGetParameter(3)?.TryParseOrDefault() ?? 0;
            var zsort = Structure.TryGetParameter(4)?.TryParseOrDefault() ?? 0;
            var height = Structure.TryGetParameter(5)?.TryParseOrDefault() ?? 0;
            var view = Structure.TryGetParameter(6)?.TryParseOrDefault() ?? 0;
            var shift = Structure.TryGetParameter(7)?.TryParseOrDefault() ?? 0;
            var radius = Structure.TryGetParameter(8)?.TryParseOrDefault() ?? 0;
            var xmax = Structure.TryGetParameter(9)?.TryParseOrDefault() ?? 0;
            var ymax = Structure.TryGetParameter(0x0A)?.TryParseOrDefault() ?? 0;
            var zmax = Structure.TryGetParameter(0x0B)?.TryParseOrDefault() ?? 0;
            var size = Structure.TryGetParameter(0x0C)?.TryParseOrDefault() ?? 0;
            var cptr = Structure.TryGetParameter(0x0D)?.ParameterContent ?? ""; // name of the color ptr
            var shadow = Structure.TryGetParameter(0x0E)?.TryParseOrDefault() ?? 0;
            var simple1 = Structure.TryGetParameter(0x0F)?.ParameterContent ?? "0";
            var simple2 = Structure.TryGetParameter(0x10)?.ParameterContent ?? "0";
            var simple3 = Structure.TryGetParameter(0x11)?.ParameterContent ?? "0";
            var name = Structure.TryGetParameter(0x12)?.ParameterContent ?? ""; // name of the object
            //---- END
            ASMExtensions.EndConstantsContext();
            header = new(pptr, bank, fptr, type, zsort, height, view, shift, 
                radius, xmax, ymax, zmax, size, cptr, shadow, simple1, simple2, simple3, name)
            {
                MacroName = Structure.MacroReference.Name,
                Base = Line,
                InlineLabelName = Line.InlineLabel
            };
            return true;
        }

        public override string ToString()
        {
            return $"Shape Header: {Name}";
        }
    }
}
