using StarFoxMapVisualizer.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace StarFoxMapVisualizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //ERROR HANDLER ONLY AVAILABLE IN RELEASE BUILD
#if RELEASE
        public App()
        {
            DispatcherUnhandledException += RootError;
        }

        private void RootError(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashWindow window = new CrashWindow(e.Exception)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            if (window.ShowDialog() ?? true)
            { // CLOSE
                Application.Current.Shutdown();
                return;
            }
            //IGNORE
            e.Handled = true;
        }
        #endif
    }
}
