using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM.TYP
{
    /// <summary>
    /// Represents a parsed line from a <see cref="ASMFile"/>
    /// </summary>
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
        public ASMLabelStructure StructureAsLabelStructure => Structure as ASMLabelStructure;
        
        public string? InlineLabel { get; private set; }
        public bool HasInlineLabel => InlineLabel == default;

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
            var parseLine = line;
            if (parseLine.Contains(';')) parseLine = parseLine.Substring(0, parseLine.IndexOf(';'));
            if (parseLine.NormalizeFormatting().StartsWith('.')) // looks like a label
            {//label
                var inlineStart = parseLine.IndexOf(';') + 1;
                if (inlineStart != -1) // literally what
                {
                    var inlineEnd = parseLine.IndexOf(' '); // find next space
                    if (inlineEnd > inlineStart)
                    {
                        InlineLabel = parseLine.Substring(inlineStart, inlineEnd);
                        parseLine = parseLine.Substring(inlineEnd); // set the line equal to everything else
                    }
                    else
                    {
                        InlineLabel = parseLine.Substring(inlineStart);
                        parseLine = "";
                    }
                }
            }            
            var newPosition = FileStream.GetActualPosition();
            if (!string.IsNullOrWhiteSpace(parseLine))
            {
                if (ASMDefineLineStructure.TryParse(parseLine, out var result))
                {
                    Structure = result;
                    var constant = new ASMConstant(this);
                    result.Constant = constant;
                    context.CurrentFile.Constants.Add(constant);
                }
                else if (ASMMacroInvokeLineStructure.TryParse(parseLine, out var mresult, imports))
                    Structure = mresult;
                else if (ASMLabelStructure.TryParse(parseLine, out var lresult))
                    Structure = lresult;
            }
            Text = line;
            Length = newPosition - Position;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
