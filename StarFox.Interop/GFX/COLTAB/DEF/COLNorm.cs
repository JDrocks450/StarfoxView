namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// A COLNorm function call, which takes a ? and a color index
    /// <code>colnorm ?,color</code>
    /// </summary>
    public class COLNorm : COLDefinition, ICOLColorIndexDefinition
    {
        public COLNorm(int lightSource, int colorByte)
        {
            LightSource = lightSource;
            ColorByte = colorByte;
        }
        public override CallTypes CallType => CallTypes.Colnorm;
        public int LightSource { get; }
        public int ColorByte { get; }
    }
}
