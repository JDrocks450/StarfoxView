using Microsoft.VisualBasic;
using StarFox.Interop.GFX.COLTAB;
using StarFox.Interop.GFX.COLTAB.DEF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static StarFox.Interop.GFX.CAD;

namespace StarFox.Interop.GFX
{
    /// <summary>
    /// Provides an interface to generate palettes compatible with Starfox.
    /// <para>This will handle creating colors through Lerping to emulate dithering.</para>
    /// </summary>
    public class SFPalette
    {
        private readonly COL palette;
        private readonly COLGroup group;

        /// <summary>
        /// The generated colors added to this <see cref="SFPalette"/>
        /// <para>Call <see cref="GetPalette"/> to evaluate this property.</para>
        /// </summary>
        public Color[] Colors { get; private set; } = { };
        /// <summary>
        /// A filtered list of just collites (light colors)
        /// </summary>
        public Dictionary<int, Color> Collites { get; } = new();
        /// <summary>
        /// A filtered list of just coldepths (light colors)
        /// </summary>
        public Dictionary<int, Color> Coldepths { get; } = new();
        private bool IsOdd = false;

        /// <summary>
        /// Create a new <see cref="SFPalette"/> taking colors from Palette and context from Group.
        /// </summary>
        /// <param name="Palette">The colors to use</param>
        /// <param name="Group">The color table found in a <see cref="COLTABFile"/></param>
        public SFPalette(in COL Palette, in COLGroup Group)
        {
            palette = Palette;
            group = Group;
        }
        public Bitmap RenderPalette()
        {
            double sqSize = Colors.Length < 1 ? 1 : Math.Sqrt(Colors.Length);
            int isqSize = (int)Math.Ceiling(sqSize); // round up for square size
            Bitmap bmp = new Bitmap(isqSize, isqSize);
            for (int y = 0; y < isqSize; y++)
            { // row
                for (int x = 0; x < isqSize; x++)
                { // col
                    int index = (isqSize * y) + x;
                    var color = index < Colors.Length ? Colors[index] : Color.White;
                    bmp.SetPixel(x, y, color);
                }
            }
            return bmp;
        }
        /// <summary>
        /// Writes the serialized color data to the given stream
        /// </summary>
        public async Task SerializeColors(Stream Destination)
        {
            await JsonSerializer.SerializeAsync(Destination, Colors.Select(x => BSPColor.FromDrawing(x)));
        }
        /// <summary>
        /// Gets the palette, this is only compatible with 8BPP mode
        /// </summary>
        /// <returns></returns>
        public Color[] GetPalette()
        {
            Collites.Clear(); // clear previous colors now
            IsOdd = false;
            var colors = new Color[0];
            int i = -1, actualPosition = -1;
            foreach(var definition in group.Definitions)
            {
                i++;
                actualPosition++;
                var color = definition.CallType switch
                {
                    COLDefinition.CallTypes.Collite => HandleCollite(definition as COLLite),
                    COLDefinition.CallTypes.Coldepth => HandleColdepth(definition as COLDepth),
                    _ => default
                };                
                if (color == null)
                {
                    i--;
                    continue;
                }
                Array.Resize(ref colors, i + 1);
                colors[i] = color.Value;
                if (actualPosition == 41) break;
            }
            return Colors = colors;
        }
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        public static Color LerpColor(Color color1, Color color2, float t)
        {
            float r = Lerp(color1.R, color2.R, t);
            float g = Lerp(color1.G, color2.G, t);
            float b = Lerp(color1.B, color2.B, t);

            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }
        private Color GetGreyscale(float Intensity) =>
            GetGreyscale(ColorTranslator.FromHtml("#192419"), Color.White, Intensity);
        private Color GetGreyscale(Color From, Color To, float Intensity) => LerpColor(From, To, Intensity);
        Color GetSolidColor(int ColorByte)
        { // SOLID COLOR
            if (Coldepths.TryGetValue(ColorByte, out var Coldepth))
                return Coldepth;
            var dirtyByte = ColorByte - 10;
            int index = (dirtyByte / 2) + (dirtyByte % 2);
            var thisColor = palette.GetPalette()[index]; // 8BPP get color by index
            Coldepths.Add(ColorByte, thisColor);
            return thisColor;
        }
        private Color GetMixies(int ColorByte)
        {
            Color Color1 = GetSolidColor(0x16); // get blue by default
            Color Color2 = Color.White;
            switch (ColorByte)
            {
                case 0x19:
                    Color2 = GetSolidColor(0x0b); // dark red
                    break;
                case 0x1a:
                    Color2 = GetSolidColor(0x0d); // dark red
                    break;
                case 0x1b:
                    Color2 = GetSolidColor(0x11); // bright orange
                    break;
                case 0x1c:
                    Color2 = GetSolidColor(0x0f); // orange
                    break;
                case 0x1d:
                    Color1 = GetSolidColor(0x1e);
                    Color2 = GetSolidColor(0x02);
                    break;
                case 0x1f:
                    Color1 = GetSolidColor(0x1e);
                    Color2 = GetSolidColor(0x18);
                    break;
            }
            return LerpColor(Color1, Color2, .5f);
        }
        private Color? HandleColdepth(COLDepth DepthColor)
        {
            if (DepthColor.ColorByte < 0x0B)
            { // 1-10 reserved for greyscale
                var grey = GetGreyscale(DepthColor.ColorByte / 10.0f);
                Coldepths.Add(DepthColor.ColorByte, grey);
                return grey;
            }
            if (DepthColor.ColorByte == 0x1f || (DepthColor.ColorByte >= 0x19 && DepthColor.ColorByte < 0x1e)) // 0x19 -> 0x1d && 0x1f is mixies
                return GetMixies(DepthColor.ColorByte);
            if (DepthColor.ColorByte == 0x12)
            {
                IsOdd = false;
                return default;
            }
            bool isInbetween = IsOdd; // simple alternator
            if (isInbetween)
            {
                IsOdd = false;
                var previousColor = Coldepths[DepthColor.ColorByte-1];
                var nextColor = GetSolidColor(DepthColor.ColorByte + 1);
                var thisColor = LerpColor(previousColor, nextColor, .5f); // lerp by half of both colors
                Coldepths.Add(DepthColor.ColorByte, thisColor);
                return thisColor;
            }
            IsOdd = true;
            return GetSolidColor(DepthColor.ColorByte);
        }
        /// <summary>
        /// Processes a <see cref="COLLite"/> color (WIP)
        /// </summary>
        /// <param name="LightColor"></param>
        /// <returns></returns>
        private Color HandleCollite(COLLite LightColor)
        {
            if (Collites.TryGetValue(LightColor.ColorByte, out var color))
                return color;
            // Falling back on CoolK definitions until parser is complete
            var collite = ColorTranslator.FromHtml(LightColor.ColorByte switch
            {
                0 => "#E7E7E7", //Solid Dark Grey
                1 => "#AAAAAA", // = Solid Darker Grey
                2 => "#BA392A", //= Shaded Bright Red/Dark Red
                3 => "#5144D4", //= Shaded Blue/Bright Blue
                4 => "#D8A950", // = Shaded Bright Orange/Black
                5 => "#190646", //= Shaded Turquoise/Black
                6 => "#801009", //= Solid Dark Red
                7 => "#2411A3", //= Solid Blue
                8 => "#7C11A3", //= Shaded Red/blue (Purple)
                9 => "#2F9E28", //= Shaded Green/Dark Green
            });
            Collites.Add(LightColor.ColorByte, collite);
            return collite;
        }
    }
}
