using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Network_Controller;
using Model;
using System.Windows.Media.Media3D;
using System.Net.Sockets;
using System.Windows.Media.Animation;
using System.Threading;

namespace View
{
    /// <summary>
    /// The primary game window.  Also, includes the interaction logic for MainWindow.xaml.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    public partial class MainWindow : Window
    {
       
        /// <summary>
        /// The name of the current local player.
        /// </summary>
        private string _PlayerName = "";

        

        /// <summary>
        /// Stores whether the user wants animations and other nice effects
        /// </summary>
        protected bool _HighPerformance;

        /// <summary>
        /// Creates a new MainWindow, first by running a LoginWindow and then setting up the main 
        /// game view once a connection is made.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Login();

        }

        /// <summary>
        /// Shows the login screen, which allows the user to either login or quit.
        /// </summary>
        protected void Login()
        {
            //TODO: Button for high performance
            _HighPerformance = true;
            //Clear out any potential game model data from last time.
            _P3DDictionary = new Dictionary<int, Cube3D>();
            _F3DDictionary = new Dictionary<int, Food3D>();
            _World = new World(DefaultWorldSize, DefaultWorldSize);
            cameraMain.Position = new Point3D(DefaultWorldSize / 2, DefaultWorldSize / 2, 100);
            txtbxMessages.Text = "";

            //Remove any 3D objects from prior game.            
            groupFood3D.Children.Clear();
            groupPlayers3D.Children.Clear();

            //Create and run the loginwindow.
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            while (loginWindow.Result == MessageBoxResult.OK)
            {
                try
                {
                    string hostname = loginWindow.ServerAddress + ":" + loginWindow.Port;
                    Network.ConnectToServer(FirstConnectCallback, hostname);
                    _PlayerName = loginWindow.PlayerName;
                    //lblPlayerName.Content = loginWindow.PlayerName;
                    break;
                }
                catch (SocketException ex)
                {
                    MessageBox.Show(ex.Message);
                    //TODO: The program crashes after showing this dialog.
                    loginWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unrecognized exception:\n" + ex.ToString());
                    loginWindow.ShowDialog();
                }

            }

            //If the result asks to quit, close the MainWindow.
            if (loginWindow.Result == MessageBoxResult.Cancel)
                this.Close();

            lblPlayerName.Content = _PlayerName;

            SetupPlayFloor(_World);

            _Stats = new PlayStats(_PlayerName);

            viewport.Focus();
        }

        /// <summary>
        /// Ensures the 3D play floor is the correct size for the given World.
        /// </summary>        
        protected void SetupPlayFloor(World world)
        {
            int columns = 30;
            int rows = 30;

            double columnWidth = ((double)world.Width / (double)columns);
            double rowHeight = ((double)world.Height / (double)rows);

            List<Point3D> pts = new List<Point3D>();
            List<int> triIndices = new List<int>();

            for (int columnIdx = 0; columnIdx < columns; columnIdx++)
            {
                double x = (columnIdx * columnWidth);// - ((double)world.Width);
                for (int rowIdx = 0; rowIdx < rows; rowIdx++)
                {
                    double y = (rowIdx * rowHeight);// - ((double)world.Height);

                    pts.Add(new Point3D(x, y, 0.1));
                    triIndices.Add(triIndices.Count);
                    pts.Add(new Point3D(x + columnWidth, y, 0.1));
                    triIndices.Add(triIndices.Count);
                    pts.Add(new Point3D(x, y + rowHeight, 0.1));
                    triIndices.Add(triIndices.Count);

                    pts.Add(new Point3D(x + columnWidth, y, 0.1));
                    triIndices.Add(triIndices.Count);
                    pts.Add(new Point3D(x + columnWidth, y + rowHeight, 0.1));
                    triIndices.Add(triIndices.Count);
                    pts.Add(new Point3D(x, y + rowHeight, 0.1));
                    triIndices.Add(triIndices.Count);
                }
            }

            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions = new Point3DCollection(pts);
            mesh.TriangleIndices = new Int32Collection(triIndices);

            GeometryModel3D geom = new GeometryModel3D();
            geom.Geometry = mesh;
            geom.Material = new DiffuseMaterial(Brushes.Gray);

            modelFloor.Content = geom;
        }

        /// <summary>
        /// The standard size of an AgCubio world.
        /// </summary>
        protected const int DefaultWorldSize = 100;
        World _World;

        /// <summary>
        /// The current player
        /// </summary>
        private int _PlayerID = int.MinValue;

        /// <summary>
        /// The maximum distance we are allowed to see, as a multiple of our cube's current size
        /// </summary>
        protected const double MAX_VIEW_DISTANCE = 10.0;

        /// <summary>
        /// The maximum the view shows when the camera is straight overhead, as a multiple of the current cube's size
        /// </summary>
        protected const double VIEW_DISTANCE_TOP = 4.0;

        //The current position of the player.
        private Point _PlayerPosition;

        /// <summary>
        /// Keeps track of where the camera should be centered. Either:
        /// The point where the player's cube is
        /// The point at the average of where all the player's cubes are after a split
        /// Plus some height above the player
        /// </summary>
        protected Point3D GetPlayerCenter()
        {
            double FoV;
            double totalMass = 0;
            double averageX = 0;
            double averageY = 0;

            int myTeamID = _World[_PlayerID].team_id;
            double numberOnTeam = 0;

            if (myTeamID != 0)
            {
                foreach (Cube3D player in _P3DDictionary.Values)
                {
                    if (player.Model.team_id == myTeamID)
                    {
                        numberOnTeam += 1;
                        averageX += player.Model.X;
                        averageY += player.Model.Y;
                        totalMass += player.Model.Mass;
                    }
                }
                averageX /= numberOnTeam;
                averageY /= numberOnTeam;

            }
            else
            {
                totalMass = _World[_PlayerID].Mass;
                averageX = _PlayerPosition.X;
                averageY = _PlayerPosition.Y;
            }

            if (Dispatcher.CheckAccess())
            {
                FoV = cameraMain.FieldOfView;
            }
            else
            {
                FoV = 0;
                Dispatcher.Invoke(new Action(() => FoV = cameraMain.FieldOfView));
            }
            // Camera height is VIEW_DISTANCE_TOP*(cubeSideLength)*tan(FieldOfView/2)
            
            double cameraHeight = VIEW_DISTANCE_TOP * Math.Sqrt(totalMass) / Math.Tan(FoV / 2.0);
            return new Point3D(averageX, averageY, cameraHeight);
        }

        /// <summary>
        /// Returns where the camera should be centered based on where the player is and where the mouse is
        /// </summary>
        protected Point3D GetCameraCenter()
        {
            double playerSize = _World[_PlayerID].Size;
            Point3D playerCenter = GetPlayerCenter();
            double cameraX = (playerCenter.X + PointerPosition.X) / 2.0;
            double cameraY = (playerCenter.Y + PointerPosition.Y) / 2.0;

            // Deal with maximum view area
            if (cameraX > playerCenter.X + playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP)) //Camera is trying to move too far to the +x direction
            {
                cameraX = playerCenter.X + playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP);
            }
            else if (cameraX < playerCenter.X - playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP)) //Camera is trying to move too far to the -x direction
            {

                cameraX = playerCenter.X - playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP);
            }

            if (cameraY > playerCenter.Y + playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP)) //Camera is trying to move too far to the +y direction
            {
                cameraY = playerCenter.Y + playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP);
            }
            else if (cameraY < playerCenter.Y - playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP)) //Camera is trying to move too far to the -y direction
            {
                cameraY = playerCenter.Y - playerSize * ((MAX_VIEW_DISTANCE / 2) - VIEW_DISTANCE_TOP);
            }

            double cameraZ = playerCenter.Z; // We're not messing with this, thankfully...
            Point3D newCameraCenter = new Point3D(cameraX, cameraY, cameraZ);
            return newCameraCenter;
        }


        /// <summary>
        /// This method is the primary interface between the JSONParser operating on data from the network, and 
        /// the GUI.
        /// </summary>
        /// <param name="cubes">The list of cubes to add or update.</param>
        /// <remarks>This method runs in the Dispatcher "thread" to enqueue the adding of visual 
        /// representations of the given model to the viewport.  The reason this is separately threaded is to 
        /// avoid any impact on game play even as it works behind the scenes to create the 3D objects (which is 
        /// an expensive process).  Note that that the way the Dispatcher works, it is not strictly an distinct 
        /// new thread so much as it is a queue for more work to be done.  In any event, adding the 3D visuals 
        /// will have no impact on performance when threaded through the Dispatcher.</remarks>
        internal void ReceiveData(IEnumerable<Cube> cubes)
        {


            foreach (Cube model in cubes)
            {
                //Possibility #1:  If its mass is 0, it should be removed.
                if (model.Mass == 0)
                {
                    //Store a reference to the old model about to be removed.
                    Cube removedCube = _World[model.Uid];

                    //Remove the id, testing if the removal is successful.
                    if (_World.Remove(model.Uid))
                    {
                        if (model.IsFood)
                        {
                            RemoveFoodAsync(model.Uid);

                            if (_World.GetNearestPlayer(new Point(model.X, model.Y)) == _PlayerID)
                            {
                                _Stats.FoodEaten++;
                            }
                        }
                        else
                        {
                            //Remove the player from the world.                            
                            //RemovePlayerAsync(model.Uid);

                            //Local player was eaten:  remove the model UID matches AND there is no 
                            //matching team left.
                            if (model.Uid == _PlayerID && _World.GetTeam(_PlayerID).Count() < 1)
                            {
                                int nearestPlayer =
                                    _World.GetNearestPlayer(new Point(removedCube.X, removedCube.Y), model.Uid);
                                _Stats.EatenBy = _World[nearestPlayer].Name;
                                Disconnect();
                                RemovePlayerAsync(model.Uid);
                                MessageBox.Show("You were eaten by " + _Stats.EatenBy + ".");
                                ShowStats(_Stats);
                                Login();
                                MessagePrint("You were eaten by " + _Stats.EatenBy + ".\n");
                                return;
                            }
                            
                            else
                            {
                                RemovePlayerAsync(model.Uid);

                                int nearestPlayer =
                                   _World.GetNearestPlayer(new Point(removedCube.X, removedCube.Y), model.Uid);

                                //if (nearestPlayer == _PlayerID)
                                //    _Stats.PlayersEaten++;

                                MessagePrint(model.Name + " was eaten by " + _World[nearestPlayer].Name + ".\n");

                                //TODO:  Include a squash animation when a player dies.
                                //DoubleAnimation squashAnimation = new DoubleAnimation(13.0, TimeSpan.FromMilliseconds(2.0));
                                //Cube3D squashed3D = _P3DDictionary[model.Uid];
                                //squashAnimation.Completed += (sender, arg) => RemovePlayerAsync(model.Uid);
                                //squashed3D.BeginAnimation(Cube3D.SwellProperty, squashAnimation);
                            }
                        }
                    }
                }

                //Possibility #2 : If the model is food, should just add it.
                else if (model.IsFood)
                {
                    if (_World.Add(model))
                    {
                        AddFoodAsync(model);
                        //Dispatcher.BeginInvoke((Action)delegate { AddFoodAsync(model); }, priority, null);
                    }
                }


                //Possibility #3: Otherwise, it's a player to either add or update.
                else
                {
                    //Add it?
                    if (_World.Add(model))
                    {
                        AddPlayerAsync(model);
                        MessagePrint(model.Name + " has entered the game.\n");
                    }
                    //Update it?
                    else
                    {
                        _World[model.Uid] = model;
                        UpdatePlayerAsync(model);
                        
                    }


                    //Special code if the player is the UID that was added or updated.
                    if (model.Uid == _PlayerID)
                    {

                        _Stats.MaximumMass = (int)Math.Max(_Stats.MaximumMass, model.Mass);
                        //Dispatcher.BeginInvoke((Action)delegate { UpdateAllFoodAsync(); }, priority, null);
                        _PlayerPosition = new Point(model.X, model.Y);
                        UpdateCamera();



                        lblMass.Content = "" + (int)model.Mass;
                        lblPlayerPosition.Content = (int)model.X + ", " + (int)model.Y;
                    }

                }

            }
        }


        /// <summary>
        /// Method which puts errors in the Messages box
        /// </summary>
        protected void ErrorMessagePrint(string message)
        {
            //Maybe we want to do something fancy, like red text, but otherwise
            //just prepend "Error: " and print normally
            MessagePrint("Error: \n" + message);
        }

        /// <summary>
        /// Maximum number of characters to store in the messages box
        /// </summary>
        protected int messageBoxHistory = 100000;

        /// <summary>
        /// Method which puts text in the messages box.  Automatically adds the carriage return.
        /// </summary>
        protected void MessagePrint(string message)
        {
            if (txtbxMessages.Dispatcher.CheckAccess())
            {
                if (txtbxMessages.Text.Length > messageBoxHistory)
                {
                    txtbxMessages.Text = message +
                        txtbxMessages.Text.Substring(0, messageBoxHistory);
                }
                else
                {
                    txtbxMessages.Text = message + txtbxMessages.Text;
                }
            }
            else
            {
                // If we do need to invoke, just use this method again...
                txtbxMessages.Dispatcher.Invoke(new Action(() => MessagePrint(message)));
            }
        }



        #region Mainwindow networking members

        /// <summary>
        /// Encoding to be used when building a string from the bytes sent by the server
        /// </summary>
        protected System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

        /// <summary>
        /// This is used to store partial cubes passed by the network
        /// </summary>
        protected string partialCubeData = "";

        /// <summary>
        /// Reference to the socket currently in use by this GUI
        /// </summary>
        protected NetworkState networkState;

        /// <summary>
        /// The parser using to convert JSON strings into cubes.
        /// </summary>
        private JSONParser<Cube> parser = new JSONParser<Cube>();

        /// <summary>
        /// Method which is called by the networking code on the first connection
        /// </summary>
        protected void FirstConnectCallback(NetworkState state)
        {
            networkState = state;
            if (state.ConnectionState == NetworkState.ConnectionStates.DISCONNECTED
                || !networkState.Socket.Connected)
            {
                ErrorMessagePrint("Connection failed in FirstConnectCallback");
                Disconnect();
                return;
            }
            Network.Send(state.Socket, _PlayerName);
            state.CallBack = ReceiveFirstPlayerCallback;
        }

        /// <summary>
        /// Receives data for the very first player.
        /// </summary>        
        protected void ReceiveFirstPlayerCallback(NetworkState state)
        {
            if (state.ConnectionState == NetworkState.ConnectionStates.HAS_DATA)
            {
                //Get the data currently in the buffer, and clear the buffer out.
                string rawData = encoding.GetString(state.Buffer);
                state.Buffer = new byte[state.Buffer.Length];

                //Parse the data into cubes.
                string data = ParseIncomingData(rawData);
                IList<Cube> cubesToAdd = parser.Parse(data);

                //If we have a first cube, set the player to that and add all cubes we got this time.
                if (cubesToAdd.Count > 0)
                {
                    //Set the first player's id.
                    _PlayerID = cubesToAdd[0].Uid;
                    Point startPosition = new Point(cubesToAdd[0].X, cubesToAdd[0].Y);

                    //Check if we need to invoke
                    if (viewport.Dispatcher.CheckAccess())
                        ReceiveData(cubesToAdd);
                    else
                    {

                        viewport.Dispatcher.BeginInvoke(new Action(() => { cameraMain.Position = new Point3D(startPosition.X, startPosition.Y, 100); }));
                        viewport.Dispatcher.BeginInvoke(new Action(() => ReceiveData(cubesToAdd)));
                    }



                    //Update the callback to the regular data receiver.
                    state.CallBack = DataCallback;
                }

                //If we have no cube, we have nothing to add, so keep watching in this method.
                else
                    state.CallBack = ReceiveFirstPlayerCallback;

                //Change the connection state to CONNECTED so we can get more data.
                state.ConnectionState = NetworkState.ConnectionStates.CONNECTED;

                //Request more data.
                if (networkState.Socket.Connected)
                    Network.RequestMoreData(state);
                else
                {
                    ErrorMessagePrint("Connection failed");
                    Disconnect();
                    return;
                }
            }

            else if (state.ConnectionState == NetworkState.ConnectionStates.CONNECTED)
                //This should never be called
                throw new NotImplementedException();

            if (state.ConnectionState == NetworkState.ConnectionStates.DISCONNECTED
                || !networkState.Socket.Connected)
            {
                ErrorMessagePrint("Connection failed");
                Disconnect();
                return;
            }
        }

        /// <summary>
        /// Method which will be called by the Network class when a connection
        /// is established or data is received
        /// </summary>
        /// <param name="state"></param>
        protected void DataCallback(NetworkState state)
        {
            
            lock (state)
            {
                if (state.ConnectionState == NetworkState.ConnectionStates.CONNECTED)
                {
                    //This should never be called
                    throw new NotImplementedException();
                }
                else if (state.ConnectionState == NetworkState.ConnectionStates.HAS_DATA)
                {
                    //If the player pushes the disconnect button while in this block, which is quite likely,
                    //We don't want to trample the disconnect button's ability to set the state to disconnected 
                    //The only other time this method should be called is if there is data to read

                    string rawData = encoding.GetString(state.Buffer);
                    state.Buffer = new byte[state.Buffer.Length];

                    string data = ParseIncomingData(rawData);
                    IList<Cube> cubesToAdd = parser.Parse(data);
                    //Check if we need to invoke
                    if (viewport.Dispatcher.CheckAccess())
                    {
                        ReceiveData(cubesToAdd);
                    }
                    else
                    {
                        //I'm not sure of the concequences of running this on the GUI thread
                        // (which invoking does), but I think it should be okay.
                        viewport.Dispatcher.BeginInvoke(new Action(() => ReceiveData(cubesToAdd)));
                    }
                    state.ConnectionState = NetworkState.ConnectionStates.CONNECTED;
                }
                else if (state.ConnectionState == NetworkState.ConnectionStates.DISCONNECTED && !state.Socket.Connected)
                {
                    ErrorMessagePrint("Connection failed");
                    Disconnect();
                    return;
                }
            }
            if (networkState.Socket.Connected)
            {
                Network.RequestMoreData(state);
            }
        }

        /// <summary>
        /// This method takes the data given to us from the network, makes sure
        /// we don't have any partial cubes, stores them if we do, then uses
        /// any stored data to pass back a newline-deliminated string of all
        /// complete cubes
        /// </summary>
        protected string ParseIncomingData(string data)
        {
            string toReturn = "";
            //The network pads the buffers with nulls, which confuse the JSON parser
            // Simply replace them with nothing, getting rid of them
            data = data.Replace("\0", "");
            string[] cubeStrings = data.Split('\n');
            //The only two possiblities for incomplete strings are the first and last
            //In the first case, just shove the data from last time on the front
            cubeStrings[0] = partialCubeData + cubeStrings[0];
            partialCubeData = "";
            //The second case just needs to have a close bracket as the last character
            string last = cubeStrings[cubeStrings.Length - 1];
            if (last.Length <= 1 || (last.Length >= 1 && !last.Substring(last.Length - 1).Equals("}")))
            {
                partialCubeData = last;
                cubeStrings[cubeStrings.Length - 1] = null;
            }
            for (int index = 0; index < cubeStrings.Length; index++)
            {
                String cube = cubeStrings[index];
                if (cube == null)
                {
                    continue;
                }
                toReturn += cube + "\n";
            }
            return toReturn;
        }

        /// <summary>
        /// Closes the socket, shows stats and lets the user reconnect
        /// </summary>
        protected void Disconnect()
        {
            if (networkState != null /*|| networkState.Socket.ConnectionState == NetworkState.ConnectionStates.DISCONNECTED*/)
            {
                //If there is a send pending, we will try to use the disposed socket. Wait for that to complete.
                //Semaphore sendInProgress = Network.SendsInProgress[networkState.Socket.RemoteEndPoint.ToString()];
                //sendInProgress.WaitOne();
                try
                {
                    lock (networkState)
                    {
                        Console.WriteLine("Closing...");
                        networkState.Socket.Disconnect(false);
                        networkState.ConnectionState = NetworkState.ConnectionStates.DISCONNECTED;
                        /*networkState.Socket.Shutdown(SocketShutdown.Both);
                        networkState.Socket.Close();*/
                        Console.WriteLine("Closed");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Disconnect error:\n" + ex.ToString());
                }
                //int debug = sendInProgress.Release();
            }
        }


        private void DisconnectButton_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show("Are you sure you want to disconnect from this game?", "Caution", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
                return;

            //cameraThreadRunning = false;
            Cube player = _World[_PlayerID];
            Disconnect();

            ShowStats(_Stats);

            Login();
        }

        #endregion



        #region Mainwindow view manipulation members

        ///// <summary>
        ///// Zooms in the camera view of the play field.
        ///// </summary>
        //private void View_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    //AdvanceCamera(e.Delta / 10);
        //}
        
        /// <summary>
        /// The ratio of a cube's height at which the camera will focus.
        /// </summary>
        private const double camFocusRatio = 0.5;

        /// <summary>
        /// The ratio of cube heights above which the camera will situate.
        /// </summary>
        private const double camElevationRatio = 12.0;


        
        /// <summary>
        /// Sets the camera to the given position and look direction, maintaining the positive Z-axis as the 
        /// up direction.
        /// </summary>
        /// <param name="position">The camera's new position.</param>
        /// <param name="direction">The camera's new look direction.</param>
        /// <param name="panTime">The time allowed to reach the described alignment.</param>
        /// <param name="upDirection">The direction considered "up" for this camera</param>
        protected void SetCamera(Point3D position, Vector3D direction, TimeSpan panTime, Vector3D upDirection)
        {
            ////Create the animations that will be used.
            //Point3DAnimation positionAnim = new Point3DAnimation(position, new Duration(panTime));
            //Vector3DAnimation lookAnim = new Vector3DAnimation(direction, new Duration(panTime));
            //Vector3DAnimation upAnim = new Vector3DAnimation(upDirection, new Duration(panTime));

            ////Execute the animations.  This will give control of the position and look direction of the camera 
            ////exclusively to the animations.
            //cameraMain.BeginAnimation(PerspectiveCamera.UpDirectionProperty, upAnim);
            //cameraMain.BeginAnimation(PerspectiveCamera.PositionProperty, positionAnim);
            //cameraMain.BeginAnimation(PerspectiveCamera.LookDirectionProperty, lookAnim);

            //Turns out, is is smoother NOT to use an animation.
            cameraMain.Position = position;
            cameraMain.LookDirection = direction;
            cameraMain.UpDirection = upDirection;
        }

        #endregion



        #region MainWindow game interface

        
        /// <summary>
        /// Called when this MainWindow is first loaded.  Starts the cursor spinning.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Rotation3DAnimation cursorSpinner
                = new Rotation3DAnimation(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0.0),
                                            new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90.0),
                                            new Duration(TimeSpan.FromMilliseconds(500)));
            cursorSpinner.RepeatBehavior = RepeatBehavior.Forever;
            rotatorPointer.BeginAnimation(RotateTransform3D.RotationProperty, cursorSpinner);
        }

        /// <summary>
        /// The cached position of the pointer.
        /// </summary>
        protected Point PointerPosition { get; private set; }

        /// <summary>
        /// The method called when the mouse moves in the viewport
        /// </summary>  
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            UpdatePointer();
        }

        /// <summary>
        /// Updates the camera based on the player's position and the location of the mouse pointer
        /// This should be run in a seperate thread
        /// </summary>
        protected void UpdateCamera()
        {
      
            //Moves the camera in a 'following' pattern.
            Cube localPlayer = _World[_PlayerID]; //<- get the current player cube.
            Point playerXY = new Point(localPlayer.X, localPlayer.Y);   //<-the x,y position of the player.
            Point camXY = new Point(cameraMain.Position.X, cameraMain.Position.Y); //<-the x,y position of the camera.
            Vector playerToCam = camXY - playerXY;

            //If the cam's distance is less than a certain amount, it should "back up" to allow space.
            if (playerToCam.Length < 5.0)
            {
                if (playerToCam.Length == 0)
                    playerToCam = new Vector(0, 1);
                else
                    playerToCam.Normalize();
                playerToCam *= 5;
                camXY = playerXY + playerToCam;
            }
            //If the cam's distance is greater than a certain amount, it should begin following the 
            //player.
            else if (playerToCam.Length > 100)
            {
                playerToCam.Normalize();
                playerToCam *= 100;
                camXY = playerXY + playerToCam;
            }

            //Find the focus position for the cube.
            Point3D playerFocus3D = new Point3D(playerXY.X, playerXY.Y, localPlayer.Size * camFocusRatio);

            //Find the new camera position for the cube.
            Point3D camPosition3D = new Point3D(camXY.X, camXY.Y, localPlayer.Size * camElevationRatio);

            //Begin the camera animation.
            SetCamera(camPosition3D, playerFocus3D - camPosition3D,
                        TimeSpan.FromSeconds(0.5), new Vector3D(0, 0, 1));

            //Since moving the camera will displace the spinney pointer, update the pointer.
            UpdatePointer();

        }

        /// <summary>
        /// Updates the cached mouse position as well as the visual 3D marker for the mouse.
        /// </summary>
        protected void UpdatePointer()
        {
            //Hit-test on the Viewport3D to determine what screen x,y coordinate is being pointed at.
            Point pt = Mouse.GetPosition(this);
            RayMeshGeometry3DHitTestResult result =
                VisualTreeHelper.HitTest(viewport, pt) as RayMeshGeometry3DHitTestResult;

            Point oldPosition = PointerPosition;

            //Move the spinney pointer.
            if (result != null)
            {
                if (object.ReferenceEquals(result.VisualHit, modelFloor))
                {
                    Point3D ptHit = result.PointHit;
                    PointerPosition = new Point(ptHit.X, ptHit.Y);
                    translatorPointer.OffsetX = ptHit.X;
                    translatorPointer.OffsetY = ptHit.Y;
                    //PointerPosition = new Point(ptHit.X, ptHit.Y);
                }
                else if (result.VisualHit is Food3D)
                {
                    PointerPosition = new Point(((Food3D)result.VisualHit).Model.X, 
                                                ((Food3D)result.VisualHit).Model.Y);
                }
                else if (result.VisualHit is Cube3D)
                {
                    PointerPosition = new Point(((Cube3D)result.VisualHit).Model.X,
                                                ((Cube3D)result.VisualHit).Model.Y);
                }
                
            }

            //Since the pointer is updated, send a request to move, but only if the socket has not 
            //been disconnected.
            if (networkState != null && networkState.ConnectionState != NetworkState.ConnectionStates.DISCONNECTED)
            {
                // Send the move command
                // For some reason, the server requires the coordinates to be an int. This should not be a big deal.            
                if ((int)oldPosition.X!=(int)PointerPosition.X || (int)oldPosition.Y != (int)PointerPosition.Y)
                {
                    Network.Send(networkState.Socket, "(move, " + (int)PointerPosition.X + ", " 
                                                        + (int)PointerPosition.Y + ")\n");
                }

            }
        }

        /// <summary>
        /// Filters out other 3D objects hit by hittesting so only the model floor is returned.
        /// </summary>
        /// <param name="obj">The object to determine whether it's a valid hit or not.</param>        
        private HitTestFilterBehavior HitTestFilterPoint(DependencyObject obj)
        {
            if (obj == modelFloor) return HitTestFilterBehavior.Stop;
            if (obj is Food3D) return HitTestFilterBehavior.Stop;
            if (obj is Cube3D) return HitTestFilterBehavior.Stop;      
            return HitTestFilterBehavior.Continue;
        }


        /// <summary>
        /// Called on the MainWindow's key down event.
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Key == Key.Space)
            {
                Split();
            }

        }

        /// <summary>
        /// Sends the command to the server to split the player.
        /// </summary>
        protected void Split()
        {
            Network.Send(networkState.Socket, "(split, " + (int)PointerPosition.X + ", " + (int)PointerPosition.Y + ")\n");
            _Stats.TimesSplit++;
        }




        #endregion




        #region MainWindow 3D player and cube object members

        private Dictionary<int, Cube3D> _P3DDictionary = new Dictionary<int, Cube3D>();
        private Dictionary<int, Food3D> _F3DDictionary = new Dictionary<int, Food3D>();


        private Random _Random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Adds a new player Cube3D object for the given model to the board.  This method should be 
        /// called asynchronously through the Dispatcher queue.  This method does NOT remove the player 
        /// model from the World.
        /// </summary>
        /// <param name="model">The player model whose 3D object should be added.</param>
        protected void AddPlayerAsync(Cube model)
        {
            Cube3D newPlayer3D = new Cube3D(model);
            if (_HighPerformance)
            {
                if (model.Color.Equals(Colors.GreenYellow))
                {
                    newPlayer3D.Squash = _Random.NextDouble();
                    DoubleAnimation squashing =
                        new DoubleAnimation(-0.5, 0.5, TimeSpan.FromSeconds(0.5 + _Random.NextDouble()));
                    squashing.AccelerationRatio = 0.25;
                    squashing.DecelerationRatio = 0.25;
                    squashing.RepeatBehavior = RepeatBehavior.Forever;
                    squashing.AutoReverse = true;
                    newPlayer3D.BeginAnimation(Cube3D.SquashProperty, squashing);

                    DoubleAnimation twisting =
                        new DoubleAnimation(-3.0, 3.0, TimeSpan.FromSeconds(_Random.NextDouble()));
                    twisting.RepeatBehavior = RepeatBehavior.Forever;
                    twisting.AutoReverse = true;
                    newPlayer3D.BeginAnimation(Cube3D.TwistProperty, twisting);
                }
                else
                {
                    DoubleAnimation Breathing = new DoubleAnimation(1.0, 1.1,
                                                new Duration(TimeSpan.FromMilliseconds(2500)));
                    Breathing.AutoReverse = true;
                    Breathing.RepeatBehavior = RepeatBehavior.Forever;
                    Breathing.DecelerationRatio = 0.7;
                    newPlayer3D.BeginAnimation(Cube3D.SwellProperty, Breathing);

                    DoubleAnimation Twisting = new DoubleAnimation(-0.15, 0.15,
                            new Duration(TimeSpan.FromMilliseconds(750)));
                    Twisting.AutoReverse = true;
                    Twisting.RepeatBehavior = RepeatBehavior.Forever;
                    newPlayer3D.BeginAnimation(Cube3D.TwistProperty, Twisting);
                }

                
            }
            _P3DDictionary.Add(model.Uid, newPlayer3D);
            groupPlayers3D.Children.Add(newPlayer3D);            

            lblPlayerCount.Content = "" + _World.Players;

            if (_World.Expand(new Point(model.X, model.Y)))
                SetupPlayFloor(_World);
        }

        /// <summary>
        /// Updates the Cube3D object whose player model's ID number matches the ID number of the 
        /// given model.  This method should be called asynchronously through the Dispatcher queue.
        /// </summary>        
        protected void UpdatePlayerAsync(Cube model)
        {
            Cube3D player3D;
            lock (_P3DDictionary)
            {
                player3D = _P3DDictionary[model.Uid];
            }

            player3D.Model = model;
        }

        /// <summary>
        /// Removes the player Cube3D object from the board.  This method should be called 
        /// asynchronously through the Dispatcher queue.  This method does NOT remove the player model 
        /// from the World.
        /// </summary>
        /// <param name="uid">The ID number of the player model whose 3D object should be 
        /// removed.</param>
        protected void RemovePlayerAsync(int uid)
        {
            Cube3D player3D;
            lock (_P3DDictionary)
            {
                player3D = _P3DDictionary[uid];
                _P3DDictionary.Remove(uid);
            }
            lblPlayerCount.Content = "" + _World.Players;
            groupPlayers3D.Children.Remove(player3D);
        }




        /// <summary>
        /// Adds a new Food3D object to the board for the given food model.  This method should be 
        /// called asynchronously through the Dispatcher queue.  This method does NOT add the play 
        /// model from the World.
        /// </summary>
        /// <param name="model">The food model whose 3D object should be add.</param>
        protected void AddFoodAsync(Cube model)
        {
            Food3D newFood3D = new Food3D(model);
            lock (_F3DDictionary)
            {
                _F3DDictionary.Add(model.Uid, newFood3D);
            }
            lblFoodCount.Content = "" + _World.Food;
            groupFood3D.Children.Add(newFood3D);

            if (_World.Expand(new Point(model.X, model.Y)))
                SetupPlayFloor(_World);
        }

        /// <summary>
        /// Removes the Food3D object from the board.  This method should be called asynchronously 
        /// through the Dispatcher queue.  This method does NOT remove the play model from the World.
        /// </summary>
        /// <param name="uid">The ID number of the food model whose 3D object should be removed.</param>
        protected void RemoveFoodAsync(int uid)
        {
            Food3D oldFood3D;
            lock (_F3DDictionary)
            {
                oldFood3D = _F3DDictionary[uid];
                _F3DDictionary.Remove(uid);
            }

            //if (oldFood3D.IsVisible)
            //    groupFood3D.Children.Remove(oldFood3D);
            lblFoodCount.Content = "" + _World.Food;
            groupFood3D.Children.Remove(oldFood3D);
        }

        #endregion




        #region MainWindow play characteristics



        /// <summary>
        /// The cached play performance for the local player.
        /// </summary>
        private PlayStats _Stats;


        /// <summary>
        /// Displays the stats board for the given player model.
        /// </summary>        
        protected void ShowStats(PlayStats data)
        {
            data.EndTime = DateTime.Now;
            StatsBoard board = new StatsBoard(data);
            board.Owner = this;
            board.ShowDialog();
        }

        #endregion





        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show("Are you sure you want to close AgCubio?", "Caution", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            Disconnect();

            e.Cancel = false;

        }

        private void viewport_LostFocus(object sender, RoutedEventArgs e)
        {
            viewport.Focus();
        }
    }


}
