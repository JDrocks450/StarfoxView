using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM
{
    public class ASMFile : IImporterObject
    {
        public string OriginalFilePath { get; }
        /// <summary>
        /// Chunks added to this object through the <see cref="ASMImporter"/>
        /// <para>Not recommended for the caller to manipulate this collection unless through an <see cref="IImporter{T}"/></para>
        /// </summary>
        public HashSet<ASMChunk> Chunks { get; } = new HashSet<ASMChunk>();

        public ASMFile(string OriginalFilePath)
        {
            this.OriginalFilePath = OriginalFilePath;
        }
    }
}
