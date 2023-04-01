using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX
{
    public class BSPColor
    {
        public BSPColor() : this(0,0,0)
        {

        }

        public BSPColor(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public static BSPColor FromDrawing(System.Drawing.Color Color)
        {
            return new BSPColor()
            {
                R = Color.R,
                G = Color.G,
                B = Color.B,
            };
        }
    }
}
