using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop
{
    public static class SFFileType
    {
        public enum FileTypes
        {
            /// <summary>
            /// General-purpose ASM Code File with no specialized behavior
            /// </summary>
            ASM,
            /// <summary>
            /// An ASM File that is written as a Map-Script.
            /// </summary>
            MAP,
            /// <summary>
            /// A source file representative of a 3D Model.
            /// </summary>
            BSP
        }
        public static string GetSummary(FileTypes Type) => Type switch
        {
            FileTypes.ASM => "Just Assembly",
            FileTypes.MAP => "Map-Script File",
            FileTypes.BSP => "Compiled 3D Models",
            _ => "Not found", // default case
        };
    }
}
