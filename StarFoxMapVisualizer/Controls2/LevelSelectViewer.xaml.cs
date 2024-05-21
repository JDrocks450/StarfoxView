using StarFox.Interop.EFFECTS;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT.MSPRITES;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;
using StarFoxMapVisualizer.Renderers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Controls2
{
    public class LevelSelectViewerItem : ContentControl
    {
        const string DEFAULTPAL = "MAP_C";

        static Dictionary<string, string> LevelAssetMap = new()
        {
            { "Out of this Dimension", "starwars3" },
            { "Sector Y", "space4" },
            { "Meteor", "bigmeteo" },
            { "Sector X", "space1" },
            { "Asteroid Belt", "space2" },
            { "Sector Z", "cluster" },
            { "The Awesome Black Hole", "blackhole" },
            { "Space Armada", "bigships" },
            { "Macbeth", "planet_planetc"},
            { "Corneria", "planet_playerplanet" },
            { "Titania", "planet_planeta" },
            { "Fortuna", "planet_planetb" },
            { "Venom", "planet_enemyplanet" },
        };
        private string? _levelName;
        private string _palette = DEFAULTPAL;

        public string? LevelName
        {
            get => _levelName;
            set
            {
                _levelName = value;
                InvalidateMSprite();
            }
        }

        private PlanetRendererControl? ContentAsPlanetRenderer => (Content as PlanetRendererControl) ?? null;
        public double PlanetAxisRotation
        {
            get => ContentAsPlanetRenderer?.PlanetRotationDegrees ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.PlanetRotationDegrees = value;
            }
        }
        public double PlanetRotationalSpeedX
        {
            get => ContentAsPlanetRenderer?.Options.RotationalSpeedX ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.Options.RotationalSpeedX = value;
            }
        }
        public double SpotlightPositionX
        {
            get => ContentAsPlanetRenderer?.SpotlightPositionX ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.SpotlightPositionX = value;
            }
        }
        public double SpotlightPositionY
        {
            get => ContentAsPlanetRenderer?.SpotlightPositionY ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.SpotlightPositionY = value;
            }
        }
        public double SpotlightMaxLumination
        {
            get => ContentAsPlanetRenderer?.Options.SpotlightMaxLumination ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.Options.SpotlightMaxLumination = value;
            }
        }
        public double SpotlightMinLumination
        {
            get => ContentAsPlanetRenderer?.Options.SpotlightMinLumination ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.Options.SpotlightMinLumination = value;
            }
        }
        public double SpotlightDistance
        {
            get => ContentAsPlanetRenderer?.Options.SpotlightDistance ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.Options.SpotlightDistance = value;
            }
        }
        public double SpotlightIntensity
        {
            get => ContentAsPlanetRenderer?.Options.SpotlightIntensity ?? double.NaN;
            set
            {
                if (ContentAsPlanetRenderer != null) ContentAsPlanetRenderer.Options.SpotlightIntensity = value;
            }
        }
        public string SelectedPalette
        {
            get => _palette;
            set
            {
                if (_palette == value) return;
                _palette = value;
                Dispatcher.BeginInvoke(InvalidateMSprite);
            }
        }

        public LevelSelectViewerItem() : base()
        {

        }

        public LevelSelectViewerItem(string? levelName) : this()
        {            
            LevelName = levelName;
        }

        public async void InvalidateMSprite()
        {
            if (string.IsNullOrWhiteSpace(LevelName)) return;
            if (!LevelAssetMap.TryGetValue(LevelName, out var mSprite)) return;
            PlanetRenderer.PlanetRendererOptions? options = ContentAsPlanetRenderer?.Options;
            ContentAsPlanetRenderer?.Dispose();

            try
            {
                if (!mSprite.StartsWith("planet_"))
                    await HandleMSprite(mSprite);
                else 
                    await HandlePlanet(mSprite.Substring(7), options);
            }
            catch (Exception ex)
            {
                AppResources.ShowCrash(ex, false, "Showing the MSprite: " + mSprite);
            }
        }

        async Task HandlePlanet(string mSprite, PlanetRenderer.PlanetRendererOptions? options = default)
        {
            (System.Drawing.Bitmap image, MSprite Sprite) mSpriteImage = await SHAPEStandard.RenderMSpriteBitmap(mSprite, _palette);

            var renderControl = new PlanetRendererControl(mSpriteImage.image);
            if (options != null) renderControl.Options = options;
            renderControl.StartAnimation();

            Content = renderControl;
        }

        async Task HandleMSprite(string mSprite)
        {
            (ImageSource image, MSprite Sprite) mSpriteImage = await SHAPEStandard.RenderMSprite(mSprite, _palette);
            Content = new CopyableImage() { Source = mSpriteImage.image };
        }
    }

    /// <summary>
    /// Interaction logic for LevelSelectViewer.xaml
    /// </summary>
    public partial class LevelSelectViewer : UserControl
    {
        const string MAPNAME = "MAP", MAPPAL = "MAP_C"; // MAP VARS

        private string _mapBG = MAPNAME;
        private string _mapPal = MAPPAL;
        private string _gfxPal = MAPPAL;

        public string MapBackgroundName
        {
            get => _mapBG;
            set
            {
                _mapBG = value;
                Dispatcher.BeginInvoke(InvalidateGraphics);
            }
        }

        public string MapPalette
        {
            get => _mapPal;
            set
            {
                _mapPal = value;
                Dispatcher.BeginInvoke(InvalidateGraphics);
            }
        }

        public string GraphicsPalette
        {
            get => _gfxPal;
            set
            {
                _gfxPal = value;
                Dispatcher.BeginInvoke(InvalidateGraphics);
            }
        }

        public LevelSelectViewer()
        {
            InitializeComponent();

            Loaded += Load;
        }

        private async void Load(object sender, RoutedEventArgs e) => await InvalidateGraphics();
        
        /// <summary>
        /// Changes both the <paramref name="MapPalette"/> and <paramref name="MapTexture"/>
        /// at the same time. 
        /// <para/>Changing <see cref="MapPalette"/> or <see cref="MapBackgroundName"/> 
        /// individually calls <see cref="InvalidateGraphics"/> twice which is a waste.
        /// </summary>
        /// <param name="MapPalette"></param>
        /// <param name="MapTexture"></param>
        public void SetGraphics(string MapPalette, string GraphicsPalette, string MapTexture)
        {
            _mapBG = MapTexture;
            _mapPal = MapPalette;
            _gfxPal = GraphicsPalette;
            Dispatcher.BeginInvoke(InvalidateGraphics);
        }
        /// <summary>
        /// Causes the background and planets to be invalidated and ultimately their textures
        /// reloaded from disk.
        /// </summary>
        /// <returns></returns>
        public async Task InvalidateGraphics()
        {
            //SHOW THE MAP SCREEN            
            StarFox.Interop.GFX.CAD.CGX.GlobalContext.HandlePaletteIndex0AsTransparent = false;

            BitmapSource? source = null;
            try
            {
                //RENDER SCR
                using System.Drawing.Bitmap mapScreen = await GFXStandard.RenderSCR(MapPalette, MapBackgroundName, null, 0);
                source = mapScreen.Convert();
                //DISPOSE                
            }
            catch (Exception ex)
            { // HANDLE CRASH WITH USER MESSAGE
                AppResources.ShowCrash(ex, false, "Rendering the Level Select Screen");
            }
            finally
            {
                StarFox.Interop.GFX.CAD.CGX.GlobalContext.HandlePaletteIndex0AsTransparent = true;
            }
            if (source == null) return; // LOADING THE MAP FAILED
            MapBGImage.Source = source;

            //REDRAW GRAMPHICS
            foreach (var item in MapGraphics.Children.OfType<LevelSelectViewerItem>())
                item.SelectedPalette = GraphicsPalette;
        } 
    }
}
