﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop
{
    public static class SFFileType
    {
        /// <summary>
        /// Files that are interpreted as ASM
        /// </summary>
        public enum ASMFileTypes
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
            BSP,
            /// <summary>
            /// Messages for commentary in the game
            /// </summary>
            MSG,            
        }
        /// <summary>
        /// Files that are stored and interpreted as Binary
        /// </summary>
        public enum BINFileTypes
        {
            /// <summary>
            /// Interlaced CGX files in High and Low Banks <see cref="GFX.DAT.FXGraphicsHiLowBanks"/>
            /// </summary>
            COMPRESSED_CGX,
            /// <summary>
            /// Sound Effects using the Bit Rate Reduction technique
            /// </summary>
            BRR,
        }
        public static string GetSummary(ASMFileTypes Type) => Type switch
        {
            ASMFileTypes.ASM => "Just Assembly",
            ASMFileTypes.MAP => "Map-Script File",
            ASMFileTypes.BSP => "Compiled 3D Models",
            ASMFileTypes.MSG => "Communications",
            _ => "Not found", // default case
        };
        public static string GetSummary(BINFileTypes Type) => Type switch
        {
            BINFileTypes.COMPRESSED_CGX => "Crunch'd Graphics",
            BINFileTypes.BRR => "Sound Effects (SFX)",
            _ => "Not found", // default case
        };
    }
}
