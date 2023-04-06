namespace StarFox.Interop.ASM.TYP.STRUCT
{
    /// <summary>
    /// Provides an interface for a parameter called in a MacroInvokeExpression
    /// </summary>
    public class ASMMacroInvokeParameter
    {
        /// <summary>
        /// The name of this parameter, from provided comments or other documentation
        /// </summary>
        public string? ParameterName;
        /// <summary>
        /// The text found at the callsite where this parameter is found
        /// </summary>
        public string ParameterContent;

        public ASMMacroInvokeParameter(string parameterContent, string? parameterName = default)
        {
            ParameterName = parameterName;
            ParameterContent = parameterContent;
        }        
    }
}
