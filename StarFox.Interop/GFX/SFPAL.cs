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
        /// <summary>
        /// A filtered list of just colnorms (colors based on normal of face ... in relation to a supposed source)
        /// </summary>
        public Dictionary<int, Color> Colnorms { get; } = new();
        /// <summary>
        /// External palettes (such as animations) are listed here to include in Editors
        /// </summary>
        public HashSet<string> ReferencedPaletteNames { get; } = new();
        /// <summary>
        /// External textures (such as <see cref="MSprites"/>) are listed here to include in Editors
        /// </summary>
        public HashSet<string> ReferencedTextureNames { get; } = new();

        private bool IsOdd = false;
        private int colorPaletteStartIndex = -1;
        private Color GetColorByIndex(int Index)
        {
            var colors = palette.GetPalette();
            return colors[Math.Min(colors.Length-1, Index + colorPaletteStartIndex)];
        }

        /// <summary>
        /// Create a new <see cref="SFPalette"/> taking colors from Palette and context from Group.
        /// </summary>
        /// <param name="Palette">The colors to use</param>
        /// <param name="Group">The color table found in a <see cref="COLTABFile"/></param>
        public SFPalette(in COL Palette, in COLGroup Group)
        {
            palette = Palette;
            group = Group;

            var colors = palette.GetPalette();
            //skip all black color except palette
            for(int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                if (color.R == 0 && color.G == 0 && color.B == 0) // black
                    continue;
                colorPaletteStartIndex = i - 1;
                break;
            }
            if (colorPaletteStartIndex < 1) colorPaletteStartIndex = 0;
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
            Coldepths.Clear();
            Colnorms.Clear();
            ReferencedPaletteNames.Clear();
            ReferencedTextureNames.Clear();

            IsOdd = false;
            var colors = new Color[0];
            int i = -1, actualPosition = -1;
            foreach (var definition in group.Definitions)
            {
                i++;
                actualPosition++;
                var color = definition.CallType switch
                {
                    COLDefinition.CallTypes.Collite => HandleCollite(definition as COLLite),
                    COLDefinition.CallTypes.Coldepth => HandleColdepth(definition as COLDepth),
                    COLDefinition.CallTypes.Colnorm => HandleColnorm(definition as COLNorm),
                    //COLDefinition.CallTypes.Colsmooth => HandleColsmooth(definition as COLSmooth),                    
                    _ => default
                };
                if (definition is COLAnimationReference anim)
                    ReferencedPaletteNames.Add(anim.TableName);
                if (definition is COLTexture texture)
                    ReferencedTextureNames.Add(texture.Reference);
                if (color == null)                        
                    continue;                
                Array.Resize(ref colors, i + 1);
                colors[i] = color.Value;               
            }
            return Colors = colors;
        }
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        public static Color LerpColor(Color color1, Color color2, float t = .5f)
        {
            float r = Lerp(color1.R, color2.R, t);
            float g = Lerp(color1.G, color2.G, t);
            float b = Lerp(color1.B, color2.B, t);

            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }
        private Color GetGreyscale(float Intensity) =>
            GetGreyscale(GetColorByIndex(0x9), Color.White, Intensity);
        private Color GetGreyscale(Color From, Color To, float Intensity) => LerpColor(From, To, Intensity);
        Color GetSolidColor(int ColorByte)
        { // SOLID COLOR
            var dirtyByte = ColorByte - 10;
            int index = (dirtyByte / 2) + (dirtyByte % 2);
            var thisColor = GetColorByIndex(index); // 8BPP get color by index
            return thisColor;
        }
        private Color GetMixies(int ColorByte)
        {
            Color Color1 = GetSolidColor(0x16); // get blue by default
            Color Color2 = Color.White;
            switch (ColorByte)
            {
                case 0x19:
                    Color2 = GetColorByIndex(1); // dark red
                    break;
                case 0x1a:
                    Color2 = GetColorByIndex(2); // coral
                    break;
                case 0x1b:
                    Color2 = GetColorByIndex(3); // orange
                    break;
                case 0x1c:
                    Color2 = GetColorByIndex(4); // bright orange
                    break;
                case 0x1d:
                    Color1 = GetColorByIndex(0xF); // green
                    Color2 = GetColorByIndex(0xA); // dark grey
                    break;
                case 0x1f:
                    Color1 = GetColorByIndex(0xF); // green
                    Color2 = GetColorByIndex(0xD); // silver
                    break;
            }
            return LerpColor(Color1, Color2, .5f);
        }
        private Color? HandleColdepth(COLDepth DepthColor)
        {
            var color = HandleDepthColorByte(DepthColor.ColorByte);
            if (!color.HasValue) return null;
            Coldepths.TryAdd(DepthColor.ColorByte, color.Value);
            return color;
        }
        private Color? HandleDepthColorByte(int ColorByte)
        {
            if (ColorByte < 0x0B)
            { // 1-10 reserved for greyscale
                var grey = GetGreyscale(ColorByte / 10.0f);
                return grey;
            }
            if (ColorByte == 0x1f || (ColorByte >= 0x19 && ColorByte < 0x1e)) // 0x19 -> 0x1d && 0x1f is mixies
                return GetMixies(ColorByte);
            if (ColorByte == 0x1e)
            {
                var color = GetColorByIndex(0xF);
                return color;
            }
            if (ColorByte == 0x12)
            {
                IsOdd = false;
                return default;
            }
            bool isInbetween = IsOdd; // simple alternator
            if (isInbetween)
            {
                IsOdd = false;
                var previousColor = Coldepths[ColorByte - 1];
                var nextColor = GetSolidColor(ColorByte + 1);
                var thisColor = LerpColor(previousColor, nextColor, .5f); // lerp by half of both colors
                return thisColor;
            }
            IsOdd = true;
            return GetSolidColor(ColorByte);
        }
        private Color HandleColorByte(int ColorByte)
        {
            if (Collites.TryGetValue(ColorByte, out var color))
                return color;

            var collite = ColorByte switch
            {
                0 => LerpColor(GetColorByIndex(10), GetColorByIndex(11)), // dark grey - grey
                1 => LerpColor(GetColorByIndex(9), GetColorByIndex(10)), // dark blue - dark grey
                2 => LerpColor(GetColorByIndex(9), GetColorByIndex(2)), // dark blue - dark red
                3 => LerpColor(GetColorByIndex(9), GetColorByIndex(5)), // dark blue - blue
                4 => LerpColor(GetColorByIndex(9), GetColorByIndex(3)), // dark blue - coral
                5 => LerpColor(GetColorByIndex(9), GetColorByIndex(6)), // dark blue - light blue
                6 => LerpColor(GetColorByIndex(2), GetColorByIndex(9)), // dark red - dark blue
                7 => LerpColor(GetColorByIndex(5), GetColorByIndex(9)), // blue - dark blue
                8 => LerpColor(GetColorByIndex(2), GetColorByIndex(5)), // dark red - dark blue
                9 => LerpColor(GetColorByIndex(0xF), GetColorByIndex(0x9))
            };
            if (false)
            {
                // Falling back on CoolK definitions until parser is complete
                collite = ColorTranslator.FromHtml(ColorByte switch
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
                    _ => "#FFFFFF" // = Error -- color not found
                });
            }
            Collites.Add(ColorByte, collite);
            return collite;
        }
        /// <summary>
        /// Processes a <see cref="COLLite"/> color (WIP)
        /// </summary>
        /// <param name="LightColor"></param>
        /// <returns></returns>
        private Color HandleCollite(COLLite LightColor) => HandleColorByte(LightColor.ColorByte);
        /// <summary>
        /// Very WIP!
        /// </summary>
        /// <param name="LightColor"></param>
        /// <returns></returns>
        private Color HandleColnorm(COLNorm LightColor)
        {
            var color = GetColorByIndex(LightColor.ColorByte);
            Colnorms.TryAdd(LightColor.ColorByte, color);
            return color;
        }
        /// <summary>
        /// Very WIP!
        /// </summary>
        /// <param name="LightColor"></param>
        /// <returns></returns>
        private Color HandleColsmooth(COLSmooth LightColor) => HandleDepthColorByte(LightColor.ColorByte) ?? Color.Red;
    }
}
