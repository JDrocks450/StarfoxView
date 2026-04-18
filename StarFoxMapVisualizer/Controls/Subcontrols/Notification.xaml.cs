using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for Notification.xaml
    /// </summary>
    public partial class Notification : ContentControl
    {
        private static Notification? OpenNotification;
        private static Timer _timer;

        public event EventHandler Dismissed;

        /// <summary>
        /// The <see cref="Action"/> attached to this notification when <see cref="Interact"/> is called
        /// </summary>
        public Action Callback { get; }
        /// <summary>
        /// How long until this notification expires from the time it was opened
        /// </summary>
        public TimeSpan Lifespan { get; } = TimeSpan.FromSeconds(5);
        public string ActionText { get; }

        public Notification()
        {
            InitializeComponent();
        }

        private Notification(Action Callback, TimeSpan Lifespan, string actionText) : this()
        {
            this.Callback = Callback;
            this.Lifespan = Lifespan;
            Content = ActionText = actionText;
        }

        /// <summary>
        /// Queues a new Notification Balloon now -- will wait for any open notifications to close themselves before continuing
        /// </summary>
        /// <param name="ActionText">3</param>
        /// <param name="Lifespan"></param>
        /// <param name="Callback"></param>
        /// <param name="Immediate">When true, will dismiss the <see cref="GetOpenNotification"/> now</param>
        internal static async Task<Notification> CreateAsync(string ActionText, TimeSpan Lifespan, Action Callback, bool Immediate = false)
        {
            Notification notification = new(Callback, Lifespan, ActionText);            
            if(OpenNotification != null)
            { // NOTIFICATION OPEN
                if (Immediate)
                    await OpenNotification.Dismiss();
                else
                {
                    await Task.Run(delegate
                    {
                        while (OpenNotification != null)
                        {
                            Task.Delay(1000);
                        }
                    });
                }
            }
            if (_timer != null) {
                _timer.Dispose();
                _timer = null;
            }
            _timer = new Timer((object sender) => notification.Dispatcher.Invoke(() => (sender as Notification)?.Dismiss()), notification, (int)Lifespan.TotalMilliseconds, 4000);
            return OpenNotification = notification;
        }

        internal void Show()
        {
            double width = Application.Current.MainWindow.ActualWidth;
            BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(width / 2, 10, width / 2, 10), new Thickness(10), TimeSpan.FromSeconds(.15)));            
        }

        /// <summary>
        /// Gets the notification that's currently open
        /// </summary>
        /// <returns></returns>
        internal Notification? GetOpenNotification()
        {
            return OpenNotification;
        }
        /// <summary>
        /// Dismisses this <see cref="Notification"/> now
        /// </summary>
        internal async Task Dismiss()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            //ANIMATION
            double width = Application.Current.MainWindow.ActualWidth;
            ThicknessAnimation anim = new ThicknessAnimation(new Thickness(10),new Thickness(width / 2, 10, width / 2, 10), TimeSpan.FromSeconds(.15));
            anim.Completed += delegate
            {
                Dismissed?.Invoke(this, null);
                OpenNotification = null;
            };
            BeginAnimation(MarginProperty, anim);            
        }
        /// <summary>
        /// Runs the attached Action Callback on this notification now
        /// </summary>
        internal async Task Interact()
        {
            Callback?.Invoke();
            await Dismiss();
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Dismiss();

        private void DockPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => Interact();

        private void ProgressBar_Loaded(object sender, RoutedEventArgs e) => 
            (sender as ProgressBar).BeginAnimation(ProgressBar.ValueProperty, new DoubleAnimation(1, 0, Lifespan));
    }
}
