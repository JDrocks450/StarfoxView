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
        /// Creates a new ASMFile representing the file provided at the specified path
        /// </summary>
        /// <param name="OriginalFilePath"></param>
        internal ASMFile(string OriginalFilePath)
        {
            this.OriginalFilePath = OriginalFilePath;
        }
        /// <summary>
        /// Shallow copy of One <see cref="ASMFile"/> to another
        /// </summary>
        /// <param name="From"></param>
        internal ASMFile(ASMFile From)
        {
            this.Chunks = From.Chunks;
            this.Constants = From.Constants;
            OriginalFilePath = From.OriginalFilePath;
        }
        public object GetConstantValue(string ConstantName) => 
            GetConstantValue(Constants.First(x => x.Name == ConstantName));
        public object GetConstantValue(ASMConstant constant) => constant.TryParseOrDefault();
        /// <summary>
        /// Gets the given constant value as an integer. 
        /// <para>If <see cref="int"/> is not the desired type, use: <see cref="GetConstantValue(string)"/></para>
        /// </summary>
        /// <param name="Constant">The name of the constant</param>
        /// <returns></returns>
        public int this[string Constant] => (int)GetConstantValue(Constant);
    }
}
