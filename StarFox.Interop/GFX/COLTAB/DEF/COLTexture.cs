using StarFox.Interop.GFX.COLTAB.DEF;

namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// A COLTexture function call, which takes a table name
    /// <code>coltext tabledef</code>
    /// </summary>
    public class COLTexture : COLDefinition
    {
        public COLTexture(string reference)
        {
            Reference = reference;
        }
        public override CallTypes CallType => CallTypes.Texture;
        public string Reference { get; }
    }
}
