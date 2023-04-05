namespace Starfox.Editor
{
    /// <summary>
    /// Describes the type of <see cref="SFCodeProjectNode"/>
    /// </summary>
    public enum SFCodeProjectNodeTypes
    {
        None,
        /// <summary>
        /// The base node of a <see cref="SFCodeProject"/> is this project type node
        /// </summary>
        Project,
        /// <summary>
        /// This is a folder
        /// </summary>
        Directory,
        /// <summary>
        /// This is a file
        /// </summary>
        File,
        /// <summary>
        /// This is an <see cref="SFOptimizer"/>
        /// </summary>
        Optimizer,
    }
}
