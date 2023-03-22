using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM.TYP
{
    public interface IASMLineStructure
    {

    }
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
    /// <summary>
    /// Represents an ASMLine that defines a constant
    /// </summary>
    public class ASMDefineLineStructure : IASMLineStructure
    {
        public ASMDefineLineStructure(string name, string value)
        {
            Name = name;
            Value = value;
        }
        /// <summary>
        /// An importer, such as <see cref="ASMLine.Parse(StreamReader)"/> will set this if/when it creates an accompanying
        /// <see cref="ASMConstant"/> definition for this line.
        /// </summary>
        public ASMConstant Constant { get; internal set; }
        /// <summary>
        /// The name given to this Constant
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The text found at the callsite
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Tries to parse this line as a macro invokation
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Reference"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static bool TryParse(string input, out ASMDefineLineStructure result)
        {
            var originalText = input;
            input = input.NormalizeFormatting();
            var blocks = input.Split(' ');
            result = default;
            if (blocks.Length <= 0) return false;
            if (blocks.Length > 2 && blocks[1].ToLower().Contains("equ"))
            { // define found
                var name = blocks[0];
                var value = blocks[2];
                result = new ASMDefineLineStructure(name, value);
                return true;
            }
            return false;
        }
    }
    /// <summary>
    /// Represents an ASMLine that invokes a macro expression
    /// </summary>
    public class ASMMacroInvokeLineStructure : IASMLineStructure
    {
        public ASMMacroInvokeLineStructure(ASMMacro MacroReference, params ASMMacroInvokeParameter[] Parameters)
        {
            this.MacroReference = MacroReference;
            this.Parameters = Parameters;
        }
        /// <summary>
        /// The macro function definition that was called in this expression
        /// </summary>
        public ASMMacro MacroReference { get; }
        /// <summary>
        /// The parameters passed if applicable
        /// </summary>
        public ASMMacroInvokeParameter[] Parameters { get; }
        public ASMMacroInvokeParameter? TryGetParameter(int index) => Parameters.ElementAtOrDefault(index);

        /// <summary>
        /// Tries to parse this line as a macro invokation
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Reference"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static bool TryParse(string input, out ASMMacroInvokeLineStructure result, params ASMFile[] Reference)
        {
            var originalText = input;
            input = input.TrimStart().TrimEnd();
            var blocks = input.Split(' ');
            result = default;
            if (blocks.Length <= 0) return false;
            var macro = SymbolOperations.MatchMacro(Reference, blocks[0]);
            if (macro == default) return false;
            ASMMacroInvokeParameter[]? parameters = { };
            if (blocks.Length > 1) // parameters?
            {
                var paramText = input.Substring(input.IndexOf(' ') + 1).Replace(" ","");
                //var paramText = blocks[1];
                var paramList = paramText.Split(',');
                if (paramList.Length > 0 && !string.IsNullOrWhiteSpace(paramList[0])) {
                    parameters = new ASMMacroInvokeParameter[paramList.Length];
                    for (int i = 0; i < paramList.Length; i++)
                    {
                        var paramCurrent = paramList[i];
                        string? paramName = default;
                        if (macro.Parameters.Length > i)
                            paramName = macro.Parameters[i];
                        parameters[i] = new ASMMacroInvokeParameter(paramCurrent, paramName);
                    }
                }
            }
            result = new ASMMacroInvokeLineStructure(macro, parameters);
            return true;
        }
    }
    public class ASMLine : ASMChunk
    {
        private readonly ASMFile[] imports;
        private readonly ASMImporterContext context;

        /// <summary>
        /// A flag that represents whether this line is believed to be invalid or of unknown type
        /// </summary>
        public bool IsUnknownType { get; set; }
        public string Text { get; private set; } = "";
        public override ASMChunks ChunkType => ASMChunks.Line;        

        /// <summary>
        /// If the structure of this line is recognized and well formatted, this will be populated, otherwise null.
        /// </summary>
        public IASMLineStructure? Structure { get; private set; }
        public bool HasStructureApplied => Structure != null;
        public ASMMacroInvokeLineStructure StructureAsMacroInvokeStructure => Structure as ASMMacroInvokeLineStructure;
        public ASMDefineLineStructure StructureAsDefineStructure => Structure as ASMDefineLineStructure;

        /// <summary>
        /// Creates a new ASMLine instance and references the Current file being imported and any imports for symbol linking
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Position"></param>
        /// <param name="Imports"></param>
        internal ASMLine(long Position, ASMImporterContext context)
        {
            OriginalFileName = context.CurrentFilePath;
            this.Position = Position;
            this.context = context;
            imports = context.Includes;
            Array.Resize(ref imports, imports.Length + 1);
            imports[imports.Length - 1] = context.CurrentFile;
        }

        public override void Parse(StreamReader FileStream)
        {
            InitStream(FileStream);
            var line = FileStream.ReadLine().RemoveEscapes();
            var newPosition = FileStream.GetActualPosition();
            if (ASMDefineLineStructure.TryParse(line, out var result))
            {
                Structure = result;
                var constant = new ASMConstant(this);
                result.Constant = constant;
                context.CurrentFile.Constants.Add(constant);
            }
            else if (ASMMacroInvokeLineStructure.TryParse(line, out var mresult, imports))
                Structure = mresult;
            Text = line;
            Length = newPosition - Position;
        }
    }
}
