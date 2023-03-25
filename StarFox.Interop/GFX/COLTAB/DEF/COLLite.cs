namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// An interface for <see cref="COLDefinition"/> that use the ColorByte property.
    /// </summary>
    public interface ICOLColorIndexDefinition
    {
        int ColorByte { get; }
    }
    /// <summary>
    /// A COLLite function call, which takes a light source and a color index
    /// <code>collite light,color</code>
    /// </summary>
    public class COLLite : COLDefinition, ICOLColorIndexDefinition
    {
        public COLLite(int lightSource, int colorByte)
        {
            LightSource = lightSource;
            ColorByte = colorByte;
        }

        public override CallTypes CallType => CallTypes.Collite;
        public int LightSource { get; }
        public int ColorByte { get; }
    }
}
