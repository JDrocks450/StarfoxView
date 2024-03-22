using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for CrashWindow.xaml
    /// </summary>
    public partial class CrashWindow : Window
    {
        static string[] Messages =
        {
            "sorry about this",
            "again? really?",
            "this must be aggravating",
            "i apologize",
            "try not doing that",
            "found a problem",
            "that's an issue",
            "this probably shouldn't happen",
            "an error occured.",
            "sample text"
        };

        public CrashWindow(Exception exception, bool Fatal = true, string Tip = "Looks like StarfoxView crashed.")
        {            
            InitializeComponent();

            //setup dialog
            NonfatalButton.Visibility = Visibility.Collapsed;
            MessageBlock.Inlines.InsertBefore(MessageBlock.Inlines.FirstInline, new Run(Tip));
            if (!Fatal)
            {
                Background = Brushes.Gray;
                NonfatalButton.Visibility = Visibility.Visible;
                CloseButton.Visibility = Visibility.Collapsed;
                ContinueButton.Visibility = Visibility.Collapsed;
            }
            RandomTitle();
            ErrorBox.Text = exception.ToString();
        }

        void RandomTitle()
        {
            var titleIndex = Random.Shared.Next(0, Messages.Length - 1);
            Title = Messages[titleIndex].ToUpper();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= true;
            Close();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //easter egg i guess
            RandomTitle();
        }
    }
}
