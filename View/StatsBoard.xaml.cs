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
using Model;

namespace View
{
    /// <summary>
    /// Interaction logic for StatsBoard.xaml.  The StatsBoard displays the play characteristics 
    /// upon conclusion of a game.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    public partial class StatsBoard : Window
    {
        /// <summary>
        /// Creates a new StatsBoard, ready to show.
        /// </summary>
        /// <param name="data">The game completion data to display.</param>
        public StatsBoard(PlayStats data)
        {
            this.Data = data;            
            InitializeComponent();
            this.DataContext = data;
        }

        /// <summary>
        /// The data to display on this stats board.
        /// </summary>
        public PlayStats Data { get; private set; }
    }

    
}
