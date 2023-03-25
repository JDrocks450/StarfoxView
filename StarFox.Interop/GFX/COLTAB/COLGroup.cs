using StarFox.Interop.GFX.COLTAB.DEF;

namespace StarFox.Interop.GFX.COLTAB
{
    /// <summary>
    /// Represents a group of Color references that can be attached to a <see cref="BSPShape"/> to give it color and/or texture.
    /// </summary>
    public class COLGroup 
    {
        /// <summary>
        /// The definitions included in this group
        /// </summary>
        public HashSet<COLDefinition> Definitions { get; } = new();

        public COLGroup(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of this group
        /// </summary>
        public string Name { get; internal set; }
        public override string ToString()
        {
            return $"{Name}: {Definitions.Count} Definitions";
        }
    }
}
