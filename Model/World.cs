using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Model
{

    /// <summary>
    /// Contains references to all cube models currently on the play field.
    /// </summary>
    public class World
    {

        /// <summary>
        /// The count of all players on the playing field.
        /// </summary>
        public int Players { get { return _PlayerModels.Count; } }

        /// <summary>
        /// The count of all food on the playing field.
        /// </summary>
        public int Food { get { return _FoodModels.Count; } }

        /// <summary>
        /// The field representing the allowable size of the World.  The height and width are stored 
        /// on the immutable Rect object, which contains useful methods like "contains" for valid 
        /// placement testing.
        /// </summary>
        private Rect _Field = Rect.Empty;

        /// <summary>
        /// The world's maximum Y position.
        /// </summary>
        public int Height
        {
            get
            {
                return (int)_Field.Height;
            }           
        }

        /// <summary>
        /// The world's maximum X position.
        /// </summary>
        public int Width
        {
            get
            {
                return (int)_Field.Width;
            }           
        }

        /// <summary>
        /// Ensures this World's boundaries contain the given point.  If the boundaries already 
        /// contains the given point, no change is made, and the method returns false.  Otherwise, 
        /// returns true.
        /// </summary>        
        public bool Expand(Point point)
        {
            //Check if no change will be made.
            if (_Field.Contains(point)) return false;

            //Expand
            _Field.Union(point);
            return true;
        }

        /// <summary>
        /// The dictionary of food Cube objects currently existing in this World, keyed by their 
        /// ID numbers.
        /// </summary>
        private Dictionary<int, Cube> _FoodModels = new Dictionary<int, Cube>();
        /// <summary>
        /// The dictionary of players' Cube objects currently existing in this World, keyed by their 
        /// ID numbers.
        /// </summary>
        private Dictionary<int, Cube> _PlayerModels = new Dictionary<int, Cube>();


        /// <summary>
        /// Creates a new World object with the given height and width.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        public World(int height, int width)
        {
            _Field = new Rect(0.0,0.0, width, height);
            
        }


        /// <summary>
        /// Returns the set of all current players' uid numbers.
        /// </summary>
        public IEnumerable<int> GetAllPlayers()
        {            
            return (_PlayerModels.Values.Select(player => player.Uid));
            //return _CubeModels.Values.Where(c => !c.IsFood).Select(c => c.Uid);
        }

        /// <summary>
        /// Returns the set of all current food items' uid numbers.
        /// </summary>        
        public IEnumerable<int> GetAllFood()
        {
            return (_FoodModels.Values.Select(food => food.Uid));
            //return _CubeModels.Values.Where(c => c.IsFood).Select(c => c.Uid);
        }


        /// <summary>
        /// Gets or sets the player Cube or Food object contained in this World with the specified ID.
        /// </summary> 
        /// <returns>If an object with the given key is contained in this World, returns that object.  If not, 
        /// returns null.</returns>
        public Cube this[int uid]
        {
            get
            {
                if (_FoodModels.ContainsKey(uid))
                    return _FoodModels[uid];
                if (_PlayerModels.ContainsKey(uid))
                    return _PlayerModels[uid];
                return null;                
            }
            set
            {
                //If the food models contains the uid, set that.
                if (_FoodModels.ContainsKey(uid))
                    _FoodModels[uid] = value;

                //Otherwise, must be a player model.  (will throw exception if bad uid).
                _PlayerModels[uid] = value;
                
            }

        }


        #region World player/Cube manipulation members


        /// <summary>
        /// Adds the given cube to this world.
        /// </summary>
        public bool Add(Cube cube)
        {
            //Is it food?
            if (cube.IsFood)
            {
                if (_FoodModels.ContainsKey(cube.Uid))
                    return false;
                _FoodModels.Add(cube.Uid, cube);
                return true;
            }

            //Must be a player.
            if (_PlayerModels.ContainsKey(cube.Uid))
                return false;
            _PlayerModels.Add(cube.Uid, cube);
            return true;
           
        }

        /// <summary>
        /// Returns whether or not the given point is a legitimate point within this World.
        /// </summary>    
        public bool ContainsPoint(Point point)
        {
            return _Field.Contains(point);
        }


        /// <summary>
        /// Returns whether a player with the given ID is contained in this world.
        /// </summary>
        public bool Contains(int uid)
        {
            return _FoodModels.ContainsKey(uid) || _PlayerModels.ContainsKey(uid);            
        }

        /// <summary>
        /// Returns whether a player with the given name is contained in this world.
        /// </summary>
        public bool Contains(string name)
        {
            foreach (Cube player in _PlayerModels.Values)            
                if (player.Name == name) return true;

            return false;
        }
        


        /// <summary>
        /// Removes the cube with the given id.  
        /// </summary>
        /// <returns>Returns true if successful, false if not.</returns>
        public bool Remove(int uid)
        {
            if (_FoodModels.ContainsKey(uid))
            {                
                _FoodModels.Remove(uid);
                return true;
            }
            if (_PlayerModels.ContainsKey(uid))
            {
                _PlayerModels.Remove(uid);
                return true;
            }
            return false;
            
        }
        #endregion



        #region World player relationship testing

        /// <summary>
        /// Returns the set of UIDs for all the cubes of the given team id.
        /// </summary> 
        public IEnumerable<int> GetTeam(int teamID)
        {
            return _PlayerModels.Values
                                .Where(player => player.team_id == teamID)
                                .Select(player => player.Uid);
        }

        /// <summary>
        /// Returns the ID of the nearest player to the given point.
        /// </summary>
        /// <param name="toPoint">The point to test against.</param>
        /// <param name="excludeID">Optional.  If a player is to be exclude, can specify here.</param>
        /// <returns>Returns a negative number if no non-excluded players exist in the World.</returns>
        public int GetNearestPlayer(Point toPoint, int excludeID = int.MinValue)
        {
            double dist = double.MaxValue;
            int nearestID = int.MaxValue;
            foreach (Cube player in _PlayerModels.Values)
            {
                if (player.Uid == excludeID) continue;  //Exclude the excludeID from the search.
                double newDist = Helpers3D.GetDistance(toPoint, new Point(player.X, player.Y));
                if (newDist< dist)
                {
                    dist = newDist;
                    nearestID = player.Uid;
                }
            }
            return nearestID;
        }

        #endregion



    }


}
