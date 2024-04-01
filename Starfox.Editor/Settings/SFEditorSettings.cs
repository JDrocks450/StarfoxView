using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starfox.Editor.Settings
{
    /// <summary>
    /// Base interface for different settings types
    /// </summary>
    public abstract class SFEditorSettings
    {
        public abstract SFCodeProjectSettingsTypes SettingsType { get; }

        /// <summary>
        /// This is called for all Settings subscribers when a setting is changed this is fired      
        /// </summary>
        /// <param name="SettingsTypeChanged"></param>
        /// <param name="ChangedObject"></param>
        public delegate void OnSettingsUpdatedHandler(SFCodeProjectSettingsTypes SettingsTypeChanged, SFEditorSettings ChangedObject);
        /// <summary>
        /// This is called for all Settings subscribers when a setting is changed this is fired      
        /// </summary>
        /// <param name="SettingsTypeChanged"></param>
        /// <param name="ChangedObject"></param>
        public event OnSettingsUpdatedHandler? SettingsChanged = default;
        /// <summary>
        /// Fires the <see cref="SettingsChanged"/> event for all subscribers to update their settings
        /// </summary>
        public void ApplyChanges() => SettingsChanged?.Invoke(SettingsType, this);        
    }

    /// <summary>
    /// A setting that goes between two ranges of numbers
    /// </summary>
    public class RangedUserSetting
    {
        public RangedUserSetting(double value, double maxValue = 1, double minValue = 0)
        {
            Value = value;
            MaxValue = maxValue;
            MinValue = minValue;
        }

        public double Value { get; set; } = 0;
        public double MaxValue { get; set; } = 1.0;
        public double MinValue { get; set; } = 0;

        public double AsPercentage => Value / MaxValue;
    }

    /// <summary>
    /// User Settings relating to Graphics preferences
    /// </summary>
    public class GraphicsUserSettings : SFEditorSettings
    {
        /// <summary>
        /// The default <see cref="ViewingDistance3D"/>
        /// </summary>
        public const double DEFAULT_ZCLIPSCENEDIST = 15000;
        /// <summary>
        /// The default <see cref="ViewingDistance3D"/>
        /// </summary>
        public const double DEFAULT_ZCLIPFIXED = 15000;
        /// <summary>
        /// The default <see cref="ViewingDistance3D"/>
        /// </summary>
        public const double MAX_ZCLIPDIST = 100000;

        /// <summary>
        /// The distance from the virtual camera in a 3D scene the Far-Plane is (Viewing distance, basically)
        /// <para/> This is specifically for 3D scenes where the User can move the camera throughout a level
        /// </summary>
        public RangedUserSetting ViewingDistance3D { get; set; } = new RangedUserSetting(DEFAULT_ZCLIPSCENEDIST, MAX_ZCLIPDIST, 1000);
        /// <summary>
        /// The distance from the virtual camera in a 3D scene the Far-Plane is (Viewing distance, basically)
        /// <para/> This is specifically for 3D scenes where the camera is fixed looking at a model
        /// </summary>
        public RangedUserSetting ViewingDistance3DModelViewer { get; set; } = new RangedUserSetting(DEFAULT_ZCLIPFIXED, MAX_ZCLIPDIST, 500);
        /// <summary>
        /// The speed in any direction the camera moves at low speed
        /// </summary>
        public RangedUserSetting Scene3DSpeed { get; set; } = new RangedUserSetting(100, 500, 1);
        public double Scene3DFastSpeedMultiplier { get; set; } = 2;
        /// <summary>
        /// The speed in any direction the camera moves at high speed See: <see cref="Scene3DFastSpeedMultiplier"/>
        /// </summary>
        public double Scene3DFastSpeed => Scene3DSpeed.Value * Scene3DFastSpeedMultiplier;
        /// <summary>
        /// The Field of View for the virtual camera
        /// <para/> This is specifically for 3D scenes where the camera is moving around a map
        /// </summary>
        public RangedUserSetting Scene3DFieldOfView { get; set; } = new RangedUserSetting(75, 200, 25);

        public override SFCodeProjectSettingsTypes SettingsType => SFCodeProjectSettingsTypes.Graphics;

        public RangedUserSetting AnimationFPS { get; set; } = new RangedUserSetting(20, 120, 1);
    }
}
