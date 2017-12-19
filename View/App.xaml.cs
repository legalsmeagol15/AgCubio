using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Model;

namespace View
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// THIS IS THE MAIN METHOD FOR OUR PROJECT!!
        /// I am not using it for anything fancy any more, though
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // This starts the actual WPF app
            base.OnStartup(e);
        }
    }
}
