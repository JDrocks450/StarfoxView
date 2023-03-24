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
        internal BSPFile(string OriginalFilePath) : base(OriginalFilePath)
        {

        }
        internal BSPFile(ASMFile From) : base(From)
        {

        }
    }
}
