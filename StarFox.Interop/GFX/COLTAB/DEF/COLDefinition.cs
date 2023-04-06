using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP.STRUCT;

namespace StarFox.Interop.GFX.COLTAB.DEF
{
    /// <summary>
    /// A base-class for a definition found in a <see cref="COLGroup"/>
    /// </summary>
    public abstract class COLDefinition
    {
        public enum CallTypes
        {
            Unknown,
            Animation,
            Collite,
            Texture,
            Colnorm,
            Coldepth,
            Colsmooth
        }
        public abstract CallTypes CallType { get; }
        /// <summary>
        /// Parses to a <see cref="COLDefinition"/> object, see inheritors of this class for choices.
        /// </summary>
        /// <param name="MacroExpression"></param>
        /// <returns></returns>
        public static COLDefinition? Parse(ASMMacroInvokeLineStructure MacroExpression)
        {
            var param1Int = MacroExpression.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
            var param2Int = MacroExpression.TryGetParameter(1)?.TryParseOrDefault() ?? 0;
            var param1Cnt = MacroExpression.TryGetParameter(0)?.ParameterContent ?? "";
            switch (MacroExpression.MacroReference.Name.ToUpper())
            {
                // A 'diffuse' material, as in it is reactive to lighting angle
                case "COLNORM":
                    return new COLNorm(param1Int, param2Int);
                case "COLLITE":
                    return new COLLite(param1Int, param2Int);
                // The equivalent of an emissive material, as in it just doesn't reactive to light angle
                case "COLDEPTH":
                    return new COLDepth(param1Int);
                // Goes between a couple of materials between frames
                case "COLANIM":
                    return new COLAnimationReference(param1Cnt);
                // A texture reference
                case "COLTEXT":
                    return new COLTexture(param1Cnt);
                default: return null;
            }
        }
    }
}
