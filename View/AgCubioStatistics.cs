using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;

namespace View
{
    /// <summary>
    /// A data object just containing the particulars of a player's performance.  Depending on 
    /// how the server is updated, this stuff may end up powering a leaderboard, high scores, etc.
    /// </summary>
    public class AgCubioStatistics
    {
        /// <summary>
        /// The name of the player to whom this data applies.
        /// </summary>
        public string PlayerName { get; }
        /// <summary>
        /// The time this player started playing.
        /// </summary>
        public DateTime StartTime { get; }
        /// <summary>
        /// The time this player finished playing.
        /// </summary>
        public DateTime EndTime { get; }
        /// <summary>
        /// The number of food eaten by this player.
        /// </summary>
        public int FoodEaten { get; }
        /// <summary>
        /// The number of players eaten by this player.
        /// </summary>
        public int PlayersEaten { get; }
        /// <summary>
        /// The mass of this player.
        /// </summary>
        public int Mass { get; }
        /// <summary>
        /// The number of times this player split into smaller cubes.
        /// </summary>
        public int TimesSplit { get; }
        /// <summary>
        /// The number of times a splitted cube belonging to this player was eaten.
        /// </summary>
        public int ChildrenEatenByOthers { get; }
        /// <summary>
        /// The player that ate this cube, if any.
        /// </summary>
        public string EatenBy { get; }

        /// <summary>
        /// Create a new StatsBoardData object with the given particulars.
        /// </summary>
        /// <param name="lastModel">The last player model.</param>
        /// <param name="startTime">The start time for this player.</param>
        /// <param name="endTime">The end time for this player.</param>
        /// <param name="foodEaten">The number of food objects eaten by this player.</param>
        /// <param name="playersEaten">The number of players eaten by this player.</param>
        /// <param name="timesSplit">The number of times this player splitted the cube.</param>
        /// <param name="childrenEatenByOthers">The number of splits of this player that were eaten by others.</param>
        /// <param name="eatenBy">The player who ate this cube, if any.</param>
        public AgCubioStatistics(Cube lastModel, DateTime startTime, DateTime endTime, int foodEaten,
                                int playersEaten, int timesSplit, int childrenEatenByOthers, 
                                string eatenBy = "None")
        {
            this.PlayerName = lastModel.Name;
            this.Mass = (int)lastModel.Mass;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
            this.FoodEaten = foodEaten;
            this.PlayersEaten = playersEaten;
            this.TimesSplit = timesSplit;
            this.ChildrenEatenByOthers = childrenEatenByOthers;
            this.EatenBy = eatenBy;
        }

    }
}
