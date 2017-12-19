using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Model
{
    /// <summary>
    /// A generic JSON parser that returns a hashset of objects of type T.
    /// </summary>
    public class JSONParser<T>
    {
        //static Regex cubeDeliminator = new Regex("{.*}");

        /// <summary>
        /// The string describing an incomplete JSON object, stored with the hope of prepending it to 
        /// the next data to come in and make a complete JSON string.
        /// </summary>
        private string incomplete;

        /// <summary>
        /// Parses the given string into JSON objects
        /// Each cube object is denoted by curly braces.  It is possible that this method will 
        /// receive an incomplete JSON string.  If so, it puts that string in the 'incomplete' 
        /// field for later and prepends that to the next JSON data to come in.
        /// </summary> 
        public IList<T> Parse(string input)
        {
            //Console.Write(input);
            List<T> result = new List<T>();

            string[] splitStrings = input.Split('\n');

            //Add to the incomplete currently being stored, if any.
            if (!IsCompleteJSON(splitStrings[0]))
            {
                input = incomplete + input;
                splitStrings = input.Split('\n');
                incomplete = "";
            }
                

            //Whether the first string was already complete, or the 'incomplete' was prepended to it, 
            //clear out the 'incomplete'.
            incomplete = "";

            //Add a cube for each string except the last, which might be corrutped.
            for (int i = 0; i < splitStrings.Length; i++)
            {
                //if (!IsCompleteJSON(splitStrings[i])) continue; //For development purposes

                try
                {
                    T newItem = JsonConvert.DeserializeObject<T>(splitStrings[i]);
                    if (newItem != null)
                        result.Add(newItem);
                

                }
                catch (Newtonsoft.Json.JsonException)
                {
                    Console.WriteLine( i + ": JSON error interpreting:\n" + splitStrings[i] + "\n");                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected error interpreting:\n" + splitStrings[i] + "\n" + ex.ToString());
                }
            }

           
      

            //Return the set of new cubes.
            return result;
  
        }

        private static bool IsCompleteJSON(string str)
        {
            if (str.Length < 2) return false; //Must be at least two brackets.
            if (str[0] != '{') return false;
            int paren = 1;
            for (int i = 1; i < (str.Length);i++)
            {
                if (str[i] == '{') return false;
                if (str[i] == '}') paren--;
            }

            return (paren == 0);
        }
        
    }
}
