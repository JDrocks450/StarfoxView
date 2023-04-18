using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX.CONVERT
{
    /// <summary>
    /// A base format for a bytemap with no format, just raw image data.
    /// </summary>
    public class FXConvertImage
    {
        public CanvasSizeDefinition Canvas { get; set; } = new()
        {
            Width = FXConvertConstraints.SuggestedCanvasW,
            Height = FXConvertConstraints.SuggestedCanvasH,
        };
        public int Width => Canvas.Width;
        public int Height => Canvas.Height;
        public int CharWidth => Canvas.CharWidth;
        public int CharHeight => Canvas.CharHeight;
        public int ColorBPP = 4;
        public byte[] ImageData = new byte[0];

        public FXConvertImage(int W, int H) : this(new()
        {
            Width= W,
            Height= H
        })
        {

        }
        public FXConvertImage(CanvasSizeDefinition Canvas)
        {
            this.Canvas = Canvas;
            ImageData = new byte[Width * Height];
        }
    }
}
