using StarFox.Interop.GFX.CONVERT;
using StarFox.Interop.GFX.DAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX
{
    /// <summary>
    /// Represents the properties of a Canvas
    /// </summary>
    public class CanvasSizeDefinition
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CharWidth { get; set; } = FXConvertConstraints.SNES_CHAR_SIZE;
        public int CharHeight { get; set; } = FXConvertConstraints.SNES_CHAR_SIZE; 
        public int Columns
        {
            get => Width / CharWidth;
            set => Width = value * CharWidth;
        }
        public int Rows
        {
            get => Width / CharWidth;
            set => Width = value * CharWidth;
        }
    }
}
