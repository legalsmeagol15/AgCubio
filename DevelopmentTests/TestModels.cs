using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Model;
using System.Windows;
using System.Windows.Media.Media3D;

namespace DevelopmentTests
{
    [TestClass]
    public class TestModels
    {
        string LONG_DATA
        {
            get
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader("..\\..\\..\\Resources\\SampleServerInput.txt"))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        const String cubeString = "{\"loc_x\":926.0,\"loc_y\":682.0,\"argb_color" +
            "\":-65536,\"uid\":5571, \"team_id\":0, \"food\":false,\"Name\":\"3500 is love\"," +
            "\"Mass\":1000.0}";

        const String foodString = "{ \"loc_x\":250.0, \"loc_y\":102.0, \"argb_" +
            "color\":-9555272, \"uid\":4990, \"team_id\":0, \"food\":true," +
            "\"Name\":\"\", \"Mass\":1.0 }";

        /// <summary>
        /// Tests a known cube against a cube generated from JSON
        /// </summary>
        [TestMethod]
        public void TestJSONParse1()
        {
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            Cube expectedCube = new Cube(5571, "3500 is love", 926.0, 682.0, -65536, 1000.0, false);
            // There is only one, but there's no other way to quickly get from a hashset
            foreach (Cube cube in parsedCubes)
            {
                Assert.AreEqual(expectedCube, cube);
                Assert.AreEqual<double>(expectedCube.X, cube.X);
                Assert.AreEqual<double>(expectedCube.Y, cube.Y);
                Assert.AreEqual<System.Windows.Media.Color>(expectedCube.Color, cube.Color);
                Assert.AreEqual<int>(expectedCube.Uid, cube.Uid);
                Assert.AreEqual<Boolean>(expectedCube.IsFood, cube.IsFood);
                Assert.AreEqual<String>(expectedCube.Name, cube.Name);
                Assert.AreEqual<double>(expectedCube.Mass, cube.Mass);

                Assert.AreEqual<int>(expectedCube.GetHashCode(), cube.GetHashCode());
            }
        }

        /// <summary>
        /// Test the first, last and a middle generated cube of a larger JSON input
        /// </summary>
        [TestMethod]
        public void TestJSONParse2()
        {
            JSONParser<Cube> parser = new JSONParser<Cube>();
            List<Cube> parsedCubes = new List<Cube>(parser.Parse(LONG_DATA));
            Cube expectedFirstCube = new Cube(5571, "3500 is love", 926.0, 682.0, -65536, 1000.0, false);
            //Arbitrarily pick the cube on line 15 to be our "middle cube"
            Cube expectedMiddleCube = new Cube(13, "", 748.0, 364.0, -1845167, 1.0, true);
            Cube expectedLastCube = new Cube(36, "", 885.0, 634.0, -15560314, 1.0, true);
            parsedCubes = new List<Cube>(){ parsedCubes[0], parsedCubes[14], parsedCubes[parsedCubes.Count - 1] };
            List<Cube> expectedCubes = new List<Cube>() { expectedFirstCube, expectedMiddleCube, expectedLastCube };
            for (int index = 0; index < parsedCubes.Count; index ++)
            {
                Assert.AreEqual(expectedCubes[index], parsedCubes[index]);
                Assert.AreEqual<double>(expectedCubes[index].X, parsedCubes[index].X);
                Assert.AreEqual<double>(expectedCubes[index].Y, parsedCubes[index].Y);
                Assert.AreEqual<System.Windows.Media.Color>(expectedCubes[index].Color, parsedCubes[index].Color);
                Assert.AreEqual<int>(expectedCubes[index].Uid, parsedCubes[index].Uid);
                Assert.AreEqual<Boolean>(expectedCubes[index].IsFood, parsedCubes[index].IsFood);
                Assert.AreEqual<String>(expectedCubes[index].Name, parsedCubes[index].Name);
                Assert.AreEqual<double>(expectedCubes[index].Mass, parsedCubes[index].Mass);

                Assert.AreEqual<int>(expectedCubes[index].GetHashCode(), parsedCubes[index].GetHashCode());
            }
        }

        ///// <summary>
        ///// Test parsing JSON with an incomplete string
        ///// </summary>
        //[TestMethod]
        //public void TestJSONParse3()
        //{
        //    String dataStart = LONG_DATA.Substring(0, LONG_DATA.Length - 5);
        //    //String dataEnd = LONG_DATA.Substring(LONG_DATA.Length - 5);
        //    JSONParser<Cube> parser = new JSONParser<Cube>();
        //    List<Cube> parsedCubes = new List<Cube>(parser.Parse(dataStart));
        //    //List<Cube> moreParsedCubes = new List<Cube>(parser.Parse(dataEnd));
        //    Cube expectedFirstCube = new Cube(5571, "3500 is love", 926.0, 682.0, -65536, 1000.0, false);
        //    //Arbitrarily pick the cube on line 15 to be our "middle cube"
        //    Cube expectedMiddleCube = new Cube(13, "", 748.0, 364.0, -1845167, 1.0, true);
        //    Cube expectedLastCube = new Cube(36, "", 885.0, 634.0, -15560314, 1.0, true);
        //    Assert.AreEqual(parsedCubes.Count, 37);
        //    //Assert.AreEqual(moreParsedCubes.Count, 0);
        //    parsedCubes = new List<Cube>() { parsedCubes[0], parsedCubes[14], parsedCubes[36] };
        //    List<Cube> expectedCubes = new List<Cube>() { expectedFirstCube, expectedMiddleCube, expectedLastCube };
        //    for (int index = 0; index < parsedCubes.Count; index++)
        //    {
        //        Assert.AreEqual(expectedCubes[index], parsedCubes[index]);
        //        Assert.AreEqual<double>(expectedCubes[index].X, parsedCubes[index].X);
        //        Assert.AreEqual<double>(expectedCubes[index].Y, parsedCubes[index].Y);
        //        Assert.AreEqual<System.Windows.Media.Color>(expectedCubes[index].Color, parsedCubes[index].Color);
        //        Assert.AreEqual<int>(expectedCubes[index].Uid, parsedCubes[index].Uid);
        //        Assert.AreEqual<Boolean>(expectedCubes[index].IsFood, parsedCubes[index].IsFood);
        //        Assert.AreEqual<String>(expectedCubes[index].Name, parsedCubes[index].Name);
        //        Assert.AreEqual<double>(expectedCubes[index].Mass, parsedCubes[index].Mass);

        //        Assert.AreEqual<int>(expectedCubes[index].GetHashCode(), parsedCubes[index].GetHashCode());
        //    }
        //}

        /// <summary>
        /// Running the constructor should have no errors
        /// </summary>
        [TestMethod]
        public void TestWorldConstructor()
        {
            World world = new World(5, 5);
        }

        /// <summary>
        /// Adding a cube should increase the cube count to 1
        /// </summary>
        [TestMethod]
        public void TestWorldAddPlayer1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, world.Players);
        }

        /// <summary>
        /// Adding a cube should increase the size of the cube collection to 1
        /// </summary>
        [TestMethod]
        public void TestWorldAddPlayerd2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, new HashSet<int>(world.GetAllPlayers()).Count);
        }

        /// <summary>
        /// A cube's UID should be in the world after adding it
        /// </summary>
        [TestMethod]
        public void TestWorldAddPlayer3()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(5571, new List<int>(world.GetAllPlayers())[0]);
        }

        /// <summary>
        /// The world should be able to tell us a player's UID is
        /// in the world after adding him
        /// </summary>
        [TestMethod]
        public void TestWorldContains1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.IsTrue(world.Contains(5571));
        }

        /// <summary>
        /// The world should be able to tell us a player's name is
        /// in the world after adding him
        /// </summary>
        [TestMethod]
        public void TestWorldContains2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.IsTrue(world.Contains("3500 is love"));
        }

        /// <summary>
        /// The world should be able to tell us a player's UID is
        /// in the world after adding him
        /// </summary>
        [TestMethod]
        public void TestWorldIndexer1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual(world[5571], parsedCubes[0]);
        }

        /// <summary>
        /// Adding a cube should increase the cube count to 1
        /// Removing the same cube should decrease the cube count back to 0
        /// </summary>
        [TestMethod]
        public void TestWorldRemovePlayer1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, world.Players);
            world.Remove(parsedCubes[0].Uid);
            Assert.AreEqual<int>(0, world.Players);
        }

        /// <summary>
        /// Adding a cube should increase the cube count to 1
        /// Attempting to remove a player not in the set should
        /// not do anything
        /// </summary>
        [TestMethod]
        public void TestWorldRemovePlayer2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(LONG_DATA);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, world.Players);
            world.Remove(parsedCubes[1].Uid);
            Assert.AreEqual<int>(1, world.Players);
            Assert.AreEqual<int>(0, world.Food);
        }

        /// <summary>
        /// Test removing a player by UID
        /// </summary>
        [TestMethod]
        public void TestWorldRemovePlayer3()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(LONG_DATA);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, world.Players);
            world.Remove(parsedCubes[0].Uid);
            Assert.AreEqual<int>(0, world.Players);
        }

        /// <summary>
        /// Adding a food should increase the food count to 1
        /// </summary>
        [TestMethod]
        public void TestWorldAddFood1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(foodString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, world.Food);
        }

        /// <summary>
        /// Adding a food should increase the size of the food collection to 1
        /// </summary>
        [TestMethod]
        public void TestWorldAddFood2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(foodString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(1, new HashSet<int>(world.GetAllFood()).Count);
        }

        /// <summary>
        /// A food's UID should be in the world after adding it
        /// </summary>
        [TestMethod]
        public void TestWorldAddFood3()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(foodString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(4990, new List<int>(world.GetAllFood())[0]);
        }

        /// <summary>
        /// Test removing a food by UID
        /// </summary>
        [TestMethod]
        public void TestWorldRemoveFood1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(LONG_DATA);
            world.Add(parsedCubes[1]);
            Assert.AreEqual<int>(1, world.Food);
            world.Remove(parsedCubes[1].Uid);
            Assert.AreEqual<int>(0, world.Food);
        }

        /// <summary>
        /// Test removing a food by model
        /// </summary>
        [TestMethod]
        public void TestWorldRemoveFood2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(LONG_DATA);
            world.Add(parsedCubes[1]);
            Assert.AreEqual<int>(1, world.Food);
            Cube foodCube = world[parsedCubes[1].Uid];
            world.Remove(foodCube.Uid);//Modified because the world changed to no longer remove by model
            Assert.AreEqual<int>(0, world.Food);
        }

        /// <summary>
        /// Test the nearest player when there is only one player present
        /// </summary>
        [TestMethod]
        public void TestWorldNearestPlayer1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(5571, world.GetNearestPlayer(new System.Windows.Point(0,0)));
        }

        /// <summary>
        /// Test the nearest player when there are a player and a food present
        /// </summary>
        [TestMethod]
        public void TestWorldNearestPlayer2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            parsedCubes = parser.Parse(foodString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(5571, world.GetNearestPlayer(new System.Windows.Point(0, 0)));
        }

        /// <summary>
        /// Test the nearest player when there are a two players
        /// </summary>
        [TestMethod]
        public void TestWorldNearestPlayer3()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            string secondCubeString = new string(cubeString.ToCharArray());
            secondCubeString = secondCubeString.Replace("5571", "100");
            parsedCubes = parser.Parse(secondCubeString);
            parsedCubes[0].X = 100;
            parsedCubes[0].Y = 100;
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(100, world.GetNearestPlayer(new Point(0, 0)));
        }

        /// <summary>
        /// Test the indexer when nothing is in the world
        /// </summary>
        [TestMethod]
        public void TestWorldIndexerSet1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world[5571] = parsedCubes[0];
            Assert.AreEqual<Point>(new Point(926.0, 682.0), new Point(world[5571].X, world[5571].Y));
        }

        /// <summary>
        /// Test the indexer when replacing something in the world
        /// </summary>
        [TestMethod]
        public void TestWorldIndexerSet2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            string secondCubeString = new string(cubeString.ToCharArray());
            //secondCubeString = secondCubeString.Replace("5571", "100");
            parsedCubes = parser.Parse(secondCubeString);
            parsedCubes[0].X = 100;
            parsedCubes[0].Y = 100;
            world[5571] = parsedCubes[0];
            Assert.AreEqual<Point>(new Point(100.0, 100.0), new Point(world[5571].X, world[5571].Y));
        }

        /// <summary>
        /// Test getting the team id when it is non-default
        /// </summary>
        [TestMethod]
        public void TestWorldGetTeam1()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(5571, new List<int>(world.GetTeam(0))[0]);
        }


        /// <summary>
        /// Test getting the team id when it is non-default
        /// </summary>
        [TestMethod]
        public void TestWorldGetTeam2()
        {
            World world = new World(5, 5);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            String secondString = cubeString.Replace("\"team_id\":0", "\"team_id\":1");
            IList<Cube> parsedCubes = parser.Parse(secondString);
            world.Add(parsedCubes[0]);
            Assert.AreEqual<int>(5571, new List<int>(world.GetTeam(1))[0]);
        }

        /// <summary>
        /// Test getting the team id when it is non-default
        /// </summary>
        [TestMethod]
        public void TestWorldExpand1()
        {
            World world = new World(5, 5);
            Point expansion = new Point(10, 10);
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(cubeString);
            Assert.IsFalse(world.ContainsPoint(new Point(7, 7)));
            world.Expand(new Point(10, 10));
            Assert.AreEqual<Point>(expansion, new Point(world.Width, world.Height));
            Assert.IsTrue(world.ContainsPoint(new Point(7, 7)));
        }

        /// <summary>
        /// Test that Cube's ToString() returns what we would expect
        /// </summary>
        [TestMethod]
        public void TestCubeToString1()
        {
            World world = new World(5, 5);
            String expected = "Cube   X:926  Y:682  Color:#FFFF0000  Uid:5571  team_id:0  IsFood:False  Name:3500 is love  Mass:1000";
            JSONParser<Cube> parser = new JSONParser<Cube>();
            IList<Cube> parsedCubes = parser.Parse(LONG_DATA);
            Assert.AreEqual<String>(expected, parsedCubes[0].ToString());

            //Ensure that the cube's properties are properly settable
            System.Windows.Point cubePt = parsedCubes[0].Position;
            Assert.AreEqual(cubePt.X, 926);
            Assert.AreEqual(cubePt.Y, 682);
            parsedCubes[0].Destination = new Point(100, 200);
            Assert.AreEqual(parsedCubes[0].Destination.X, 100);
            Assert.AreEqual(parsedCubes[0].Destination.Y, 200);
            Assert.AreEqual(parsedCubes[0].argb_color, -65536);
            parsedCubes[0].IsVirus = false;
            Assert.IsFalse(parsedCubes[0].IsVirus);
            parsedCubes[0].IsVirus = true;
            Assert.IsTrue(parsedCubes[0].IsVirus);
            Assert.AreEqual(parsedCubes[0].Size, Math.Sqrt(1000));
            Rect fp = parsedCubes[0].FootPrint;
            Assert.AreEqual(fp.Width, Math.Sqrt(1000));
            Assert.AreEqual(fp.Height, Math.Sqrt(1000));
            DateTime rightNow = DateTime.Now;
            parsedCubes[0].SplitTime = rightNow;
            Assert.AreEqual(rightNow, parsedCubes[0].SplitTime);
            Assert.IsFalse(parsedCubes[0].Equals(null));
            Assert.IsTrue(parsedCubes[0].Equals(parsedCubes[0]));

        }

        /// <summary>
        /// Try to get the distance between two 2D points
        /// </summary>
        [TestMethod]
        public void TestGetDistance2D1()
        {
            double distance = Helpers3D.GetDistance(new Point(0, 0), new Point(1, 1));
            double expectedDistance = Math.Sqrt(2);
            Assert.AreEqual<double>(expectedDistance, distance);
        }

        /// <summary>
        /// Try to get the distance between two 2D points
        /// </summary>
        [TestMethod]
        public void TestGetDistance3D1()
        {
            double distance = Helpers3D.GetDistance(new Point3D(0, 0, 0), new Point3D(1, 1, 1));
            double expectedDistance = Math.Sqrt(3);
            Assert.AreEqual<double>(expectedDistance, distance);
        }

        /// <summary>
        /// Test getting the perpenducular vector to two vectors
        /// </summary>
        [TestMethod]
        public void TestGetNormalVector3D1()
        {
            Vector3D vector1 = new Vector3D(1, 0, 0);
            Vector3D vector2 = new Vector3D(0, 1, 0);
            Vector3D normal = Helpers3D.GetNormal(vector1, vector2);
            Vector3D expected = new Vector3D(0, 0, 1);
            Assert.AreEqual<Vector3D>(expected, normal);
        }

        /// <summary>
        /// Test getting the perpenducular vector to two vectors
        /// </summary>
        [TestMethod]
        public void TestGetNormalTriangle1()
        {
            Point3D point1 = new Point3D(1, 0, 0);
            Point3D point2 = new Point3D(1, 1, 0);
            Point3D point3 = new Point3D(0, 1, 0);
            Vector3D normal = Helpers3D.GetNormal(point1, point2, point3);
            Vector3D expected = new Vector3D(0, 0, 1);
            Assert.AreEqual<Vector3D>(expected, normal);
        }
    }
}
