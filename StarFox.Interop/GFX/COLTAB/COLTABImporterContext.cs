using StarFox.Interop.ASM;

namespace StarFox.Interop.GFX.COLTAB
{
    /// <summary>
    /// The context attached to a <see cref="COLTABImporter"/> instance
    /// </summary>
    public class COLTABImporterContext : ImporterContext<ASMFile>
    {        
        /// <summary>
        /// The currently importing group
        /// </summary>
        internal COLGroup? CurrentGroup { get; set; }
        /// <summary>
        /// Gets <see cref="ImporterContext{T}.CurrentFile"/> <see langword="as"/> <see cref="COLTABFile"/>
        /// <para>This being null means there is undefined behavior in the importer as the CurrentFile should always be of this type.</para>
        /// </summary>
        /// <returns></returns>
        internal COLTABFile GetCurrentFile() => CurrentFile as COLTABFile;
        /// <summary>
        /// Starts a new <see cref="COLGroup"/> and sets the <see cref="CurrentGroup"/> property to the new group.
        /// </summary>
        /// <param name="Name"></param>
        internal void StartNewGroup(string Name)
        {
            CurrentGroup = new COLGroup(Name);
            while (GetCurrentFile().Groups.ContainsKey(Name)) // UHHH guess that one already exists? 
                Name += '_'; // add an underscore as a placeholder
            GetCurrentFile().Groups.Add(Name,CurrentGroup); // add this group to the register
        }
    }
}
