using System.Numerics;
using System;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows;
using StarFox.Interop.MAP.CONTEXT;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarFoxMapVisualizer.Misc;
using System.Windows.Media;

namespace StarFoxMapVisualizer.Renderers
{
    /// <summary>
    /// Interaction logic for BackgroundSkyRenderer.xaml
    /// </summary>
    public partial class BackgroundSkyRenderer : UserControl, ISCRRendererBase
    {
        const int RENDER_W = 224, RENDER_H = 192,
                  SCR_W = 512, SCR_H = 512;

        double BackgroundX, BackgroundY, ViewportWidth, ViewportHeight;

        public MAPContextDefinition? LevelContext { get; set; }
        public Dictionary<string, string> ReferencedFiles { get; } = new();

        public BackgroundSkyRenderer()
        {
            InitializeComponent();

            BackgroundX = (SCR_W / 2) - (RENDER_W / 2);
            BackgroundY = (SCR_H / 2) - (RENDER_H / 2);
            ViewportWidth = SCR_W;
            ViewportHeight = SCR_H;

            UpdateViewport();
        }
        public BackgroundSkyRenderer(MAPContextDefinition levelContext) : this()
        {
            LevelContext = levelContext;

            _ = SetContext(levelContext);
        }

        void UpdateViewport()
        {
            BackgroundBrush.Viewbox = new System.Windows.Rect(
                0, 0, ViewportWidth, ViewportHeight);
            BackgroundBrush.Viewport = new System.Windows.Rect(
                BackgroundX, -BackgroundY, SCR_W, SCR_H);
        }

        public void ScrollToCamera(PerspectiveCamera Camera)
        {
            //BackgroundX = (SCR_W / 2) - (RENDER_W / 2) + (-LookAt.X * SCR_W);

            var LookAt = Camera.LookDirection;
            double FOV = Camera.FieldOfView;

            ViewportWidth = SCR_W;
            ViewportHeight = SCR_H;

            double halfSCRW = SCR_W / 2, screenYBound = SCR_H - RENDER_H;

            Point pixelpos = new Point(LookAt.Z, LookAt.X);
            double xRotation = Math.Atan2(pixelpos.Y, pixelpos.X) * (FOV / 100);
            double yRotation = -LookAt.Y * .5;

            double YOffset = LevelContext?.BG2.VerticalOffset ?? (.235 * SCR_H);            

            double desiredY = ((SCR_H + YOffset) / 2) - (RENDER_H / 2) + (yRotation * screenYBound);
            if (desiredY > screenYBound)
                ViewportHeight -= desiredY - screenYBound;

            BackgroundX = SCR_W + (xRotation * halfSCRW);
            BackgroundY = desiredY;
            UpdateViewport();
        }

        public async Task SetContext(MAPContextDefinition? SelectedContext, bool ExtractCCR = false, bool ExtractPCR = false)
        {
            LevelContext = SelectedContext;

            BackgroundX = (SCR_W / 2) - (RENDER_W / 2);
            BackgroundY = (SCR_H / 2) - (RENDER_H / 2);
            ViewportWidth = SCR_W;
            ViewportHeight = SCR_H;

            UpdateViewport();

            if (LevelContext != null)
            {
                using (var scr = await GFXStandard.RenderSCR(
                SelectedContext.BackgroundPalette,
                SelectedContext.BG2ScrFile,
                SelectedContext.BG2ChrFile))
                    BackgroundBrush.ImageSource = scr.Convert();
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            }
        }
    }
}
