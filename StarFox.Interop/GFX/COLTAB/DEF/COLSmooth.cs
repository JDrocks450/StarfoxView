namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// A COLSmooth function call, which takes a light source and a color index
    /// <code>colsmooth light,color</code>
    /// </summary>
    public class COLSmooth : COLDefinition, ICOLColorIndexDefinition
    {
        public COLSmooth(int lightSource, int colorByte)
        {
            LightSource = lightSource;
            ColorByte = colorByte;
        }
        public override CallTypes CallType => CallTypes.Colsmooth;
        public int ColorByte { get; }
        public int LightSource { get; }
    }
}
