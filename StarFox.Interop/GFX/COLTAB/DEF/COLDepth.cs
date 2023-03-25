namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// A COLDepth function call, a color index
    /// <code>coldepth color</code>
    /// </summary>
    public class COLDepth : COLDefinition, ICOLColorIndexDefinition
    {
        public COLDepth(int colorByte)
        {
            ColorByte = colorByte;
        }
        public override CallTypes CallType => CallTypes.Coldepth;
        public int ColorByte { get; }
    }
}
