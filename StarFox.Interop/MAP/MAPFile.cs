using StarFox.Interop.ASM;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
        /// Scripts in this file, accessible by their label name. See: <see cref="MAPScriptHeader.LevelMacroName"/>
        /// <para/>
        /// One <see cref="MAPFile"/> can have many level scripts inside
        /// <para/> They should start with an label indicating where it starts
        /// and end with a <c>mapend</c> event
        /// </summary>
        public Dictionary<string, MAPScript> Scripts { get; } = new();
        /// <summary>
        /// Creates a new MAPFile representing the referenced file
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        internal MAPFile(string OriginalFilePath) : base(OriginalFilePath) {
            
        }
        internal MAPFile(ASMFile From) : base(From)
        { 
            
        }        
    }
}
