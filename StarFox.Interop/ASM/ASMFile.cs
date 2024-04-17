using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM
{
    public class ASMFile : IImporterObject
    {        
        public string OriginalFilePath { get; protected set;  }
        /// <summary>
        /// Chunks added to this object through the <see cref="ASMImporter"/>
        /// <para>Not recommended for the caller to manipulate this collection unless through an <see cref="CodeImporter{T}"/></para>
        /// </summary>
        public HashSet<ASMChunk> Chunks { get; protected set; } = new HashSet<ASMChunk>();
        /// <summary>
        /// Constant Definitions are kept separately from <see cref="Chunks"/>, as <see cref="ASMLine"/> objects defining
        /// constants have <see cref="ASMDefineLineStructure"/> applied, and can be found in <see cref="Chunks"/>
        /// </summary>
        public HashSet<ASMConstant> Constants { get; protected set; } = new HashSet<ASMConstant> { };
        /// <summary>
        /// A macro for getting all <see cref="Chunks"/> that are <see cref="ASMLine"/> instances
        /// </summary>
        public IEnumerable<ASMLine> Lines => Chunks.OfType<ASMLine>();
        /// <summary>
        /// A macro for getting all <see cref="Chunks"/> that are <see cref="ASMLine"/> instances and have 
        /// <see cref="ASMLine.StructureAsMacroInvokeStructure"/> set
        /// </summary>
        public IEnumerable<ASMMacroInvokeLineStructure> MacroInvokeLines => Lines.Select(x => x.StructureAsMacroInvokeStructure).
            Where(y => y != default);
        /// <summary>
        /// Creates a new ASMFile representing the file provided at the specified path
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        internal ASMFile(string OriginalFilePath)
        {
            this.OriginalFilePath = OriginalFilePath;
        }
        /// <summary>
        /// Shallow copy of one <see cref="ASMFile"/> to another
        /// </summary>
        /// <param name="From"></param>
        internal ASMFile(ASMFile From)
        {
            this.Chunks = From.Chunks;
            this.Constants = From.Constants;
            OriginalFilePath = From.OriginalFilePath;
        }
        /// <summary>
        /// Returns whether a constant was found by name in the current file
        /// </summary>
        /// <param name="ConstantName"></param>
        /// <returns></returns>
        public bool ConstantExists(string ConstantName) => Constants.Select(x => x.Name).Contains(ConstantName);
        /// <summary>
        /// Gets the value of the given constant by name.
        /// <para/>Will throw an exception if not found, use <see cref="ConstantExists(string)"/> to be safe
        /// </summary>
        /// <param name="ConstantName"></param>
        /// <returns></returns>
        public string? GetConstantValue(string ConstantName) => 
            GetConstantValue(Constants.First(x => x.Name == ConstantName));
        /// <summary>
        /// Gets the value of the given constant by name.
        /// <para/>Will throw an exception if not found, use <see cref="ConstantExists(string)"/> to be safe
        /// </summary>
        /// <param name="ConstantName"></param>
        /// <returns></returns>
        public string? GetConstantValue(ASMConstant constant) => constant.Value;

        public int GetConstantNumericValue(ASMConstant constant, params ASMFile[] Includes)
        {
            ASMExtensions.BeginConstantsContext(Includes);
            int value = constant.TryParseOrDefault();
            ASMExtensions.EndConstantsContext();
            return value;
        }

        /// <summary>
        /// Gets the given constant value as an integer. 
        /// <para/>Will throw an exception if not found. Use <see cref="ConstantExists(string)"/> to be safe.
        /// <para>If <see cref="int"/> is not the desired type, use: <see cref="GetConstantValue(string)"/></para>
        /// </summary>
        /// <param name="Constant">The name of the constant</param>
        /// <returns></returns>
        public int this[string Constant] => ASMExtensions.TryParseOrDefault(GetConstantValue(Constant));
    }
}
