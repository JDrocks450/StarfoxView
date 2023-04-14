using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM
{
    public abstract class ImporterContext<T> where T : IImporterObject
    {
        public T[] Includes { get; set; }
        public T CurrentFile { get; set; }
        public string? CurrentFilePath { get; set; }
        public int CurrentLine { get; set; }
    }
    public class ASMImporterContext : ImporterContext<ASMFile>
    {       
        public ASMImporterContext() { }
    }

    /// <summary>
    /// A custom-written basic ASM code object importer
    /// </summary>
    public class ASMImporter : CodeImporter<ASMFile>
    {
        private ASMImporterContext _context;
        internal ASMImporterContext Context => _context;
        internal ASMFile[]? CurrentIncludes => _context?.Includes;

        public ASMImporter()
        {
            _context = new()
            {
                CurrentFilePath = default,
            };
        }

        public ASMImporter(string FilePath, params ASMFile[] Imports) : this()
        {
            _context.CurrentFilePath = FilePath;
            SetImports(Imports);
            _ = ImportAsync(FilePath);
        }

        /// <summary>
        /// You can import other ASM files that have symbol definitions in them to have those symbols linked.
        /// </summary>
        /// <param name="Imports"></param>
        public override void SetImports(params ASMFile[] Imports)
        {
            _context.Includes = Imports;
        }      

        public override async Task<ASMFile> ImportAsync(string FilePath)
        {
            _context.CurrentFilePath = FilePath;  
            ASMFile newFile = _context.CurrentFile = new(FilePath);
            _context.CurrentLine = -1;
            using (var fs = File.OpenText(FilePath)) // open the file as StreamReader
            {
                while(!fs.EndOfStream) 
                {
                    var chunk = ProcChunk(FilePath, _context, fs); // process this line as a new chunk
                    if (chunk == default) // fail out if not processed (unknown chunk syntax, other errors)
                        continue; // the assembly chunk was not useful or an error, 
                    newFile.Chunks.Add(chunk); // upon success, add this chunk to the Chunks collection
                }
            }
            return ImportedObject = newFile;
        }
        /// <summary>
        /// Process the current line of the FileStream as a chunk header
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        internal static ASMChunk? ProcChunk(string FileName, ASMImporterContext Context, StreamReader fs)
        {
            var Current = Context.CurrentFile;
            var imports = Context.Includes;

            long position = fs.GetActualPosition();
            var header = fs.ReadLine().RemoveEscapes(); // read line
            Context.CurrentLine++;//increment line register                                   
            var type = ASMChunk.Conjecture(header); // investigate what it is

            ASMChunk? chunk = default;
            switch (type)
            {
                case ASMChunks.Comment:
                    chunk = new ASMComment(FileName, position)
                    {
                        Line = Context.CurrentLine
                    };
                    chunk.Parse(fs);                    
                    break;
                case ASMChunks.Macro:
                    chunk = new ASMMacro(position, Context)
                    {
                        Line = Context.CurrentLine
                    };
                    chunk.Parse(fs);
                    break;
                default:
                case ASMChunks.Unknown:
                case ASMChunks.Line:
                    chunk = new ASMLine(position, Context)
                    {
                        IsUnknownType = type is ASMChunks.Unknown,
                        Line = Context.CurrentLine
                    };
                    chunk.Parse(fs);
                    break;
            }
            if (chunk is ASMLine line) // check if this is a LINE of Assembly code
            {
                if (string.IsNullOrWhiteSpace(line.Text))// is the string empty or just spaces?
                    return null; // return nothing if this isn't useful.
            }                        
            return chunk;
        }

        internal override ImporterContext<IncludeType>? GetCurrentContext<IncludeType>()
        {
            return Context as ImporterContext<IncludeType>;
        }
    }
}
