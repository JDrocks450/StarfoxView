using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF.UI.Extensions.Backgrounds
{    
    /// <summary>
    /// Interaction logic for SnowFallPanel.xaml
    /// </summary>
    public partial class SnowFallPanel : UserControl
    {
        internal static Random SnowFallRandom = new Random();
        
        public double SPEED;
        public int TileSize; 
        List<Image> SnowControls = new List<Image>();

        public SnowFallPanel() : this(TimeSpan.Zero)
        {
            
        }

        public SnowFallPanel(TimeSpan AnimationDelay, int TileSize = 75, double Speed = 5)
        {
            if (AnimationDelay == default) AnimationDelay = TimeSpan.FromSeconds(0);
            SPEED = Speed;            
            InitializeComponent();
            Loaded += delegate
            {
                TiledBG.Viewport = new Rect(0, 0, TileSize, TileSize);
                if (AnimationDelay.TotalSeconds != 0) {
                    var task = Task.Delay(AnimationDelay.Milliseconds);
                    task.ContinueWith(delegate { Dispatcher.Invoke(Init); });
                    return;
                }
                Init();
            };
        }

        private void Init()
        {
            Double UNIF_VAL = Math.Max(SnowGrid.ActualWidth, SnowGrid.ActualHeight);
            TRANSLATION.BeginAnimation(TranslateTransform.XProperty, GetBackgroundAnimation(0, -(int)UNIF_VAL, SPEED));
            TRANSLATION.BeginAnimation(TranslateTransform.YProperty, GetBackgroundAnimation(0, (int)UNIF_VAL, SPEED));
            SnowGrid.Margin = new Thickness(0, -UNIF_VAL, -UNIF_VAL, 0);
        }

        private DoubleAnimation GetBackgroundAnimation(int FromValue, int ToValue, double Time)
        {
            return new DoubleAnimation(FromValue, ToValue, TimeSpan.FromSeconds(Time))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        #region PERFORMANCE DISASTER

        private void BatchFill(ushort Amount)
        {

        }

        private DoubleAnimationUsingPath GetAnimationTimeline(Point startPoint, Point endpoint)
        {
            DoubleAnimationUsingPath timeline = new DoubleAnimationUsingPath();                        
            PathFigure figure = new PathFigure()
            {
                StartPoint = startPoint,
                IsClosed = false
            };
            timeline.PathGeometry = new PathGeometry();
            figure.Segments.Add(
                new BezierSegment(
                    new Point(startPoint.X - 25, startPoint.Y + 100), 
                    new Point(startPoint.X, startPoint.Y + 300),
                    endpoint,
                    false));
            timeline.PathGeometry.Figures.Add(figure);
            return timeline;
        }

        private void ApplyAnimation(Image control, Point startPoint)
        {
            var transform = new TranslateTransform();
            RegisterName("snowPieceTransform", transform);
            control.RenderTransform = transform;
            int far = (int)SnowGrid.ActualHeight;
            if (far == 0)
                far = 700;
            Point endpoint = new Point(startPoint.X - 75, far + 75);
            Storyboard anim = new Storyboard()
            {                
                Duration = TimeSpan.FromSeconds(5),
            };
            var xTimeline = GetAnimationTimeline(startPoint, endpoint);
            xTimeline.Source = PathAnimationSource.X;
            Storyboard.SetTargetName(xTimeline, "snowPieceTransform");
            Storyboard.SetTargetProperty(xTimeline,
                new PropertyPath(TranslateTransform.XProperty));
            xTimeline.Freeze();
            anim.Children.Add(xTimeline);   
            xTimeline = GetAnimationTimeline(startPoint, endpoint);
            xTimeline.Source = PathAnimationSource.Y;
            Storyboard.SetTargetName(xTimeline, "snowPieceTransform");
            Storyboard.SetTargetProperty(xTimeline,
                new PropertyPath(TranslateTransform.YProperty));
            xTimeline.Freeze();
            anim.Children.Add(xTimeline);   
            anim.Begin(control, false);
        }

        private Image GetSnowControl(out Point location)
        {
            Image snowControl = new Image();
            SnowGrid.Children.Add(snowControl);
            //get random location on the top edge of the control
            int far = (int)SnowGrid.ActualWidth;
            if (far == 0)
                far = 1000;
            location = new Point(SnowFallRandom.Next(0, far), -50);
            snowControl.Margin = new Thickness(location.X, location.Y, 0, 0);
            return snowControl;
        }
        #endregion
    }
}
