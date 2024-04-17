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
        PlanetRenderer planetRenderer;

        public PlanetRendererControl()
        {
            InitializeComponent();

            Loaded += delegate
            {
                Load();
                StartAnimation();
            };  
        }

        Color[,]? cacheImage;
        
        private void Load()
        {
            string imagePath = "E:\\Solutions\\repos\\StarFoxMapVisualizer\\StarFoxMapVisualizer\\Resources\\Image\\planetb.png";

            System.Drawing.Bitmap planetTexture = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(imagePath);
            planetRenderer = new PlanetRenderer(planetTexture);
        }

        public void StartAnimation(TimeSpan? FrameRate = default)
        {
            FrameRate = FrameRate ?? PlanetRenderer.GetFPSTimeSpan(60);
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
