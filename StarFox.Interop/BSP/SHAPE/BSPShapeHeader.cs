using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP.SHAPE
{
    public class BSPShapeHeader
    {
        /// <summary>
        /// The line this has been parsed from
        /// </summary>
        public ASMLine? Base { get; internal set; }
        /// <summary>
        /// The macro function used to create this shape header
        /// </summary>
        public string MacroName { get; internal set; }
        /// <summary>
        /// The pointer to where the points are stored
        /// </summary>
        public string PointPtr { get; }
        /// <summary>
        /// The bank to look in for this shape
        /// </summary>
        public int Bank { get; }
        public string FacePtr { get; }
        /// <summary>
        /// <code>byte</code>
        /// Looks unused in the code
        /// </summary>
        public int Type { get; }
        /// <summary>
        /// <code>ZSort (bitshift left) Shift</code>
        /// Unsure what this currently does
        /// </summary>
        public int ZSort { get; }
        /// <summary>
        /// <code>Height (bitshift left) Shift</code>
        /// Unused in ShapeHdr macro
        /// </summary>
        public int Height { get; }
        /// <summary>
        /// <code>View (bitshift left) Shift</code>
        /// Unused in ShapeHdr macro
        /// </summary>
        public int View { get; }
        /// <summary>
        /// Shifts left <see cref="ZSort"/>, <see cref="Height"/>, <see cref="View"/>
        /// </summary>
        public int Shift { get; }
        /// <summary>
        /// Radius is run through macro function chk1dig for verification
        /// <para><code>word</code></para>
        /// </summary>
        public float Radius { get; }
        /// <summary>
        /// XMax, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int XMax { get; }
        /// <summary>
        /// YMax, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int YMax { get; }
        /// <summary>
        /// ZMax, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int ZMax { get; }
        /// <summary>
        /// Size, shift left by <see cref="Shift"/>
        /// <para><code>word</code></para>
        /// </summary>
        public int Size { get; }
        /// <summary>
        /// Pointer to color palette used for this shape. 
        /// <para>For our purposes we're storing just the name.</para>
        /// </summary>
        public string ColorPalettePtr { get; }
        /// <summary>
        /// Shadow reference
        /// </summary>
        public int Shadow { get; }
        /// <summary>
        /// Generally unused
        /// </summary>
        public string Simple1 { get; }
        /// <summary>
        /// Generally unused
        /// </summary>
        public string Simple2 { get; }
        /// <summary>
        /// Generally unused
        /// </summary>
        public string Simple3 { get; }
        /// <summary>
        /// Name of this shape
        /// </summary>
        public string Name { get; }
        internal static string[] CompatibleMacroNames =
        {
            "shapehdr"
        };

        internal BSPShapeHeader()
        {

        }

        public BSPShapeHeader(string pointPtr, int bank, string facePtr, 
            int type, int zSort, int height, int view, int shift, float radius, int xMax, int yMax, int zMax,
            int size, string colorPalettePtr, int shadow, string simple1, string simple2, string simple3, string name)
        {
            PointPtr = pointPtr;
            Bank = bank;
            FacePtr = facePtr;
            Type = type;
            ZSort = zSort;
            Height = height;
            View = view;
            Shift = shift;
            Radius = radius;
            XMax = xMax;
            YMax = yMax;
            ZMax = zMax;
            Size = size;
            ColorPalettePtr = colorPalettePtr;
            Shadow = shadow;
            Simple1 = simple1;
            Simple2 = simple2;
            Simple3 = simple3;
            Name = name;
        }

        /// <summary>
        /// Attempts to turn the given macro invoke expression into a shape header function call.
        /// <para>If the given line is not a valid header function call, it will return false.</para>
        /// <para>If it is valid, it will steal as many parameters as it can and put them into the returned object.</para>
        /// </summary>
        /// <param name="Structure"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static bool TryParse(ASMLine Line, out BSPShapeHeader? header)
        {
            header = default;
            if (Line == default) return false; // stop.
            if (!Line.HasStructureApplied) return false;
            var Structure = Line.StructureAsMacroInvokeStructure;
            if (Structure == null) return false;

            if (!CompatibleMacroNames.Contains(Structure.MacroReference.Name.ToLower())) // not found
                return false; // uh oh, leave this line isn't a header
            //found the macro needed (shape header)
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
            if (!string.IsNullOrWhiteSpace(name))
                name = name.TrimStart('<').TrimEnd('>');
            header = new(pptr, bank, fptr, type, zsort, height, view, shift, 
                radius, xmax, ymax, zmax, size, cptr, shadow, simple1, simple2, simple3, name)
            {
                MacroName = Structure.MacroReference.Name,
                Base = Line
            };
            return true;
        }

        public override string ToString()
        {
            return $"Shape Header: {Name}";
        }
    }
}
