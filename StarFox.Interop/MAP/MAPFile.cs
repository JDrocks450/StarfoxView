using StarFox.Interop.ASM;
using StarFox.Interop.MAP.EVT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP
{
    /// <summary>
    /// Represents a Map Script File
    /// </summary>
    public class MAPFile : ASMFile
    {
        /// <summary>
        /// The title of the MAP file
        /// </summary>
        public string Title { get; set; }      
        /// <summary>
        /// The events that make up this level script
        /// </summary>
        public HashSet<MAPEvent> Events { get; private set; } = new();
        /// <summary>
        /// Creates a new MAPFile representing the referenced file
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        internal MAPFile(string OriginalFilePath) : base(OriginalFilePath) {
            
        }
        internal MAPFile(ASMFile From) : this(From.OriginalFilePath)
        {
            this.Chunks = From.Chunks;
            this.Constants= From.Constants;            
        }
    }
}
