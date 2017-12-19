using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;

namespace Model
{
    /// <summary>
    /// A data object just containing the particulars of a player's performance.  Depending on 
    /// how the server is updated, this stuff may end up powering a leaderboard, high scores, etc.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    public class PlayStats
    {
        /// <summary>
        /// The name of the player to whom this data applies.
        /// </summary>
        public string PlayerName { get; set; }
        /// <summary>
        /// The time this player started playing.
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// The time this player finished playing.
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// The amount of time the player has played.
        /// </summary>
        public TimeSpan TimePlayed { get { return EndTime - StartTime; } }
        /// <summary>
        /// The amount of food eaten by the player.
        /// </summary>
        public int FoodEaten { get; set; }
        /// <summary>
        /// The number of food eaten by this player.
        /// </summary>
        public double MaximumMass { get; set; }
        /// <summary>
        /// The number of players eaten by this player.
        /// </summary>
        public List<string> PlayersEaten { get; }
        /// <summary>
        /// The mass of this player.
        /// </summary>
        public double LatestMass { get; set; }
        /// <summary>
        /// The number of times this player split into smaller cubes.
        /// </summary>
        public int TimesSplit { get; set; }
        /// <summary>
        /// The number of times a splitted cube belonging to this player was eaten.
        /// </summary>
        public int ChildrenEatenByOthers { get; set; }
        /// <summary>
        /// The player that ate this cube, if any.
        /// </summary>
        public string EatenBy { get; set; }
        /// <summary>
        /// The highest rank the player achieved in the play session, according to mass.
        /// </summary>
        public int BestRank { get; set; }

        /// <summary>
        /// Create a new StatsBoardData object with the given particulars.
        /// </summary>
        /// <param name="lastModel">The last player model.</param>
        /// <param name="startTime">The start time for this player.</param>
        /// <param name="endTime">The end time for this player.</param>
        /// <param name="MaximumMass">The largest mass this player achieved.</param>        
        /// <param name="timesSplit">The number of times this player splitted the cube.</param>
        /// <param name="childrenEatenByOthers">The number of splits of this player that were eaten by others.</param>
        /// <param name="eatenBy">The player who ate this cube, if any.</param>
        public PlayStats(Cube lastModel, DateTime startTime, DateTime endTime, int MaximumMass,
                         int timesSplit, int childrenEatenByOthers, string eatenBy = "None")
        {
            this.PlayerName = lastModel.Name;
            this.LatestMass = (int)lastModel.Mass;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
            this.MaximumMass = MaximumMass;            
            this.TimesSplit = timesSplit;
            this.ChildrenEatenByOthers = childrenEatenByOthers;
            this.EatenBy = eatenBy;
        }

        /// <summary>
        /// Creates a blank stats board info with the given player name.
        /// </summary>
        /// <param name="playerName"></param>
        public PlayStats(string playerName)
        {
            this.PlayerName = playerName;
            this.LatestMass = 0;
            this.StartTime = DateTime.Now;
            this.MaximumMass = 0;
            this.PlayersEaten = new List<string>();
            this.TimesSplit = 0;
            this.ChildrenEatenByOthers = 0;
            this.EatenBy = "";
        }

    }
}
