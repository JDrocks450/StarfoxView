using StarFox.Interop.ASM;

namespace StarFox.Interop.GFX.COLTAB
{
    /// <summary>
    /// A file containing a color table, which is a set of macro functions that describe colors in different named blocks
    /// </summary>
    public class COLTABFile : ASMFile
    {
        /// <summary>
        /// The groups added to this file, sorted by name
        /// </summary>
        public Dictionary<string, COLGroup> Groups { get; } = new();        
        /// <summary>
        /// Creates a new <see cref="COLTABFile"/> instance with no groups.
        /// </summary>
        /// <param name="originalFilePath"></param>
        public COLTABFile(string originalFilePath) : base(originalFilePath)
        {

        }
        /// <summary>
        /// Creates a new <see cref="COLTABFile"/> instance with no groups.
        /// </summary>
        /// <param name="originalFilePath"></param>
        public COLTABFile(ASMFile From) : base(From)
        {

        }

        public string OriginalFilePath { get; }
        /// <summary>
        /// Gets a group by the given name, not case sensitive.
        /// <para>null when the group wasn't found.</para>
        /// </summary>
        /// <param name="Name">The name of the group</param>
        /// <returns></returns>
        public COLGroup? GetGroup(string Name) => Groups.FirstOrDefault(x => x.Key.ToUpper() == Name.ToUpper()).Value;
        /// <summary>
        /// See: <see cref="GetGroup(string)"/>
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool TryGetGroup(string Name, out COLGroup? group)
        {
            group = GetGroup(Name);
            return group != null;
        }
    }
}
