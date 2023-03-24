using StarFox.Interop.ASM.TYP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP.SHAPE
{
    /// <summary>
    /// Face functions have the following call signature:
    /// <para><code>face[2,3,4,5,6,7,8,12] color, index (unused), Normal (X,Y,Z), params Verts (as references to points)</code></para>
    /// </summary>
    internal static class BSPFaceStructureConverter
    {
        private static string[] CompatibleMacros =
        {
            "face2", "face3", "face4", "face5", "face6", "face7", "face8", "face12"
        };
        /// <summary>
        /// Attempts to convert the given line into a BSPFace, if they're compatible.
        /// </summary>
        /// <param name="Line"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public static bool TryParse(in ASMLine Line, out BSPFace Result)
        {
            Result = default;
            if (Line == default) return false; // stop.
            if (!Line.HasStructureApplied) return false;
            var Structure = Line.StructureAsMacroInvokeStructure;
            if (Structure == null) return false;

            if (!CompatibleMacros.Contains(Structure.MacroReference.Name.ToLower())) // not found
                return false; // uh oh, leave this line isn't a header
            //found the macro needed (face)
            Result = new BSPFace()
            {
                Color = Structure.TryGetParameter(0)?.TryParseOrDefault() ?? 0,
                Index = Structure.TryGetParameter(1)?.TryParseOrDefault() ?? 0,
                Normal = new BSPVec3()
                {
                    X = Structure.TryGetParameter(2)?.TryParseOrDefault() ?? 0,
                    Y = Structure.TryGetParameter(3)?.TryParseOrDefault() ?? 0,
                    Z = Structure.TryGetParameter(4)?.TryParseOrDefault() ?? 0,
                }
            };
            int startIndex = 5;
            int faces = Structure.Parameters.Count() - startIndex;
            var verts = Result.PointIndices = new BSPPointRef[faces];
            for(int i = 0; i < faces; i++)
            {
                verts[i] = new BSPPointRef()
                {
                    PointIndex = Structure.TryGetParameter(i + startIndex)?.TryParseOrDefault() ?? 0,
                    Position = i
                };
            }
            return true;
        }
    }
}
