using Starfox.Editor.Settings;

namespace Starfox.Editor
{

    /// <summary>
    /// A serializable <see cref="SFCodeProject"/> that can be saved to disk that saves 
    /// Settings, Special Paths, etc. without runtime information like Open files and Includes
    /// </summary>
    [Serializable] public class SFCodeProjectState
    {
        /// <summary>
        /// Gets whether or not this project has a Shapes Directory set yet
        /// </summary>
        public bool ShapesDirectoryPathSet => ShapesDirectoryPath != default;
        /// <summary>
        /// The path to the SHAPES directory -- if this project has one set.
        /// <para>See: <see cref="ShapesDirectoryPathSet"/> to check for this scenario</para>
        /// </summary>
        public string? ShapesDirectoryPath { get; set; } = default;

        /// <summary>
        /// Public for serialization -- not recommended to make direct changes
        /// <para>See: <see cref="GetSettings{T}(SFCodeProjectSettingsTypes)"/></para>
        /// </summary>
        public Dictionary<SFCodeProjectSettingsTypes, SFEditorSettings> Settings { get; set; } =
            new Dictionary<SFCodeProjectSettingsTypes, SFEditorSettings>()
            {
                //Initializes a new Graphics user settings instance
                { SFCodeProjectSettingsTypes.Graphics, new GraphicsUserSettings() }
            };
        /// <summary>
        /// Gets the specified type of settings and casts to the provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Type">The type of settings</param>
        /// <returns></returns>
        public T GetSettings<T>(SFCodeProjectSettingsTypes Type) where T : SFEditorSettings => (T)Settings[Type];
    }
}
