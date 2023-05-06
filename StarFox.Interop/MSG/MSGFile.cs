using StarFox.Interop.ASM;

namespace StarFox.Interop.MSG
{
    /// <summary>
    /// Contains <see cref="MSGEntry"/> objects that represent the messages contained within the provided file
    /// </summary>
    public class MSGFile : ASMFile
    {
        /// <summary>
        /// Creates a blank <see cref="MSGFile"/> from with the given file path.
        /// </summary>
        /// <param name="FilePath"></param>
        public MSGFile(string FilePath) : base(FilePath)
        {

        }
        /// <summary>
        /// Clones from an existing <see cref="ASMFile"/> instance
        /// </summary>
        /// <param name="Base"></param>
        public MSGFile(ASMFile Base) : base(Base)
        {

        }
        /// <summary>
        /// The message entries in this file
        /// </summary>
        public Dictionary<int, MSGEntry> Entries { get; } = new();
    }
}
