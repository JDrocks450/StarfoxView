using StarFox.Interop.ASM;
using StarFox.Interop.BSP.SHAPE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BSP
{
    public class BSPFile : ASMFile
    {
        /// <summary>
        /// The shapes added in this BSP file
        /// <para>Usually shapes come in large files with many other shapes defined along side them.</para>
        /// </summary>
        public HashSet<BSPShape> Shapes { get; internal set; } = new();
        /// <summary>
        /// These are shapes that are just a standalone definition. Developers used a trick to get multiple shapes out of one set of faces and
        /// points through clever use of inline labels that reference the same shape code.
        /// </summary>
        public HashSet<BSPShape> BlankShapes { get; internal set; } = new();
        /// <summary>
        /// Errors that happened while exporting are dumped here
        /// </summary>
        public StringBuilder ImportErrors { get; } = new();

        public HashSet<string> ShapeHeaderEntries { get; } = new();
        /// <summary>
        /// Finds items that are in <see cref="ShapeHeaderEntries"/> yet not in <see cref="Shapes"/>
        /// </summary>
        /// <returns></returns>
        /*
        public IEnumerable<string> GetShapeHeaderDiscrepencies()
        {
            List<string> discrepencies = new();
            foreach(var headerItem in ShapeHeaderEntries)
            {
                if (!Shapes.ContainsKey(headerItem))
                    discrepencies.Add(headerItem);
            }
            return discrepencies;
        }*/

        internal BSPFile(string OriginalFilePath) : base(OriginalFilePath)
        {

        }
        internal BSPFile(ASMFile From) : base(From)
        {

        }
    }
}
