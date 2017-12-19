using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace View
{
    /// <summary>
    /// An interactive window for setting a player name, server address, and port number.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    public partial class LoginWindow : Window
    {
        /// <summary>
        /// Create a LoginWindow with the default values.
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();

            txtbxPlayerName.Text = PlayerName;
            txtbxServerAddress.Text = ServerAddress;
            txtbxServerPort.Text = Port.ToString();
        }

        /// <summary>
        /// The selected port for this login window.
        /// </summary>
        public int Port { get; private set; } = 11000;

        /// <summary>
        /// The selected player name for this login window.
        /// </summary>
        public string PlayerName { get; private set; } = "YourNameHere";

        /// <summary>
        /// The selected server address for this login window.
        /// </summary>
        public string ServerAddress { get; private set; } = "localhost";

        /// <summary>
        /// The result from this dialog box.  Until the 'login' button is clicked, it will signify
        /// "cancel".
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;


        private void Quit_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Login_Clicked(object sender, RoutedEventArgs e)
        {
            //The port must be parseable as an int.
            int newPort;
            if (!int.TryParse(txtbxServerPort.Text, out newPort))
            {
                MessageBox.Show("Please enter a valid port.");
                return;
            }
            Port = newPort;

            PlayerName = txtbxPlayerName.Text;
            //Opted not to clean player names of whitespace.

            ServerAddress = txtbxServerAddress.Text;
            ServerAddress = new Regex("\\s").Replace(ServerAddress, "");

            Result = MessageBoxResult.OK;
            
            this.Close();
        }

        private void txtbx_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox txtbx = (TextBox)sender;
            txtbx.SelectAll();
        }
    }
}
