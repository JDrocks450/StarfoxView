using StarFox.Interop.BRR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// <summary>
    /// Interaction logic for WaveFormControl.xaml
    /// </summary>
    public partial class WaveFormControl : ContentControl
    {
        /// <summary>
        /// Multiplied by the width of the control dictates how many samples to display.
        /// <para>Default is 1.0 -- as in if the control is 200px wide it will display 200 samples as 1px wide lines</para>
        /// </summary>
        public double Precision = 1.0;

        public WaveFormControl()
        {
            InitializeComponent();
        }

        public WaveFormControl(BRRSample Sample) : this()
        {
            Loaded += delegate
            {
                Display(Sample);
            };            
        }

        public void Display(BRRSample Sample)
        {
            WaveFormHost.Children.Clear();
            double widthMeasurement = HOST.ActualWidth == 0 ? HOST.Width : HOST.ActualWidth;
            double heightMeasurement = HOST.ActualHeight == 0 ? HOST.Height : HOST.ActualHeight;
            double halfDesignHeight = 100; // distance from median to top / bottom of control
            double designWidth = 1;
            double totalSamples = (int)(Math.Max(widthMeasurement, Sample.SampleData.Count) * Precision);
            double step = totalSamples / widthMeasurement;            
            short HighBound = Sample.SampleData.Max();
            short LowBound = 0;
            int distance = HighBound - LowBound;
            int currentX = -1;
            List<int> addedSamples = new();
            void AddPoint(int index)
            {
                currentX++;
                var dataPoint = Sample.SampleData[index];
                if (dataPoint == short.MaxValue || 
                    dataPoint == short.MinValue) 
                    return;
                var value = Math.Abs(dataPoint);
                var Percentage = (double)value / distance;
                var lineHeight = Percentage * halfDesignHeight;
                lineHeight *= 2;
                Rectangle rect = new Rectangle()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(currentX, 0, 0, 0),
                    Width = designWidth,
                    Height = lineHeight,
                    //Fill = Brushes.Red,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                WaveFormHost.Children.Add(rect);
            }
            for(double i = 0; i < Sample.SampleData.Count-1; i += step)
            {
                var index = (int)i;
                if (addedSamples.Contains(index)) continue;
                addedSamples.Add(index);
                AddPoint(index);
            }
            currentX++;
            AddPoint(Sample.SampleData.Count - 1);
            WaveFormHost.Height = halfDesignHeight;
            WaveFormHost.Width = totalSamples;
            WaveFormHost.LayoutTransform = new ScaleTransform(
                scaleX: widthMeasurement / currentX,
                scaleY: heightMeasurement / (halfDesignHeight)
            );
        }
    }
}
