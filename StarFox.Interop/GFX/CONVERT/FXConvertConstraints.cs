using StarFox.Interop.GFX.DAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX.CONVERT
{
    public static class FXConvertConstraints
    {
        public enum FXCanvasTemplates
        {
            MSPRITES,
            CGX
        }
        public const int SNES_CHAR_SIZE = 8;
        public const int SuggestedCanvasH = 128, SuggestedCanvasW = 256;
        /// <summary>
        /// 0: 3DMSPRITES Canvas
        /// 1: CGX Regular Canvas
        /// </summary>
        internal static CanvasSizeDefinition[] GeneralCanvasSizes =
        {
            //3D MSPRITES
            new()
            {
                Width = SuggestedCanvasW,
                Height = SuggestedCanvasH,
                CharWidth = SNES_CHAR_SIZE,
                CharHeight = SNES_CHAR_SIZE
            },
            //CGX REGULAR
            new()
            {
                Width = FXCGXFile.SuggestedCanvasW,
                Height = FXCGXFile.SuggestedCanvasH,
                CharWidth = SNES_CHAR_SIZE,
                CharHeight = SNES_CHAR_SIZE
            },
        };
        public static CanvasSizeDefinition GetDefinition(FXCanvasTemplates Template) => GeneralCanvasSizes[(int)Template];
    }
}
