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
        /// <summary>
        /// Will attempt to parse the content of this parameter as an integer.
        /// <para>If the content contains a $, it is assumed to be hex.</para>
        /// </summary>
        /// <returns></returns>
        public int TryParseOrDefault()
        {
            var content = ParameterContent;
            if (string.IsNullOrEmpty(content)) return 0;
            if (content.Contains("$")) return TryParseHexOrDefault();
            if (int.TryParse(content, out int result)) { return result; }
            return 0;
        }
        /// <summary>
        /// See: <see cref="TryParseOrDefault"/>
        /// </summary>
        /// <returns></returns>
        internal int TryParseHexOrDefault()
        {
            var content = ParameterContent;
            if (string.IsNullOrEmpty(content)) return 0;
            return Convert.ToInt32(content.Replace("$", ""), 16);
        }
    }
}
