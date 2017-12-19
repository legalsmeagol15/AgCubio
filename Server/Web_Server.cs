using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network_Controller;
using System.Net.Sockets;
using MySql.Data.MySqlClient;
using Model;

namespace Server
{

    /// <summary>
    /// The server that receives web requests and responds with a web page.
    /// </summary>
    /// <author>Wesley Oates & Simon Redman</author>
    /// <date>Dec 10, 2015</date>
    class Web_Server
    {
        private Encoding _Encoding;
        private Dictionary<int, PlayStats> _PlayerStatsReference;

        protected internal const string CONNECTION_STRING = "server=atr.eng.utah.edu;database=cs3500_wesleyo;uid=cs3500_wesleyo;password=blackboxers";

        /// <summary>
        /// This is the list of columns we are reading from the database.
        /// Changing it will change what is in the HTML output.
        /// </summary>
        public readonly string[] PLAYSESSION_COLUMNS = { "SessionID", "PlayerName",
                /*"PlayerBestRank",*/ "PlayerStart", "PlayTime", "PlayerEnd", "PlayerMaximumMass",
                "PlayerItemsEaten" };

        /// <summary>
        /// Creates a new web server with the given port number and encoding, and then begins the 
        /// await-data callback cascade.
        /// </summary>
        public Web_Server(int portNumber, Dictionary<int, PlayStats> playerStats, Encoding encoding = null)
        {
            Network.Server_Awaiting_Client_Loop(HandshakeCallback, portNumber);
            _PlayerStatsReference = playerStats;
            if (encoding == null) _Encoding = Encoding.UTF8;
            else _Encoding = encoding;
        }


        /// <summary>
        /// The first callback upon establishing a connection.  This method switches the next callback 
        /// to ReceiveWebRequest and then requests data from the connection.
        /// </summary>        
        protected void HandshakeCallback(NetworkState state)
        {
            Network.Send(state.Socket, handshakeString, SafeCallback);
            state.CallBack = ReceiveWebRequest;
            Network.RequestMoreData(state);
            Console.WriteLine("A new web browser has contacted the server.");
        }

        private static string handshakeString = "HTTP/1.1 200 OK\r\nConnection: close\r\n"
                                              + "Content-Type: text/html; charset=UTF-8\r\n"
                                              + "\r\n";

        /// <summary>
        /// The callback that is invoked at the conclusion of the handshake send.  The purpose of this 
        /// method is because there were some weird Web shenanigans happening at random times when 
        /// we were firing in web requests.
        /// </summary>
        /// <param name=""></param>
        protected void SafeCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The connection was disposed before a send could complete.\r\n"
                                  + ex.ToString());
            }

        }

        protected void ReceiveWebRequest(NetworkState state)
        {
            try
            {
                string msg = _Encoding.GetString(state.Buffer);
                string[] splitMsg = msg.Split(new char[] { '\r', '\n', ' ' });


                int idx;
                for (idx = 0; idx < splitMsg.Length && splitMsg[idx] != "GET"; idx++) ;
                if (idx + 2 >= splitMsg.Length)
                    throw new InvalidOperationException("The Web client is stupid.");
                if (splitMsg[idx + 2] != "HTTP/1.1")
                    throw new InvalidOperationException("The Web client is decrepit.");
                idx++;


                string[] splitCmd = splitMsg[idx].Split('?');
                //Put in the HTML tag and start the table
                StringBuilder response = new StringBuilder("<HTML>\n");
                response.Append(GetHtmlHeader());
                switch (splitCmd[0])
                {
                    case "/":
                    case "/scores":
                        response.Append(GetTableTitle("Scores:"));
                        response.Append(GetPlayerScores());
                        break;
                    case "/games":
                        if (splitCmd.Length < 2)
                        {
                            response.Append(GetErrorPage(splitCmd[0]));
                        }
                        else
                        {
                            response.Append(GetTableTitle("Games:"));
                            response.Append(GetPlayerGames(splitCmd[1]));
                        }
                        break;
                    case "/eaten":
                        if (splitCmd.Length < 2 )
                        {
                            response.Append(GetErrorPage(splitMsg[idx]));
                        }
                        else
                        {
                            response.Append(GetTableTitle("Players eaten:"));
                            response.Append(GetEatenPlayers(splitCmd[1]));
                        }
                        break;
                    case "/highscores":
                        response.Append(GetTableTitle("High Scores:"));
                        response.Append(GetHighscores());
                        break;
                    case "/current":
                        response.Append(GetCurrentPlayers());
                        break;
                    default:
                        //response = "<html> There was an error with command \"" + splitCmd[0] + "\".</html>";
                        response.Append(GetErrorPage(splitCmd[0]));
                        break;

                }
                response.Append("</HTML>");



                Network.Send(state.Socket, response + "\r\n\r\n", SendCompleteCallback);

            }
            catch (Exception e)
            {
                //Strictly speaking, if something goes wrong, I don't really care.
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Gets a simply header string to append to the web page.
        /// </summary>
        protected string GetTableTitle(string title)
        {
            
            StringBuilder toReturn = new StringBuilder();
            //Put headers on the table rows
            toReturn.Append("<H3>");
            toReturn.Append("<p>" + title + "<p>");
            toReturn.Append("</H3>");

            return toReturn.ToString();

        }

        /// <summary>
        /// Returns a HTML page showing all game sessions in the database
        /// </summary>
        /// <returns></returns>
        protected String GetPlayerScores()
        {
            using (MySqlConnection connection = new MySqlConnection(CONNECTION_STRING))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM PlaySessions";
                    //command.CommandText = "SELECT * FROM PlaySessions, PlayersEate nWHERE (PlaySessions.SessionID = PlayersEaten.SessionID)";
                    return (GetHtmlTableForCommand(command, PLAYSESSION_COLUMNS));
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns a HTML page showing all of the named player's game sessions
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected string GetPlayerGames(string name)
        {
            name = name.Split('=')[1];
            using (MySqlConnection connection = new MySqlConnection(CONNECTION_STRING))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM PlaySessions WHERE PlayerName = '" + name + "'";
                    //command.CommandText = "SELECT * FROM PlayersEaten, PlaySessions WHERE (PlayersEaten.SessionID = PlaySessions.SessionID)";
                    String toReturn = GetHtmlTableForCommand(command, PLAYSESSION_COLUMNS);
                    if (toReturn.Length < 1)
                    {
                        return "An error occured. Please check that "  + name + " is correctly spelled.";
                    } else
                    {
                        return toReturn;
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Returns a HTML page containing the list of players eaten in a passed game session
        /// </summary>
        /// <param name="sessionIDParameter">The string "id=" followed by the ID (an Int, please)</param>
        protected string GetEatenPlayers(string sessionIDParameter)
        {
            int sessionID;
            if (!int.TryParse(sessionIDParameter.Split('=')[1], out sessionID))
            {
                return ("Please pass the session ID as an int");
            }
            
            using (MySqlConnection connection = new MySqlConnection(CONNECTION_STRING))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    /*command.CommandText = "SELECT * FROM PlayersEaten, PlaySessions WHERE (PlayersEaten.SessionID = " + sessionID +
                        " AND PlayersEaten.PlayerEaten = PlaySessions.PlayerName)";*/
                    command.CommandText = "SELECT * FROM PlayersEaten WHERE (PlayersEaten.SessionID = " + sessionID + " )";
                    String toReturn = GetHtmlTableForCommand(command, new string[] { "PlayerEaten", "TimesEaten" } );
                    if (toReturn.Length < 1)
                    {
                        return "SessionID " + sessionID + " either ate nobody or has not yet been played!";
                    } else
                    {
                        return toReturn;
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Returns the top game sessions, by mass
        /// </summary>
        /// <param name="count">The number to return.  Defaults to ten.</param>
        ///<param name="sortColumn">The column to sort by.</param>
        protected string GetHighscores(int count = 10, string sortColumn = "PlayerMaximumMass")
        {
            using (MySqlConnection connection = new MySqlConnection(CONNECTION_STRING))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM PlaySessions ORDER BY " + sortColumn + " DESC";
                    String toReturn = GetHtmlTableForCommand(command, PLAYSESSION_COLUMNS, 10);
                    if (toReturn.Length < 1)
                    {
                        return "There are no scores to rank!";
                    }
                    else
                    {
                        return toReturn;
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        protected string GetCurrentPlayers()
        {
            string[] columns = new String[] { "Player Name", "Start Time", "Maximum Mass", "Things Eaten" };
            HashSet<PlayStats> connectedPlayers = new HashSet<PlayStats>(_PlayerStatsReference.Values);
            StringBuilder toReturn = new StringBuilder("<TABLE>\n" + "\t<thead>\n");
            foreach (string row in columns)
            {
                toReturn.Append("\t\t<td>" + row + "</td>\n");
            }
            toReturn.Append("\t</thead>\n");
            bool gotData = false;
            lock (_PlayerStatsReference)
            {
                foreach (PlayStats playerStat in connectedPlayers)
                {
                    gotData = true;
                    toReturn.Append("\t<tr>\n");

                    toReturn.Append("\t\t<td>");
                    toReturn.Append("<a href=/games?player=" + playerStat.PlayerName + ">" + playerStat.PlayerName + "</a>");
                    toReturn.Append("</td>\n");

                    toReturn.Append("\t\t<td>");
                    toReturn.Append(playerStat.StartTime.ToString("yyyy-mm-dd HH:mm:ss"));
                    toReturn.Append("</td>\n");

                    toReturn.Append("\t\t<td>");
                    toReturn.Append(playerStat.MaximumMass);
                    toReturn.Append("</td>\n");

                    toReturn.Append("\t\t<td>");
                    toReturn.Append( (playerStat.FoodEaten + playerStat.PlayersEaten.Count) );
                    toReturn.Append("</td>\n");

                    toReturn.Append("\t</tr>\n");
                }
            }
            if (gotData)
            {
                return toReturn.ToString();
            } else
            {
                return "No players appear to be connected!";
            }
        }

        /// <summary>
        /// Method to return a page indicating the request failed
        /// </summary>
        /// <returns></returns>
        protected string GetErrorPage(string command)
        {
            StringBuilder toReturn = new StringBuilder();
            toReturn.Append("There was an error with command \"" + command + "\"\n");
            return toReturn.ToString();
        }

        /// <summary>
        /// Takes the given MySqlCommand and returns a table with all the columns specified
        /// </summary>
        /// <param name="command">The command to use on the database</param>
        /// <param name="columns">The columns to look for in the given database</param>
        /// <param name="count">The number of rows to return. Negative means all.</param>
        /// <returns>Either the table with the given data, or the empty string if no data was returned with the given command</returns>
        public string GetHtmlTableForCommand(MySqlCommand command, string[] columns, int count = -1)
        {
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                //Put in the HTML tag and start the table
                StringBuilder toReturn = new StringBuilder();
                //Put headers on the table rows
                toReturn.Append("<TABLE>\n" + "\t<thead>\n");

                foreach (string row in columns)
                {
                    toReturn.Append("\t\t<td>" + row + "</td>\n");
                }
                toReturn.Append("\t</thead>\n");
                //Insert the data from the database
                bool gotData = false;
                while (reader.Read() && count != 0)
                {
                    count--;
                    gotData = true;
                    toReturn.Append("\t<tr>\n");
                    foreach (string row in columns)
                    {
                        toReturn.Append("\t\t<td>");
                        if (row.Equals("PlayerName") || row.Equals("PlayerEaten"))
                        { // If PlayerName row, insert as a link to all their games
                            toReturn.Append("<a href=/games?player=" + reader[row] + ">" + reader[row] + "</a>");
                        }
                        else if (row.Equals("SessionID"))
                        { // If SessionID row, insert as a link to all eaten players
                            toReturn.Append("<a href=/eaten?id=" + reader[row] + ">" + reader[row] + "</a>");
                        }
                        else
                        {
                            toReturn.Append(reader[row]);
                        }
                        toReturn.Append("</td>\n");
                    }
                    toReturn.Append("\t</tr>\n");
                }
                if (gotData)
                {
                    return toReturn.ToString();
                } else
                {
                    return "";
                }
            }

        }

        /// <summary>
        /// In theory, this can be done with css, but I have forgotten how and
        /// that would be a bunch of work.
        /// Also could be done with a readonly variable, but I think that
        /// would look really messy.
        /// </summary>
        /// <returns></returns>
        protected string GetHtmlHeader()
        {
            StringBuilder toReturn = new StringBuilder("<HEAD bgcolor=\"#FF0000\">\n");
            toReturn.Append("<TITLE>Blackboxers AgCubio</TITLE>");
            toReturn.Append("<STYLE>\n");
            //Set page background color
            toReturn.Append("body {\n\tbackground-color: LightSlateGray;\n" +
                                    "}\n");
            //Set table properties
            toReturn.Append("table, th, td {\n\tborder-collapse: collapse;\n" +
                                    "\tborder: 2px solid black;\n" +
                                    "\tbackground-color: Linen;\n" +
                                    "}\n");
            //ALL STYLING INFORMATION IN HERE PLEASE
            toReturn.Append("</STYLE>\n");
            //HTML FOR HEADER HERE
            toReturn.Append("<DIV style=\"background-color:#FFFFFF\">\n");
            toReturn.Append("\t<a href=\"/scores\">Home</a>\n");
            toReturn.Append("\t<a href=\"/highscores\">Highscores</a>\n");
            toReturn.Append("\t<a href=\"/current\">Currently Connected Players</a>\n");
            toReturn.Append("</DIV>\n");
            toReturn.Append("</HEAD>\n");
            return toReturn.ToString();
        }

        /// <summary>
        /// The callback that occurs when a send is complete.  Ends the 'send' process, and disconnects 
        /// because these are web requests rather than persistent connections.
        /// </summary>        
        protected void SendCompleteCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndSend(ar);

            socket.Disconnect(false);
            socket.Close();
            //socket.Shutdown(SocketShutdown.Both);

        }
    }
}
