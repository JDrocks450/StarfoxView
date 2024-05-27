using StarFox.Interop.EFFECTS;
using StarFoxMapVisualizer.Misc;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Controls;

namespace StarFoxMapVisualizer.Renderers
{
    /// <summary>
    /// Interaction logic for PlanetRenderer.xaml
    /// </summary>
    public partial class PlanetRendererControl : UserControl, IDisposable
    {
        public double DefaultFrameRate = 8;

        PlanetRenderer planetRenderer;

        public PlanetRenderer.PlanetRendererOptions Options
        {
            get => planetRenderer.Options; 
            set => planetRenderer.Options = value;
        }
        public double SpotlightPositionX
        {
            get => planetRenderer.Options.SpotlightPositionX;
            set => planetRenderer.Options.SpotlightPositionX = value;
        }
        public double SpotlightPositionY
        {
            get => planetRenderer.Options.SpotlightPositionY;
            set => planetRenderer.Options.SpotlightPositionY = value;
        }
        public double PlanetRotationDegrees
        {
            get => PlanetRotation.Angle;
            set => PlanetRotation.Angle = value;
        }

        public PlanetRendererControl() : base()
        {
            InitializeComponent();
            planetRenderer = new PlanetRenderer();
            Unloaded += PlanetRendererControl_Unloaded;
        }

        private void PlanetRendererControl_Unloaded(object sender, System.Windows.RoutedEventArgs e) => Dispose();

        public PlanetRendererControl(Bitmap PlanetTexture, PlanetRenderer.PlanetRendererOptions? Options = null) : this() => Load(PlanetTexture, Options);

        public void Load(Bitmap PlanetTexture, PlanetRenderer.PlanetRendererOptions? Options = null) => planetRenderer.LoadTexture(PlanetTexture, Options);

        public void StartAnimation(TimeSpan? FrameRate = default)
        {
            FrameRate = FrameRate ?? PlanetRenderer.GetFPSTimeSpan(DefaultFrameRate);
            if (planetRenderer == null) throw new NullReferenceException("PlanetRenderer is null, call Load first.");
            planetRenderer.StartAsync((Bitmap bmp) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    RenderImage.Source = bmp.Convert();
                    bmp.Dispose();
                });
            }, false, FrameRate, TimeSpan.Zero);
        }

        public void Dispose()
        {
            planetRenderer?.Dispose();
        }
    }
}
