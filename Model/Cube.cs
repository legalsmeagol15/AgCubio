using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Newtonsoft.Json;




namespace Model
{
    /// <summary>
    /// The data model for the cube.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    [JsonObject(MemberSerialization.OptOut)]
    public class Cube
    {
        /// <summary>
        /// The cube's X coordinate on the playing field.
        /// </summary>
        [JsonProperty("loc_x")]
        public double X { get; set; }

        /// <summary>
        /// The cube's Y coordinate on the playing field.
        /// </summary>
        [JsonProperty("loc_y")]
        public double Y { get; set; }


        /// <summary>
        /// A handy way to return the cube's position.
        /// </summary>
        /// <remarks>The X and Y are conveyed for the JSON, rather than the point structure.</remarks>
        [JsonIgnore]
        public Point Position { get { return new Point(X, Y); } }

        /// <summary>
        /// The color of this cube.
        /// </summary>
        /// <remarks>The color of the cube is JSON ignored because a separate property, the argb_color, 
        /// is maintained in an int format for easy transmission to the client.</remarks>
        [JsonIgnore]
        public Color Color { get; }


        /// <summary>
        /// Representation of the cube's colour, required for sending JSON in the specification-
        /// approved manner.
        /// </summary>
        /// <remarks>Turns out, the reason we haven't been able to do easy conversion between an int 
        /// and a Color is because we are using a System.Windows.Media.Color, whereas the more common 
        /// approach is to use a System.Drawing.Color.  The two color structures do not have 
        /// identical methods.  Unfortunately, WPF *must* use the System.Windows.Media.Color, so 
        /// we're kind of stuck separately maintaining an int version of the current color.</remarks>
        public int argb_color { get; }


        /// <summary>
        /// The destination point of the this cube.
        /// </summary>
        /// <remarks>This property is JSON ignored because the destination point is used only by the 
        /// game server, and it should not be sent to the player.</remarks>
        [JsonIgnore]
        public Point Destination { get; set; }


        /// <summary>
        /// The unique id number of this cube.
        /// </summary>
        [JsonProperty("uid")]
        public int Uid { get; set; }


        /// <summary>
        /// An id which, after splitting, will be shared by all cubes owned by the same player.
        /// </summary>
        /// <remarks>Until the player actually splits, the team_id will always signify 0.  In 
        /// retrospect, this could have been used to handily distinguish between a player and a food if 
        /// the player's team_id was always non-zero, but oh well. </remarks>
        public int team_id { get; set; }

        /// <summary>
        /// A value determining whether the given cube is food (i.e., a non-player).
        /// </summary>
        /// <remarks>This is JSON ignored because a separate boolean ("food") is what is actually 
        /// serialized.</remarks>
        [JsonProperty("food")]
        public bool IsFood { get; }


        /// <summary>
        /// Used to signal whether we're looking at a virus here. EEK!
        /// </summary>
        [JsonIgnore]
        public bool IsVirus { get; set; }


        /// <summary>
        /// The name of this cube.
        /// </summary>        ]
        public string Name { get; }

        /// <summary>
        /// The accrued mass of this cube.
        /// </summary>
        public double Mass { get; set; }


        /// <summary>
        /// The size of this Cube.
        /// </summary>
        /// <remarks>This property should be JSON ignored because size is simply a function of 
        /// mass.  However, having a single location where it can be changed if needed can be useful.
        /// </remarks>
        [JsonIgnore]
        public double Size { get { return Math.Sqrt(Mass); } }


        /// <summary>
        /// Returns a rect describing the footprint size of the cube.
        /// </summary>
        [JsonIgnore]
        public Rect FootPrint
        {
            get
            {
                double sz = this.Size;
                return new Rect(X - (sz / 2), Y - (sz / 2), sz, sz);
            }
        }

        /// <summary>
        /// The time this cube was last splitted.  Used for team merging purposes.
        /// </summary>
        [JsonIgnore]
        public DateTime SplitTime { get; set; }


        /// <summary>
        /// Creates a cube model for a player.  This is the JSON-compatible constructor.
        /// </summary>
        /// <param name="uid">The unique ID of this cube.</param>
        /// <param name="name">The name of this player.</param>
        /// <param name="loc_x">The 'x' coordinate on the play field.</param>
        /// <param name="loc_y">The 'y' coordinate on the play field.</param>
        /// <param name="argb_color">The color of this cube.</param>
        /// <param name="mass">The mass of this cube.</param>
        /// <param name="food">Whether or not this cube is a food model.</param>
        [JsonConstructor]
        public Cube(int uid, string name, double loc_x, double loc_y, int argb_color, double mass, bool food)
        {
            
            this.Name = name;            

            //Parse the color.
            string hexColor = argb_color.ToString("X");            
            byte a = Convert.ToByte( hexColor.Substring(0, 2), 16);
            byte r = Convert.ToByte(hexColor.Substring(2, 2), 16);
            byte g = Convert.ToByte(hexColor.Substring(4, 2), 16);
            byte b = Convert.ToByte(hexColor.Substring(6, 2), 16);
            this.Color = Color.FromArgb(a,r,g, b);
            this.argb_color = argb_color;     

            this.X = loc_x;
            this.Y = loc_y;
            this.Mass = mass;
            this.IsFood = food;
            this.Uid = uid;
            
        }

        /// <summary>
        /// For implementation purposes, converts the cube into a string of its important particulars.
        /// </summary>        
        public override string ToString()
        {
            string str = "Cube   X:" + X + "  Y:" + Y + "  Color:" + Color.ToString() + "  Uid:" + Uid 
                + "  team_id:" + team_id + "  IsFood:" + IsFood + "  Name:" + Name + "  Mass:" + Mass;

            return str;

        }

        /// <summary>
        /// Returns true if the comparison cube's uid and name are identical.
        /// </summary>
        public override bool Equals(object obj)
        {
            Cube other = obj as Cube;
            if (other == null) return false;

            return (other.Uid == Uid || other.Name == Name);
            
        }

        /// <summary>
        /// Returns the hashcode equal to the uid plus the cube's name's hashcode.
        /// </summary>        
        public override int GetHashCode()
        {
            return Math.Abs(Uid + Name.GetHashCode());
        }

    }
}
