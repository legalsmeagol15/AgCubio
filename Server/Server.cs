//AgCubio Server by Wesley Oates and Simon Redman
//Date: 3 December 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using Network_Controller;
using System.Windows;
//using System.Windows.Media;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Runtime.Serialization;
using View;

namespace Server
{
    /// <summary>
    /// The class describing an AgCubio game server.  The game server is controls the game itself and 
    /// operates through the GameLoop method, which is its "heartbeat".
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 25, 2015.</date>
    class Server
    {
        

        static void Main(string[] args)
        {
            WorldOptions worldOptions;

            if (args.Length > 0)
            {
                //Assume the first and only passed argument is a filename.
                //Otherwise, blow up.
                
                string filename = args[0];
                JSONParser<WorldOptions> parser = new JSONParser<WorldOptions>();

                using (System.IO.StreamReader reader = new System.IO.StreamReader(filename))
                {
                    try
                    {
                        String[] lines = reader.ReadToEnd().Split('\n');
                        StringBuilder JSON = new StringBuilder(100);
                        foreach(String line in lines)
                        {
                            if (!line.Trim().StartsWith("#"))
                            {
                                JSON.Append(line);
                            }
                        }
                        worldOptions = parser.Parse(JSON.ToString())[0];
                    }
                    catch
                    {
                        //If an exception occurs that prevents reading, just re-call as if there is 
                        //no WorldOptions file.
                        Console.WriteLine("Error reading and parsing \"WorldOptions\" file.  Using defaults.");
                        worldOptions = new WorldOptions();
                    }
                }
            }
            else
            {
                worldOptions = new WorldOptions();
                Console.WriteLine("No \"World Options\" passed. Using defaults.");
                if (!System.IO.File.Exists("./WorldOptions.json"))
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter("./WorldOptions.json"))
                    {
                        StringBuilder toWrite = new StringBuilder();
                        String[] lines = JsonConvert.SerializeObject(worldOptions).Split(',');
                        for (int line = 0; line < lines.Length; line ++)
                        {
                            toWrite.Append(lines[line]);
                            if (line < lines.Length - 1)
                            {
                                toWrite.Append(",\n");
                            }
                            
                        }
                        writer.Write(toWrite.ToString());
                        Console.WriteLine("If you like, you may pass a " +
                        "\"World Options\" file and configure the server. A " +
                        "default file has been created for you in the " +
                        "directory where the server executable is located. " +
                        "See the README for more information.");
                    }
                }
            }

            Server server = new Server(worldOptions);
            Console.Read();

            //Gracefully disconnect all connections
            while (server._NetworkStates.Count > 0)
            {
                server.Disconnect(server._NetworkStates.Values.ToArray()[0]);
            }
        }


        #region Server state members

        private Random _Random = new Random(DateTime.Now.Millisecond);

        private World _World;

        private WorldOptions _WorldOptions;

        /// <summary>
        /// The collection of player stats for this game session.
        /// </summary>
        protected Dictionary<int, PlayStats> _PlayerStats;
        
        #endregion



        #region Server constructors

        /// <summary>
        /// Creates a new Server and begins its game loop.
        /// </summary>
        /// <param name="worldOptions">The gameplay parameters to play the game on this server.</param>
        public Server(WorldOptions worldOptions)
        {
            _WorldOptions = worldOptions;
            
            _World = new World(worldOptions.Width, worldOptions.Height);

            _PlayerStats = new Dictionary<int, PlayStats>();

            Web_Server webServer = new Web_Server(_WorldOptions.WebPortNumber, _PlayerStats);

            

            Network.Server_Awaiting_Client_Loop(HandshakeCallback, _WorldOptions.GamePortNumber);

            //This is an exception-handling loop, should any exceptions make their way past back out 
            //of the game loop.  
            while (true)
            {
                try
                {
                    GameLoop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    break;  //  <--Take this out when ready. 
                }
            }


        }
        #endregion

       

        #region Server game loop methods


        /// <summary>
        /// This loop is the heart of the game logic.  It forces the update of the world at periods 
        /// at equal to or greater than the specified server heartbeat interval.
        /// </summary>
        private void GameLoop()
        {
            DateTime lastBeat = DateTime.Now;
            while (true)
            {
                //Step #1 - ensure enough time has elapsed for a heartbeat.
                DateTime currentBeat = DateTime.Now;
                TimeSpan thisBeat = currentBeat - lastBeat;
                if (thisBeat < _WorldOptions.Heartbeat)
                {
                    Thread.Sleep(_WorldOptions.Heartbeat - thisBeat);
                }
                if (thisBeat > (_WorldOptions.Heartbeat + _WorldOptions.Heartbeat))
                    Console.WriteLine("GameLoop arrhythmia:  heartbeat at "
                                        + thisBeat.TotalMilliseconds + " ms exceeds allowed heartbeat of "
                                        + _WorldOptions.Heartbeat.TotalMilliseconds + " ms.");

                //In the following steps, 'changedPlayers' and 'changedFood' will be passed to the 
                //different game methods to operate upon.  When all game functions are completed, 
                //those sets will constitute the set of items that should be transmitted to the 
                //various players.


                //Step #2 - create the set of players and food that have been changed by the game.
                ISet<Cube> changedFood = new HashSet<Cube>();
                ISet<Cube> changedPlayers = new HashSet<Cube>();

                //Step #2 - Receive and process any requests that are waiting in the Requests queue.
                //This may include a split or a move.
                GameProcessRequests(changedPlayers);

                //Step #3 - Add food.
                GameAddFood(thisBeat, changedFood);

                //Step #3a - Add viruses.
                GameAddViruses(thisBeat, changedPlayers);

                //Step #4 - Move all the players.                
                GameMoveAllPlayers(thisBeat, changedPlayers);

                //Step #5 - Atrophy all (applicable) players
                GameAtrophyPlayers(changedPlayers);

                //Step #6 - Describe all player cubes' footprints.
                IDictionary<Cube, Rect> footprints = GameGetPlayerFootprints();

                //Step #7 - Eat food.
                GameEatFood(footprints, changedPlayers, changedFood);

                //Step #8 - Eat players, and merge splitted players.
                GameEatPlayers(footprints, changedPlayers);

                //Step #8a - encounter viruses.
                GameEncounterViruses(footprints, changedPlayers);

                //Step #9 - rank the players
                GameRankPlayersThisSession();
                
                //Step #20 - send data regarding each changed item.
                Broadcast(changedPlayers);
                Broadcast(changedFood);

                //Step #21 - update all player's highscore stats
                foreach (Cube player in changedPlayers)
                {
                    if (player.IsVirus || player.Uid != player.team_id)
                    {
                        continue;
                    }
                    lock (_PlayerStats)
                    {
                        if (_PlayerStats.ContainsKey(player.team_id))
                        {
                            PlayStats thisPlayerStats = _PlayerStats[player.team_id];
                            thisPlayerStats.LatestMass = player.Mass;
                            if (player.Mass > thisPlayerStats.MaximumMass)
                            {
                                thisPlayerStats.MaximumMass = player.Mass;
                            }
                        }
                        
                    }
                }

                //Step Last - set the last beat to the current beat, and go through the loop again.  
                lastBeat = currentBeat;

            }
        }



        /// <summary>
        /// Any instance can use the same point converter and that's fine, but we should just as well 
        /// save the instantiation overhead and create only one.
        /// </summary>
        /// <remarks>Be careful that this is a System.Windows.PointConverter and not a 
        /// System.Drawing.PointConverter.  The reason for this is that the Cube model uses the first 
        /// version, and is locked in for compatability with WPF.
        /// </remarks>
        private static System.Windows.PointConverter pointConverter
            = new System.Windows.PointConverter();


        /// <summary>
        /// Process any requests received from the client.  Valid requests include move requests, 
        /// split requests, and nothing else.  Any other request, or an invalidly-build request, must 
        /// cause the server to disconnect per the specs.  This method must operate ONLY on the main 
        /// game loop thread.
        /// </summary>
        private void GameProcessRequests(ISet<Cube> changedPlayers)
        {
            Regex moveRequest = new Regex("^\\(move, ");
            Regex splitRequest = new Regex("^\\(split, ");

            //Get the pending requests in a lock, but clear out the pending by simply changing to a 
            //new Queue.  This will interfere with the operation of the network threads as little as 
            //possible and allow the to continue adding to the requests while this game function 
            //works.
            Queue<Request> current;
            lock (_Pending)
            {
                current = _Pending;
                _Pending = new Queue<Request>();
                //A thought - does this mean that another thread which tries to lock on _Pending 
                //but is forced to wait will later proceed to access the old _Pending reference when 
                //the lock finally releases?  It shouldn't...
            }

            //Now, handle all the current requests.
            while (current.Count > 0)
            {
                Request req = current.Dequeue();
                //Every request must be evaluated in its own try block to guard against the possibility 
                //of corrupted requests.
                try
                {
                    //POSSIBILITY #1 - A MOVE REQUEST
                    //Syntax for a move request:
                    //Network.Send(networkState.Socket, "(move, " + (int)PointerPosition.X + ", " + (int)PointerPosition.Y + ")\n");
                    if (moveRequest.IsMatch(req.Message))
                    {

                        //Find out what point was specified in the string.                        
                        string ptString = req.Message.Substring(7, req.Message.Length - 8);
                        System.Windows.Point pt =
                            (System.Windows.Point)pointConverter.ConvertFromString(ptString);

                        //Set destination for the UID's team.
                        Cube[] team = GetTeam(req.Uid);
                        foreach (Cube teamMember in team)
                        {
                            teamMember.Destination = pt;
                            changedPlayers.Add(teamMember);
                        }

                        continue;
                    }


                    //POSSIBILITY #2 - A SPLIT REQUEST
                    //Syntax for a split request:
                    //Network.Send(networkState.Socket, "(split, " + (int)PointerPosition.X + ", " + (int)PointerPosition.Y + ")\n");
                    else if (splitRequest.IsMatch(req.Message))
                    {
                        //What was the point specified in the req.message?  Set the destination first.
                        string ptString = req.Message.Substring(7, req.Message.Length - 9);
                        System.Windows.Point pt =
                            (System.Windows.Point)pointConverter.ConvertFromString(ptString);
                        Cube[] team = GetTeam(req.Uid);
                        //Console.WriteLine("Processing a split request.");
                        //SER: What is the point of this loop?
                        //WWO: Even if it can't split, should update the destination.
                        foreach (Cube teamMember in team)
                        {
                            teamMember.Destination = pt;
                            changedPlayers.Add(teamMember);
                        }


                        //Check if there have been too many splits already.                        
                        if (team.Length >= _WorldOptions.MaxSplits)
                            continue;

                        /*Increment the player's highscore data with the number
                        of splits*/
                        lock (_PlayerStats)
                        {
                            if (_PlayerStats.ContainsKey(_World[req.Uid].team_id))
                            {
                                _PlayerStats[_World[req.Uid].team_id].TimesSplit++;
                            }
                            
                        }
                        //Every cube out there with the same team_id must split, if big enuf.
                        foreach (Cube teamMember
                            in team.Where(tm => tm.Mass >= _WorldOptions.MinimumSplitMass))
                        {
                            SplitCube(teamMember, changedPlayers, pt);
                        }
                        continue;
                    }


                    //ANY OTHER POSSIBILITY - THE REQUEST IS JUNK.
                    //Anything else must cause the server to kill the connection.
                    throw new InvalidRequestException("Invalid request " + req.Message + " from player ID " + req.Uid);
                }
                catch (InvalidRequestException)
                {
                    //When the client sends invalid data, kill the connection.   
                        Disconnect(_NetworkStates[req.Uid]);

                }
            }

        }


        /// <summary>
        /// Helper method when processing a split request. Also useful for 
        /// exploding a player who ate a virus.  This method will split the given team member in two.
        /// </summary>
        /// <param name="teamMember">The team member to split.</param>
        /// <param name="changedPlayers">The set of cubes that will be broadcasted.</param>
        private void SplitCube(Cube teamMember, ISet<Cube> changedPlayers, System.Windows.Point pt)
        {
            int newCubeID;
            lock (_WorldOptions)
            {
                newCubeID = NextID;
            }

            //Modify this team member to split.
            teamMember.Mass /= 2;

            //Create the new cube that is in the team.
            Cube newCube = new Cube(newCubeID, teamMember.Name, teamMember.X, teamMember.Y, teamMember.argb_color, teamMember.Mass, teamMember.IsFood);
            newCube.team_id = teamMember.team_id;
            newCube.Destination = pt;
            DateTime splitTime = DateTime.Now;
            teamMember.SplitTime = splitTime;
            newCube.SplitTime = splitTime;

            //Displace the cubes.
            Vector toDestination = teamMember.Destination - teamMember.Position;
            if (toDestination.X == 0 && toDestination.Y == 0) //<=if they're at destination already?
                toDestination = new System.Windows.Point(_WorldOptions.Width / 2,
                                                         _WorldOptions.Height / 2)
                                            - teamMember.Position;
            if (toDestination.X == 0 && toDestination.Y == 0)   //If it's still 0, set to arbitrary.
                toDestination = new Vector(1, 0);
            //displacer will be 90 degrees different from toDestination
            Vector displacer = new Vector(-toDestination.Y, toDestination.X);
            displacer.Normalize();
            displacer *= teamMember.Size / 2;   // <-move as far as the size.
            System.Windows.Point tmPt = teamMember.Position + displacer;
            System.Windows.Point newPt = newCube.Position - displacer;
            teamMember.X = tmPt.X;
            teamMember.Y = tmPt.Y;
            newCube.X = newPt.X;
            newCube.Y = newPt.Y;

            //Add the new cube to the world.
            lock (_World)
            {
                _World.Add(newCube);
            }

            //Add both cubes to the broadcast signal.
            changedPlayers.Add(teamMember);
            changedPlayers.Add(newCube);
        }


        /// <summary>
        /// Adds food to the game, and returns the set of food IDs that have been added.
        /// </summary>
        /// <param name="beat">The time that has elapsed for which food should be added.</param>        
        private void GameAddFood(TimeSpan beat, ISet<Cube> changedFood)
        {
            //Shouldn't need to lock on _World, cuz getting the food count requires no iteration.
            int currentFoodCount = _World.Food;

            //Find what the empty ratio is, to add food per a differential equation.
            //  if f(t) is the number of food at as a function of time, and k is the new food 
            //  per beat as a constant set out in the WorldOptions, then:
            //
            //  f'(t) = k[1-(f(t)/maxFood)] * (thisBeat/standardBeat)
            //
            double emptyRatio = 1.0 - ((double)_World.Food / _WorldOptions.MaxFoodCount);
            //Scale the empty ratio based on how long a beat we are working with.
            emptyRatio *= ((double)beat.TotalMilliseconds
                / _WorldOptions.Heartbeat.TotalMilliseconds);
            //Multiply by the number of food that should be added per beat.
            emptyRatio *= _WorldOptions.NewFoodPerBeat;

            //Now, create and add the new food.            
            for (int i = 0; i < emptyRatio; i++)
            {
                Cube newFood = GetNewFood();
                lock (_World)
                {
                    _World.Add(newFood);
                }
                changedFood.Add(newFood);
            }
        }


        /// <summary>
        /// Adds viruses to the game.
        /// </summary>
        /// <param name="beat">The size of the current beat, for scaling.</param>
        /// <param name="changedPlayers">The set of changed playerlike cubes (including viruses) 
        /// that will be transmitted.  While viruses will be added to this set, they are not 
        /// actually players.</param>
        private void GameAddViruses(TimeSpan beat, ISet<Cube> changedPlayers)
        {
            //Find what the empty ratio is, to add food per a differential equation.
            //  if v(t) is the number of viruses at as a function of time, and k is the new viruses 
            //  per beat as a constant set out in the WorldOptions, then:
            //
            //  v'(t) = k[1-(v(t)/maxViruses)] * (thisBeat/standardBeat)
            //
            double emptyRatio = 1.0 - ((double)_Viruses.Count / _WorldOptions.MaxVirusCount);
            //Scale the empty ratio based on how long a beat we are working with.
            emptyRatio *= ((double)beat.TotalMilliseconds
                / _WorldOptions.Heartbeat.TotalMilliseconds);
            //Multiply by the number of viruses that should be added per beat.
            emptyRatio *= _WorldOptions.NewVirusPerBeat;

            //Now, create and add the new viruses to the virus dictionary - AND to the world.            
            for (int i = 0; i < emptyRatio; i++)
            {
                Cube newVirus = GetNewVirus();
                _Viruses.Add(newVirus.Uid, newVirus);
                lock (_World)
                {
                    _World.Add(newVirus);
                }
                changedPlayers.Add(newVirus);
            }
        }


        /// <summary>
        /// Move all the players in accordance with their stored destinations.  This method must 
        /// operate ONLY on the main game loop thread.  Note that players on the same team will 
        /// repel each other, so they will not move directly to their destinations until they are 
        /// old enough.
        /// </summary>        
        private void GameMoveAllPlayers(TimeSpan beat, ISet<Cube> changedPlayers)
        {
            Cube[] allPlayers = GetAllPlayers(false);
            
            foreach (Cube player in allPlayers)
            {
                
                //Find the destination vector.  If the move vector ends up bigger than the destination 
                //vector, then the destination vector will be substituted.
                Vector toDestination = player.Destination - player.Position;

                //The way the player is going.
                Vector going = toDestination;
                if (going.Length < 0.5) continue;   //If player isn't really moving.
                going.Normalize();

                //Multiply by mass ratio - bigger masses mean smaller speeds.
                going *= (_WorldOptions.PlayerStartMass / player.Mass);

                //Multiply by the world's global standard speed.
                going *= _WorldOptions.PlayerSpeed;

                //Scale the going vector by how long of a beat this has been.
                going *= ((double)beat.TotalMilliseconds
                            / _WorldOptions.Heartbeat.TotalMilliseconds);

                //Cannot move further than the distance to the destination (unless repelling).
                if (going.Length > toDestination.Length)
                    going = toDestination;

                //Check if this is a team that repel each other.
                going += GetRepulsor(player);
                //Console.WriteLine(GetRepulsor(player).Length);

                //Add going to the current player position.  This is the new position.
                System.Windows.Point newPosition = player.Position + going;

                //Ensure the player stays in the world.
                if (newPosition.X < 0 || newPosition.X > _World.Width)
                    return;
                if (newPosition.Y < 0 || newPosition.Y > _World.Height)
                    return;

                //Set the player to the new position, and ensure the player ends up in the broadcast
                player.X = newPosition.X;
                player.Y = newPosition.Y;
                changedPlayers.Add(player);
            }
        }


        /// <summary>
        /// Reduces the mass of all players above a minimum mass floor. The higher the mass, the 
        /// faster the mass reduction rate will work pursuant to a differential equation.
        /// </summary>
        /// <param name="changedPlayers">The set of already changed players, to
        /// which we will add</param>
        protected void GameAtrophyPlayers(ISet<Cube> changedPlayers)
        {

            Cube[] allPlayers;
            lock (_World)
            {
                allPlayers = _World.GetAllPlayers().Select(idx => _World[idx]).ToArray();
            }

            foreach (Cube player in allPlayers)
            {   
                //
                //  M'(t) =  (M(t)-atrophyFloor) * (thisBeat/standardBeat) * WorldAtrophyRate
                //
                double overMin = player.Mass - _WorldOptions.PlayerMinimumAtrophy;
                player.Mass -= (_WorldOptions.PlayerAtrophyRate) * overMin;

                changedPlayers.Add(player);
            }
        }


        /// <summary>
        /// Returns the set of Rect objects that define the footprints of each player, arranged in a 
        /// dictionary by player ID.
        /// </summary>        
        /// <remarks>This method will lock _World for thread safety.</remarks>
        private IDictionary<Cube, Rect> GameGetPlayerFootprints()
        {

            //Get the footprints of all the players.
            Dictionary<Cube, Rect> footPrints = new Dictionary<Cube, Rect>();
            Cube[] allPlayers = GetAllPlayers(false);
            foreach (Cube player in allPlayers)
            {
                
                footPrints.Add(player, player.FootPrint);
            }
            
            return footPrints;
        }


        /// <summary>
        /// Eats food contained within the various players' cube footprints (which would be squares).  
        /// This method must operate ONLY on the main game loop thread.  Also, this method presumes 
        /// that the items contains in the changed food state were just barely added.  Therefore, 
        /// finding them in the set will mean they should actually be removed.
        /// </summary>
        /// <param name="footPrints">The described footprints of the players, used to test whether a 
        /// player has eaten a given food item.</param>
        /// <param name="changedFood">The set of food that has been added in the current heart beat.
        /// Food IDs may be removed if the food just barely appeared and is immediately eaten.  
        /// Otherwise, any food eaten will be listed here.</param>
        /// <param name="changedPlayers">The set of players that have been changed by eating food.
        /// </param>
        /// <remarks>  This method is apt to be pretty expensive, because it operates on O(P * F), 
        /// where P is the number of players and F is the number of food.  With 5,000 food and 20 
        /// players, that means 100,000 times thru the loop.  Maybe there's a better way to do this...
        /// </remarks>
        private void GameEatFood(IDictionary<Cube, Rect> footPrints, ISet<Cube> changedPlayers,
            ISet<Cube> changedFood)
        {
            Cube[] allFood;
            lock (_World)
            {
                allFood = _World.GetAllFood().Select(idx => _World[idx]).ToArray();
            }
            
            foreach (Cube focusFood in allFood)
            {
                
                //There can be more than one player eating it, if it happened to appear in the 
                //overlap between two players.  If no players are eating the food, go on to next 
                //food.  <=this is probably unnecessarily careful.
                IEnumerable<Cube> playersEating =
                        footPrints.Keys.Where(cube => footPrints[cube].Contains(focusFood.Position));
                int playersEatingCount = playersEating.Count();
                if (playersEatingCount == 0) continue;

                //How much does each player get to eat?  Don't forget to set the food's mass to 0 
                //once this is determined.
                double massForEach = focusFood.Mass / playersEatingCount;
                focusFood.Mass = 0;

                lock (_World)
                {
                    _World.Remove(focusFood.Uid);

                    //If the food is on changedFood already that means it was just barely added and 
                    //hasn't been sent to clients.  So being just barely eaten, it should be removed 
                    //from changedFood if it cannot be added.
                    if (!changedFood.Add(focusFood))
                        changedFood.Remove(focusFood);
                }


                //Now add the eaten mass to each player.
                foreach (Cube player in playersEating)
                {
                    player.Mass += massForEach;
                    changedPlayers.Add(player);
                    lock (_PlayerStats)
                    {
                        if (_PlayerStats.ContainsKey(player.team_id))
                        {
                            _PlayerStats[player.team_id].FoodEaten++;
                            _PlayerStats[player.team_id].MaximumMass =
                                Math.Max(_PlayerStats[player.team_id].MaximumMass, player.Mass);
                        }
                        
                    }
                }


            }

        }


        /// <summary>
        /// Looks for any pair of players that overlap each other.  If the ratio of that overlap 
        /// exceeds the threshold set in the WorldOptions, then the smaller player (ie, the player with 
        /// the larger overlap) is eaten by the bigger player.  Cubes that overlap each other that are 
        /// members of the same team must check if they have aged enough to eat each other, and 
        /// ensure that the team_id survives as a player.UID for exactly one cube.
        /// </summary>
        /// <param name="footprints">The dictionary of footprint rects of all the players.  This should 
        /// be pre-determined for efficiency.</param>
        /// <param name="changedPlayers">The set of players that will require reporting to the clients.
        /// </param>
        private void GameEatPlayers(IDictionary<Cube, Rect> footprints, ISet<Cube> changedPlayers)
        {
            
            Cube[] allPlayers = footprints.Keys.ToArray();

            //Each players must be compared to each other player.
            for (int a = 0; a < allPlayers.Length - 1; a++)
            {                
                Cube playerA = allPlayers[a];
                if (!footprints.ContainsKey(playerA)) continue;
                Rect footprintA = footprints[playerA];


                for (int b = a + 1; b < allPlayers.Length; b++)
                {
                    Cube playerB = allPlayers[b];
                    if (!footprints.ContainsKey(playerB)) continue;
                    Rect footprintB = footprints[playerB];

                    Rect intersection = Rect.Intersect(footprintA, footprintB);
                    if (intersection.IsEmpty)
                    {
                        continue;
                    }
                    double intersectionArea = intersection.Width * intersection.Height;
                    if (intersectionArea == 0.0) continue;

                    //Find the ratio intersected by each - if it's too small, nobody is eaten.
                    double ratioA = intersectionArea / (footprintA.Width * footprintA.Height);
                    double ratioB = intersectionArea / (footprintB.Width * footprintB.Height);
                    if (ratioA < _WorldOptions.PlayerEatenRatio
                        && ratioB < _WorldOptions.PlayerEatenRatio)
                            continue;                    

                    //Which cube gets eaten?
                    Cube eater = (playerA.Mass > playerB.Mass) ? playerA : playerB;
                    Cube eaten = (object.ReferenceEquals(eater, playerA)) ? playerB : playerA;

                    //Check if team members are too young to merge.
                    if (playerA.team_id != 0 && playerA.team_id == playerB.team_id)
                    {
                        //Console.WriteLine("Processing a merge...");

                        DateTime rightNow = DateTime.Now;
                        if ((rightNow - playerA.SplitTime) < TimeSpan.FromSeconds(_WorldOptions.NoMergeSeconds)
                            || (rightNow - playerB.SplitTime) < TimeSpan.FromSeconds(_WorldOptions.NoMergeSeconds))
                        {
                            continue;   //Young Team members cannot eat each other.
                        }

                        //If the eaten was the one with the uid=team_id match, switch them.
                        if (eaten.Uid == eaten.team_id)
                        {
                            Cube temp = eaten;
                            eaten = eater;
                            eater = temp;
                        }
                    }

                    //Add to stats if one player ate another, but only if the team_ids are different.
                    else if (playerA.team_id != playerB.team_id)
                    {
                        lock (_PlayerStats)
                        {
                            if (_PlayerStats.ContainsKey(eater.team_id))
                                _PlayerStats[eater.team_id].PlayersEaten.Add(eaten.Name);
                            if (_PlayerStats.ContainsKey(eaten.team_id))
                                _PlayerStats[eaten.team_id].EatenBy = eater.Name;
                        }

                    }





                    ////If the eaten cube was on a team, swap the id with a
                    ////different member of the team
                    //int[] eaten
                    //List<int> eatenTeam = new List<int>(_World.GetTeam(eaten.Uid));
                    //if (eatenTeam.Count > 0)
                    //{
                    //    //Console.WriteLine("GameEatPlayers: " + eatenTeam.IndexOf(eaten.Uid));
                    //    Cube mainTeamMember = _World[eatenTeam[0]];
                    //    Cube secondTeamMember = _World[eatenTeam[1]];
                    //    int primaryUid = mainTeamMember.Uid;
                    //    lock (_World)
                    //    {
                    //        mainTeamMember.Uid = secondTeamMember.Uid;
                    //        secondTeamMember.Uid = primaryUid;
                    //    }
                    //    changedPlayers.Add(_World[eatenTeam[0]]);
                    //    changedPlayers.Add(_World[eatenTeam[1]]);

                    //    //Re-key the internal dictionaries in World
                    //    _World.Remove(eatenTeam[0]);
                    //    _World.Remove(eatenTeam[1]);
                    //    _World.Add(secondTeamMember);
                    //    _World.Add(mainTeamMember);
                    //}

                    //Do the eating.
                    eater.Mass += eaten.Mass;
                    eaten.Mass = 0;
                    lock (_World)
                    {
                        bool debug = _World.Remove(eaten.Uid);
                    }
                    footprints.Remove(eaten);

                    //Add to the list of changes.
                    changedPlayers.Add(eater);
                    changedPlayers.Add(eaten);
                }
            }


        }


        /// <summary>
        /// The logic for what happens when a player touches a virus.  Note that mere contact is 
        /// enough to cause a result - this is different than eating a player, where there must be a 
        /// sufficient overlap threshold.
        /// </summary>
        /// <param name="footprints">The dictionary of footprints, already generated.</param>
        /// <param name="changedPlayers">The set of players that will end up being broadcasted.  The 
        /// viruses changed will be added to this set, but should not be considered players.</param>
        private void GameEncounterViruses(IDictionary<Cube, Rect> footprints, ISet<Cube> changedPlayers)
        {
            
            foreach (Cube virus in _Viruses.Values)
            {
                Rect virusFootprint = virus.FootPrint;
                foreach (Cube player in footprints.Keys)
                {
                    if (footprints[player].IntersectsWith(virusFootprint))
                    {
                        virus.Mass = 0;                        
                        changedPlayers.Add(virus);
                        SplitCube(player, changedPlayers, player.Position);
                    }
                }
            }
        }

        /// <summary>
        /// Maintains the top-5 records for each player.  Note that two sub-children of the same 
        /// player may end up in the top-5, but only the better rank of the two will be stored on the 
        /// applicable player's game stats.
        /// </summary>        
        private void GameRankPlayersThisSession()
        {
            
            IEnumerable<Cube> allPlayers = GetAllPlayers(false).OrderBy(cube => cube.Mass);
            int idx = 1;
            foreach (Cube playerCube in allPlayers)
            {
                lock (_PlayerStats)
                {
                    if (_PlayerStats.ContainsKey(playerCube.team_id))
                    {
                        _PlayerStats[playerCube.team_id].BestRank
                            = Math.Min(_PlayerStats[playerCube.team_id].BestRank, idx);
                    }
                    
                }
                
                if (++idx > 5) break;
            }            
        }

        #endregion



        #region Server play object members

        private Dictionary<int, Cube> _Viruses = new Dictionary<int, Cube>();

        /// <summary>
        /// Gets all the player cubes in a thread-safe manner by locking on the _World object.
        /// </summary>        
        protected Cube[] GetAllPlayers(bool includeViruses = false)
        {
            Cube[] result;
            if (includeViruses)
            {
                lock (_World)
                {
                    result = _World.GetAllPlayers().Select(idx => _World[idx]).ToArray();
                }
            }
            else
            {
                lock (_World)
                {
                    result = _World.GetAllPlayers().Select(idx => _World[idx])
                                                   .Where(cube => !cube.IsVirus)
                                                   .ToArray();
                }
            }
            return result;

        }

        /// <summary>
        /// Gets all the player cubes of the given team in a thread-safe manner by locking on the 
        /// _World object.
        /// </summary> 
        protected Cube[] GetTeam(int team_id)
        {
            Cube[] result;
            lock (_World)
            {
                result = _World.GetTeam(team_id).Select(idx => _World[idx]).ToArray();
            }
            return result;
        }

        /// <summary>
        /// The stored next ID number.  No reference should be made to this field outside of the the 
        /// property declaration, which will handle incrementing.
        /// </summary>
        private int _NextID = 1;
        /// <summary>
        /// The next available ID number in this current world.  Any time this property is read, 
        /// the value will increment one.
        /// </summary>
        /// <remarks>Thread safety for access of this property should lock on the _WorldOptions 
        /// object.</remarks>
        protected int NextID { get { return _NextID++; } }

        /*
        /// <summary>
        /// Used as a lock in the NextTeamID property
        /// </summary>
        private object NextTeamIDLockObject = new object();

        /// <summary>
        /// Returns a Team ID which has not yet been returned
        /// </summary>
        protected int NextTeamID
        {
            get
            {
                lock (NextTeamIDLockObject)
                {
                    return NextTeamID += 1;
                }
            }
            private set { }
        }
        */


        /// <summary>
        /// Creates and returns a new food object at a random location in the World.
        /// </summary>
        /// <remarks>This method will be thread-safe by locking on the _WorldOptions.</remarks>
        protected Cube GetNewFood()
        {
            int id;
            lock (_WorldOptions)
            {
                id = NextID;
            }


            //Where will the food appear?
            System.Windows.Point origin =
                new System.Windows.Point(_Random.NextDouble() * _World.Width,
                                        _Random.NextDouble() * _World.Height);


            //What will be the new color?
            //Note that R can range 128-256, G can range 0-128, and B can range 128-256.  This 
            //ensures that a true green will not be generated for food.
            System.Drawing.Color color =
                System.Drawing.Color.FromArgb(64 + (int)(_Random.NextDouble() * 128.0),
                                              (int)(_Random.NextDouble() * 180.0),
                                              64 + (int)(_Random.NextDouble() * 128.0));


            return new Cube(id, "", origin.X, origin.Y, color.ToArgb(), 1.0, true);
        }


        /// <summary>
        /// Returns a new virus with a random position.
        /// </summary>        
        protected Cube GetNewVirus()
        {
            int id;
            lock (_WorldOptions)
            {
                id = NextID;
            }


            //Where will the virus appear?
            System.Windows.Point origin =
                new System.Windows.Point(_Random.NextDouble() * _World.Width,
                                        _Random.NextDouble() * _World.Height);


            Cube newVirus = new Cube(id, "virus", origin.X, origin.Y,
                                      System.Drawing.Color.GreenYellow.ToArgb(),
                                      _WorldOptions.PlayerStartMass, false);

            newVirus.Destination = origin;  //<-ensures the virus won't move.
            newVirus.IsVirus = true;

            return newVirus;

        }


        /// <summary>
        /// Creates and returns a new player with the given name at a random location in the World.
        /// </summary>
        /// <remarks>This method will be thread-safe by locking on the _WorldOptions, and also 
        /// on _World.</remarks>
        protected Cube GetNewPlayer(string name)
        {
            //What is the new id number?
            int id;
            lock (_WorldOptions)
            {
                id = NextID;
            }


            //Where will the player appear?
            System.Windows.Point origin =
                new System.Windows.Point(_Random.NextDouble() * _World.Width,
                                        _Random.NextDouble() * _World.Height);

            //Check that the player doesn't appear in any other players.
            Cube[] otherPlayers;
            lock (_World)
            {
                otherPlayers = _World.GetAllPlayers().Select(idx => _World[idx]).ToArray();
            }



            //What will be the new color?
            //Note that R can range 128-256, G can range 0-128, and B can range 128-256.  This 
            //ensures that a true green will not be generated for players, to reserve for viruses.
            System.Drawing.Color color =
                System.Drawing.Color.FromArgb(64 + (int)(_Random.NextDouble() * 128.0),
                                              (int)(_Random.NextDouble() * 180.0),
                                              64 + (int)(_Random.NextDouble() * 128.0));

            //Return a new cube.
            Cube toReturn = new Cube(id, name, origin.X, origin.Y, color.ToArgb(),
                            _WorldOptions.PlayerStartMass, false);

            //Set the team to the current id.
            toReturn.team_id = toReturn.Uid;
            return toReturn;

        }


        /// <summary>
        /// Gets the repulsion vector for a player based on proximity to others of same team_id.
        /// </summary>
        ///<remarks>Locks on _World for thread safety.</remarks>
        private Vector GetRepulsor(Cube player)
        {
            //Get the other team members besides the player.
            Cube[] otherTeam;
            lock (_World)
            {

                otherTeam = _World.GetTeam(player.team_id).Where(other => other != player.Uid)
                                                          .Select(idx => _World[idx])
                                                          .ToArray();
            }

            //If there's no other team, there is no repulsion.
            if (otherTeam.Length == 0) return new Vector(0, 0);

            //The locus is the average of the other team members' positions, with each team member 
            //weighted by their age since split.
            
            DateTime rightNow = DateTime.Now;
            
            Vector gravity = new System.Windows.Point(otherTeam.Average(cube => cube.X), 
                                                      otherTeam.Average(cube => cube.Y)) 
                                            - player.Position;            
            //if (gravity.Length == 0.0) return new Vector(0, 0);

            double distanceRatio = gravity.Length / (player.FootPrint.Width/2);
           
            if (distanceRatio<1.0)
            {
                //If the locus of the other players is within the player, just expel the other players.
                gravity.Normalize();
                gravity *= (player.FootPrint.Width);
            }
            else if (distanceRatio>0.0)
            {
                //IF the locus of the other players is outside the given player, gravity works 
                //like normal and scales by the inverse of square of distance.                
                gravity /= (distanceRatio * distanceRatio);
            }
            else
            {
                //The locus is right on top of the player.
                gravity = new System.Windows.Point(_World.Width / 2, _World.Height / 2) 
                                            - player.Position;
                if (gravity.Length == 0.0) gravity = new Vector(1, 0);
                gravity.Normalize();
                gravity *= (player.FootPrint.Width);
            }

            //Gravity gets weaker as cube gets older.
            gravity *= 1 - ((rightNow - player.SplitTime).TotalSeconds / _WorldOptions.NoMergeSeconds);

            //gravity should be scaled by World settings.
            gravity *= _WorldOptions.TeamRepulsionStrength;            
            
            //Return the repulsor, which is the reverse of the gravity.
            return -gravity;

        }

        #endregion



        #region Server networking members


        /// <summary>
        /// The set of requests awaiting processing by the game loop.
        /// </summary>        
        protected Queue<Request> _Pending = new Queue<Request>();

        /// <summary>
        /// Encoding to be used when building a string from the bytes sent by the server
        /// </summary>
        protected static System.Text.UTF8Encoding _Encoding = new System.Text.UTF8Encoding();


        /// <summary>
        /// Collection of NetworkStates associated with all connected clients.
        /// </summary>
        /// <remarks>WWO:  Your note was appropriate, a dictionary will allow more flexibility.  This 
        /// dictionary is the "phone book" by which we notify all clients of changes in the world. 
        /// </remarks>
        protected Dictionary<int, NetworkState> _NetworkStates = new Dictionary<int, NetworkState>();


        /// <summary>
        /// Method which is called by the networking code when a client first connects
        /// </summary>
        protected void HandshakeCallback(NetworkState state)
        {
            state.CallBack = ReceivePlayerNameCallback;
            Network.RequestMoreData(state);
            Console.WriteLine("A new client has contacted the server.");
        }

        /// <summary>
        /// Called by the network thread when a client sends his name.
        /// </summary>        
        protected void ReceivePlayerNameCallback(NetworkState state)
        {
            if (state.ConnectionState == NetworkState.ConnectionStates.HAS_DATA)
            {
                //Get the data currently in the buffer, and clear the buffer out.
                string rawData = _Encoding.GetString(state.Buffer);
                state.Buffer = new byte[state.Buffer.Length];

                //The 1st 'request' is actually the new player name.  Create the cube & send it.
                string[] receivedRequests = rawData.Split('\n');
                string newPlayerName = receivedRequests[0].Replace("\0", "");
                Console.WriteLine("     Created player name:" + newPlayerName);

                if (newPlayerName == "test")
                {
                    Console.WriteLine("Testing from here.");
                }
                
                Cube player = GetNewPlayer(newPlayerName);

                state.ID = player.Uid;
                lock (_World)
                {
                    _World.Add(player);
                }

                PlayStats newStats = new PlayStats(player.Name);
                newStats.StartTime = DateTime.Now;
                newStats.LatestMass = player.Mass;
                newStats.MaximumMass = player.Mass;
                newStats.BestRank = _PlayerStats.Count;
                lock (_PlayerStats)
                {
                    _PlayerStats.Add(player.team_id, newStats);
                    
                }
                String toSend = JsonConvert.SerializeObject(player);
               

                //Change the connection state to CONNECTED so we can get more data.
                state.ConnectionState = NetworkState.ConnectionStates.CONNECTED;

                //Chain the standard request-receipt method.
                state.CallBack = ReceiveRequestsCallback;

                //Add the connection to the collection of current connections
                lock (_NetworkStates)
                {
                    //Network.Send(state.Socket, toSend);
                    
                    _NetworkStates.Add(player.Uid, state);
                }
                Broadcast(new HashSet<Cube>(new Cube[] { player }));

                System.Threading.Thread.Sleep(50);

                //Send all cubes currently in the world
                SendExistingCubes(state);

                //Request more data.
                if (state.Socket.Connected)
                    Network.RequestMoreData(state);
                else
                {
                    Console.WriteLine("Connection lost in ReceivePlayerNameCallback.");
                    Disconnect(state);
                    return;
                }
            }
            else if (state.ConnectionState == NetworkState.ConnectionStates.CONNECTED)
            {
                //This should never be called
                throw new NotImplementedException();
            }
            else if (state.ConnectionState == NetworkState.ConnectionStates.DISCONNECTED
                || !state.Socket.Connected)
            {
                Console.WriteLine("Connection disconnected in ReceivePlayerNameCallback.");
                Disconnect(state);
                return;
            }
        }

        /// <summary>
        /// Run-once-on-player-connection to send a new client all the cubes currently in the world
        /// </summary>
        protected void SendExistingCubes(NetworkState state)
        {
            
            //HashSet<int> debug_allPlayerIDs = new HashSet<int>(_World.GetAllPlayers());
            //HashSet<int> debug_allFoodIDs = new HashSet<int>(_World.GetAllFood());
            lock (_World)
            {
                foreach (int uid in _World.GetAllPlayers())
                {
                    Network.Send(state.Socket, JsonConvert.SerializeObject(_World[uid]) + "\r\n");
                }
                foreach (int uid in _World.GetAllFood())
                {
                    Network.Send(state.Socket, JsonConvert.SerializeObject(_World[uid]) + "\r\n");
                }
            }
            /*
            IEnumerable<int> allUids = new HashSet<int>();
            HashSet<Cube> allCubes = new HashSet<Cube>();

            lock (_World)
            {
                allUids = allUids.Concat(_World.GetAllPlayers());
                allUids = allUids.Concat(_World.GetAllFood());
                foreach (int uid in allUids)
                {
                    allCubes.Add(_World[uid]);
                }
            }

            StringBuilder jsonCubes = new StringBuilder();
            foreach (Cube cube in allCubes)
            {
                jsonCubes.Append(JsonConvert.SerializeObject(cube) + "\r\n");
            }

            Network.Send(state.Socket, jsonCubes.ToString());
            */
        }



        /// <summary>
        /// Receives requests from a client.
        /// </summary>        
        protected void ReceiveRequestsCallback(NetworkState state)
        {
            if (state.ConnectionState == NetworkState.ConnectionStates.HAS_DATA)
            {
                //Get the data currently in the buffer, and clear the buffer out.
                string rawData = _Encoding.GetString(state.Buffer);
                rawData = rawData.Replace("\0", "");
                state.Buffer = new byte[state.Buffer.Length];

                //Enqueue any received requests.
                foreach (string receivedRequest in rawData.Split('\n'))
                {
                    if (receivedRequest.Length < 1)
                    {
                        continue;
                    }
                    lock (_Pending)
                    {
                        _Pending.Enqueue(new Request(state.ID, receivedRequest));
                    }
                }

                //Change the connection state to CONNECTED so we can get more data.
                state.ConnectionState = NetworkState.ConnectionStates.CONNECTED;

                //Request more data.
                if (state.Socket.Connected)
                    Network.RequestMoreData(state);
                else
                {
                    Console.WriteLine("Connection lost in ReceiveRequestsCallback.");
                    Disconnect(state);
                    return;
                }
            }
            else if (state.ConnectionState == NetworkState.ConnectionStates.CONNECTED)
            {
                //This should never be called
                throw new NotImplementedException();
            }
            else if (state.ConnectionState == NetworkState.ConnectionStates.DISCONNECTED
                || !state.Socket.Connected)
            {
                Console.WriteLine("Connection disconnected in ReceiveRequestsCallback.");
                Console.WriteLine("ReceiveRequestsCallback: SocketEndpoint is " + state.Socket.RemoteEndPoint.ToString());
                Disconnect(state);
                return;
            }
        }

        
        /// <summary>
        /// Closes the socket associated with a player when he dies
        /// </summary>
        protected void Disconnect(NetworkState state)
        {

            //Update the databases appropriately.              
            using (MySqlConnection conn = new MySqlConnection(Web_Server.CONNECTION_STRING))
            {
                try
                {
                    Model.PlayStats session;
                    lock (_PlayerStats)
                    {
                        session = _PlayerStats[state.ID];
                        _PlayerStats.Remove(state.ID);
                    }
                    session.EndTime = DateTime.Now;

                    Console.WriteLine("Saving to database...");
                    // Open a connection
                    conn.Open();

                    
                    //Step #1 - store the game session.
                    MySqlCommand sessionCommand = conn.CreateCommand();
                    sessionCommand.CommandText = GetMySQLSessionString(session, state.ID);
                    sessionCommand.ExecuteNonQuery();
                    
                    //Step #1a - gotta get the session id.
                    int sessionID;
                    MySqlCommand sessionIDGetCommand = conn.CreateCommand();
                    sessionIDGetCommand.CommandText = "SELECT MAX(SessionID) FROM PlaySessions";
                    using (MySqlDataReader reader = sessionIDGetCommand.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException("Could not get the latest session ID.");
                        sessionID =(int) reader["MAX(SessionID)"];                        
                    }
                    //Console.WriteLine("Got session id " + sessionID);

                    
                    //Step #2 - store the eaten-bys
                    MySqlCommand eatenByCommand = conn.CreateCommand();
                    eatenByCommand.CommandText = GetMySQLEatenString(session, sessionID);
                    if (!string.IsNullOrWhiteSpace(eatenByCommand.CommandText))
                        eatenByCommand.ExecuteNonQuery();
                    Console.WriteLine("'" + eatenByCommand.CommandText + "'");
                    

                    Console.WriteLine("Save to database complete.");
                    //TODO:  would threading thru command.BeginExecuteNonQuery() help?

                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }


            //if (state.ConnectionState != NetworkState.ConnectionStates.DISCONNECTED)
            {
                
                try
                {
                    lock (state)
                    {
                        Console.WriteLine("Disconnecting connection from " + state.Socket.RemoteEndPoint.ToString());
                        state.ConnectionState = NetworkState.ConnectionStates.DISCONNECTED;
                        state.Socket.Shutdown(SocketShutdown.Both);
                        state.Socket.Close();
                        state.Socket.Dispose();
                        lock (_NetworkStates)
                        {
                            _NetworkStates.Remove(state.ID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Disconnect error:\n" + ex.ToString());
                }
                //int debug = sendInProgress.Release();

            }
        }

        private static string GetMySQLSessionString(PlayStats stats, int id)
        {

            //Credit http://stackoverflow.com/questions/3633262/convert-datetime-for-mysql-using-c-sharp
            //for the datetime parsing string.
            string startTime = stats.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            string endTime = stats.EndTime.ToString("yyyy-MM-dd HH:mm:ss");
            int playTime = (int)((stats.EndTime - stats.StartTime).TotalMinutes);


            StringBuilder sb = new StringBuilder();
            //INSERT INTO `cs3500_wesleyo`.`PlaySessions` (`SessionID`, `PlayerID`, `PlayerName`, `PlayerBestRank`, `PlayerStart`, `PlayerEnd`, `PlayerMaximumMass`, `PlayerCubesEaten`) VALUES ('1', '0', 'Augustus Caesar', '1', '12/8/15 11:46 am', '12/8/15 12:00 pm', '11235', '100');
            sb.Append("INSERT INTO `cs3500_wesleyo`.`PlaySessions` (");
            sb.Append("`PlayerID`, `PlayerName`, `PlayerBestRank`, `PlayerStart`, `PlayerEnd`, `PlayTime`, `PlayerMaximumMass`, `PlayerItemsEaten`");
            sb.Append(") VALUES (");
            sb.Append("'" + id + "', ");
            sb.Append("'" + stats.PlayerName + "', ");
            sb.Append("'" + stats.BestRank + "', ");
            sb.Append("'" + startTime + "', ");
            sb.Append("'" + endTime + "', ");
            sb.Append("'" + playTime + "', ");
            sb.Append("'" + stats.MaximumMass + "', ");
            sb.Append("'" + stats.PlayersEaten.Count + stats.FoodEaten + "')");

            return sb.ToString();
        }


        private static string GetMySQLEatenString(PlayStats session, int sessionID)
        {
            
            StringBuilder sb = new StringBuilder();
            //INSERT INTO `cs3500_wesleyo`.`PlaySessions` (`SessionID`, `PlayerID`, `PlayerName`, `PlayerBestRank`, `PlayerStart`, `PlayerEnd`, `PlayerMaximumMass`, `PlayerCubesEaten`) VALUES ('1', '0', 'Augustus Caesar', '1', '12/8/15 11:46 am', '12/8/15 12:00 pm', '11235', '100');
 
            foreach (string eatenPlayer in session.PlayersEaten.Distinct())
            {
                sb.Append("INSERT INTO `cs3500_wesleyo`.`PlayersEaten` ");
                sb.Append("(`SessionID`, `PlayerEaten`, `TimesEaten`)");
                sb.Append(" VALUES (");
                sb.Append("'" + sessionID + "', ");
                sb.Append("'" + eatenPlayer + "', ");
                sb.Append("'" + session.PlayersEaten.Count(name => name == eatenPlayer) + "');");
            }
            return sb.ToString();
        }


        private void OnClientDisconnected(NetworkState state)
        {
            lock (_NetworkStates)
            {
                if (_NetworkStates.ContainsKey(state.ID))
                    _NetworkStates.Remove(state.ID);
            }
        }


        /// <summary>
        /// Broadcasts the cube model for the given set of cube IDs.
        /// </summary>        
        protected void Broadcast(IEnumerable<Cube> cubes)
        {
            //Create the JSON strings.
            StringBuilder jsonCubes = new StringBuilder();
            foreach (Cube cube in cubes)
            {
                jsonCubes.Append(JsonConvert.SerializeObject(cube) + "\r\n");
            }

            if (jsonCubes.Length == 0)
            {
                return;
            }

            //Send 'em all out to all connected clients.
            lock (_NetworkStates)
            {
                foreach (NetworkState state in _NetworkStates.Values)
                {
                    if (state.ConnectionState != NetworkState.ConnectionStates.DISCONNECTED)
                    {
                        Network.Send(state.Socket, jsonCubes.ToString());
                    }
                }
            }

        }

        #endregion



        #region Server data models

        /// <summary>
        /// A data model containing a reference to a player's request, and the ID of the player making 
        /// the request.
        /// </summary>
        internal class Request
        {
            /// <summary>
            /// The ID of the player making the request.
            /// </summary>
            public int Uid { get; }

            /// <summary>
            /// The message of the player's request.
            /// </summary>
            public string Message { get; }

            /// <summary>
            /// Creates a new request.
            /// </summary>
            /// <param name="id">The UID of the player making the request.</param>
            /// <param name="message">The message of the player's request.</param>
            public Request(int id, string message)
            {
                this.Uid = id;
                this.Message = message;
            }
        }


        /// <summary>
        /// A data model for maintaining the options available in this AgCubio game.
        /// </summary>
        /// <remarks>The purpose of this data model is to assemble important statistics about a game of 
        /// AgCubio in a form that they could be easily transmitted from a client to a server.  The 
        /// idea is to allow extensibility so that a player could choose what kind of game he wants to 
        /// play, and meet other players that have similar preferences.  The idea is to allow us to 
        /// know how people play the game and why, and the data can be used to optimize the player 
        /// experience.  In other words, it will allow us a simple place to go to tweak the parameters 
        /// of the game to "make it fun."</remarks>
        internal class WorldOptions
        {
            /// <summary>
            /// Height of the world
            /// </summary>
            public int Height { get; set; } = 1000;

            /// <summary>
            /// Width of the world
            /// </summary>
            public int Width { get; set; } = 1000;

            /// <summary>
            /// The maximum number of viruses that may appear on the board.
            /// </summary>
            public int MaxVirusCount { get; set; } = 20;
            /// <summary>
            /// The number of viruses that will appear per beat when there are none on the board.
            /// </summary>
            public int NewVirusPerBeat { get; set; } = 2;

           
            /// <summary>
            /// The amount of time that must elapsed before the split team members may merge again.
            /// </summary>
            public double NoMergeSeconds { get; set; } = 10.0;

            /// <summary>
            /// The strength multiplier with which two brand-new team members appearing immediately 
            /// next to each other will repel each other.
            /// </summary>
            public double TeamRepulsionStrength { get; set; } = 3.0;

            /// <summary>
            /// The starting mass of a new player on the board.
            /// </summary>
            public double PlayerStartMass { get; set; } = 500.0;

            /// <summary>
            /// The minimum mass limit to which a player's Mass may ebb due to atrophy.
            /// </summary>
            public double PlayerMinimumAtrophy { get; set; } = 200.0;

            /// <summary>
            /// The rate of atrophy of a player whose Mass equals the PlayerStartMass, per game beat.
            /// </summary>
            public double PlayerAtrophyRate { get; set; } = 0.00005;

            /// <summary>
            /// The amount which a player must be overlapped before it is considered "eaten".
            /// </summary>
            public double PlayerEatenRatio { get; set; } = 0.5;

            /// <summary>
            /// The speed at which a player of mass equal to the PlayerStartMass will move, per second.
            /// </summary>
            public int PlayerSpeed { get; set; } = 2;

            /// <summary>
            /// In a completely empty world, the number of new food that should be added per beat.
            /// </summary>
            public int NewFoodPerBeat { get; set; } = 10;

            /// <summary>
            /// Max food allowed in the world.
            /// </summary>
            public int MaxFoodCount { get; set; } = 5000;

            /// <summary>
            /// Maximum number of split cubes allowed
            /// </summary>
            public int MaxSplits { get; set; } = 10;

            /// <summary>
            /// The mass of the smallest cube allowed to split
            /// </summary>
            public double MinimumSplitMass { get; set; } = 100;

            /// <summary>
            /// Creates a new WorldOptions specification.
            /// </summary> 
            public WorldOptions(int width = 1000, int height = 1000, double playerStartMass = 500.0,
                int maxFoodCount = 5000)
            {
                this.Height = height;
                this.Width = width;
                this.PlayerStartMass = playerStartMass;
            }


            /// <summary>
            /// The minimum timespan that must occur before the server updates.
            /// </summary>
            public TimeSpan Heartbeat { get; } = TimeSpan.FromMilliseconds(50);


            /// <summary>
            /// The number at which to connect for the game server.
            /// </summary>
            public int GamePortNumber { get; set; } = 11000;


            /// <summary>
            /// The number to connect and listen for web requests.
            /// </summary>
            public int WebPortNumber { get; set; } = 11100;

        }


        /// <summary>
        /// An exception that derives from InvalidOperationException which can be thrown when the server 
        /// attempts to process an invalid request.
        /// </summary>
        [Serializable]
        internal class InvalidRequestException : InvalidOperationException
        {
            //public InvalidRequestException()
            //{
            //}

            public InvalidRequestException(string message) : base(message)
            {
            }

            //public InvalidRequestException(string message, Exception innerException) : base(message, innerException)
            //{
            //}

            //protected InvalidRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
            //{
            //}
        }

        #endregion


    }

}
