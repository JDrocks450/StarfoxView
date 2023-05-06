namespace StarFox.Interop.MSG
{
    /// <summary>
    /// Represents a <c>MESSAGE</c> macro call with <c>Speaker,English,SecondaryLanguage,Sound</c> parameters
    /// </summary>
    public class MSGEntry
    {
        public MSGEntry(string speaker, string english, string secondaryLanguage, string sound)
        {
            Speaker = speaker;
            English = english;
            SecondaryLanguage = secondaryLanguage;
            Sound = sound;
        }

        /// <summary>
        /// The person talking
        /// <para>This corresponds with parameter <c>0</c></para>
        /// </summary>
        public string Speaker { get; set; }
        /// <summary>
        /// What they are saying, in English
        /// <para>This corresponds with parameter <c>1</c></para>
        /// </summary>
        public string English { get; set; }
        /// <summary>
        /// What they are saying, in whatever language this file is currently translating
        /// <para>This corresponds with parameter <c>2</c></para>
        /// </summary>
        public string SecondaryLanguage { get; set; }
        /// <summary>
        /// The type of sound to make for this communication
        /// <para>This corresponds with parameter <c>3</c></para>
        /// </summary>
        public string Sound { get; set; }
    }
}
