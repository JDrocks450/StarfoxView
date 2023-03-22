using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM.TYP
{
    /// <summary>
    /// A comment in an <see cref="ASMFile"/>
    /// </summary>
    public class ASMMacro : ASMChunk, IASMNamedSymbol
    {
        private readonly ASMImporterContext context;

        /// <summary>
        /// If this macro is not a macro, or invalid this is false
        /// </summary>
        public bool IsValid { get; private set; } = false;
        public string Name { get; private set; } = "";
        public string[] Parameters { get; private set; } = new string[0];
        public ASMChunk[] Lines { get; private set; } = { };

        internal ASMMacro(long Position, ASMImporterContext Context) 
        {
            OriginalFileName = Context.CurrentFilePath;
            this.Position = Position;
            this.Length = 0;
            context = Context;
        }
        public override ASMChunks ChunkType => ASMChunks.Macro;
        /// <summary>
        /// Looks at the given chunk header to see if this is a Macro
        /// </summary>
        /// <param name="Header"></param>
        /// <returns></returns>
        public static bool CheckMacroHeader(string Header)
        {
            var headerLine = Header;
            headerLine = headerLine.TrimStart().TrimEnd();
            while (headerLine.Contains("  ")) // recursive remove unnecessary spaces
                headerLine = headerLine.Replace("  ", " ");
            var blocks = headerLine.Split(' ');
            if (blocks.Length < 2) // not valid formatting            
                return false;            
            if (blocks[1].ToLower() != "macro") // not a macro
                return false;
            return true;
        }

        /// <summary>
        /// Parses 
        /// </summary>
        /// <param name="FileStream"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override void Parse(StreamReader FileStream)
        {
            void fail()
            {
                IsValid= false;
            }
            InitStream(FileStream); // move stream position            
            var headerLine = FileStream.ReadLine(); // read the line            
            long newPosition = FileStream.GetActualPosition();
            long runningLength = newPosition - Position; // start tracking the length of this block
            headerLine = headerLine.RemoveEscapes().TrimStart().TrimEnd();
            while (headerLine.Contains("  ")) // recursive remove unnecessary spaces
                headerLine = headerLine.Replace("  ", " ");
            var blocks = headerLine.Split(' ');
            if (blocks.Length < 2) // not valid formatting
            {
                fail();
                return;
            }
            Name = blocks[0]; // split by spaces, take the first word
            if (blocks[1].ToLower() != "macro") // not a macro
            {
                fail();
                return;
            }
            if (blocks.Length > 2) // more text after macro keyword suggests parameters
            {
                var paramStart = headerLine.ToLower().IndexOf("macro "); // find where parameters start
                var parameterText = headerLine.Substring(paramStart + 6); // take all text past macro
                var parameters = parameterText.Split(","); // split by commas
                Parameters = parameters.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(); // take all non-empty parameter names
            }
            var lines = new ASMChunk[0];
            while (!FileStream.EndOfStream)
            {
                long currentPosition = FileStream.GetActualPosition(); // store position
                var line = FileStream.ReadLine().RemoveEscapes(); // PEEK at the line
                runningLength+= FileStream.GetActualPosition() - currentPosition;
                FileStream.BaseStream.Seek(currentPosition, SeekOrigin.Begin); // move it back to where it was to complete the PEEK
                FileStream.DiscardBufferedData();
                var module = ASMImporter.ProcChunk(OriginalFileName, context, FileStream); // check each line to see what it is
                Array.Resize(ref lines, lines.Length+1);
                lines[lines.Length-1] = module;
                if (line.ToLower().Contains("endm")) // end macro spotted
                    break;
            }
            Lines = lines;
            Length = runningLength;
        }
    }
}
