using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Model;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Media.Animation;

namespace View
{


    /// <summary>
    /// The visual representation of a Cube game model.  A Cube3D may be added to the children of a Viewport3D 
    /// for easy three-dimensional display.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    internal class Cube3D : ModelVisual3D
    {

        private Cube _Model;
        /// <summary>
        /// The game model for this GUI Cube3D.
        /// </summary>
        protected internal Cube Model
        {
            get
            {
                return _Model;
            }
            set
            {
                _Model = value;
                if (this.X != _Model.Y)
                    this.X = _Model.X;
                if (this.Y != Model.Y)
                    this.Y = _Model.Y;
                if (!_Model.Color.Equals(Brush.Color))
                    Brush.Color = _Model.Color;

                //TODO:  Implement a mass-changing animation, rather than instantaneous change.
                //double newSize = Math.Sqrt(_Model.Size);
                //DoubleAnimation resizer =
                //    new DoubleAnimation(newSize, new Duration(TimeSpan.FromSeconds(1.0)));
                //resizer.IsAdditive = true;
                //resizer.IsCumulative = false;
                //this.BeginAnimation(SizeProperty, resizer);
                Size = _Model.Size;

            }
        }


        /// <summary>
        /// The materials used to render this Cube3D.
        /// </summary>
        private readonly MaterialGroup Materials;


        /// <summary>
        /// The translator that sets the cube at the proper X,Y,Z
        /// </summary>
        private readonly TranslateTransform3D Translation;

        /// <summary>
        /// The scaler that sets the size of the Cube3D for animations.
        /// </summary>
        private readonly ScaleTransform3D Sweller;

        /// <summary>
        /// The scaler that sets the size of the Cube3D for animations.
        /// </summary>
        private readonly ScaleTransform3D Sizer;

        /// <summary>
        /// The rotator that spins the Cube3D for animations.
        /// </summary>
        private readonly Rotation3D Rotator;

        

        /// <summary>
        /// Returns the apparent top of this Cube3D.
        /// </summary>
        public double Top
        {
            get
            {                
                return Model.Size / (1 + Squash);
            }
        }


        /// <summary>
        /// Ths basic color of this Cube3D.
        /// </summary>
        public Color Color
        {
            get
            {
                return Brush.Color;
            }
        }
        /// <summary>
        /// Reference to the solid color brush object that forms the basis of this Cube3D's paint.
        /// </summary>
        private readonly SolidColorBrush Brush;


        /// <summary>
        /// The mesh describing the shape of this Cube3D object.
        /// </summary>
        protected internal MeshGeometry3D Mesh { get; private set; }


        /// <summary>
        /// The number of of layers or slices with which this GUI model will be drawn.  A Cube3D object 
        /// of size 8, for example, will actually be an 8x8x8 set of .125x.125x.125-size cubes.
        /// </summary>
        protected const int Layers = 8;



        /// <summary>
        /// Creates a new renderable Cube3D object based on the given play model.
        /// </summary>        
        public Cube3D(Cube model)
        {            
            if (model.IsFood)
                throw new ArgumentException("Cannot create a Cube3D from a food model.");

            //Get the newly created shape for this Cube3D
            Mesh = GetUnitCube(Squash, Twist);

            //Set up the first materials
            this.Materials = new MaterialGroup();
            Brush = new SolidColorBrush(model.Color);
            this.Materials.Children.Add(new DiffuseMaterial(Brush));

            //Load up the Geometry with the materials and the created mesh.
            GeometryModel3D geom = new GeometryModel3D(Mesh, Materials);
            geom.BackMaterial = new DiffuseMaterial(Brushes.Yellow);

            //Set the current content to the new Geometry.
            this.Content = geom;

            //Set up transforms.
            Sweller = new ScaleTransform3D(1, 1, 1, 0, 0, 0);
            Sizer = new ScaleTransform3D(0.1, 0.1, 0.1, 0, 0, 0);
            Rotator = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            Translation = new TranslateTransform3D(0, 0, 0);
            Transform3DGroup tg = new Transform3DGroup();            
            tg.Children.Add(new RotateTransform3D(Rotator));
            tg.Children.Add(Sizer);
            tg.Children.Add(Sweller);            
            tg.Children.Add(Translation);
            this.Transform = tg;

            //Set up the name tag.
            SetupNameTag(model.Name);

            //Set this model to the new model.
            this.Model = model;

        }



        #region Cube3D animation dependency properties

        //Note:  The following dependency properties are created in the standard format as described 
        //by microsoft in their documentation.  For brevity, the static DependencyProperty declarations' 
        //descriptions are omitted, while the CLR property that accesses the DependencyProperty is 
        //fully commented.  This is a common pattern in msdn.  Please don't dock points for it.

        public static DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(Cube3D),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnPositionChanged)));
        /// <summary>
        /// The X coordinate of the center of this Cube3D in (x,y,z) space.
        /// </summary>
        public double X
        {
            get
            {
                return (double)GetValue(XProperty);
            }
            set
            {
                SetValue(XProperty, value);
            }
        }


        public static DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(Cube3D),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnPositionChanged)));
        /// <summary>
        /// The Y coordinate of the center of this Cube3D in (x,y,z) space.
        /// </summary>
        public double Y
        {
            get
            {
                return (double)GetValue(YProperty);
            }
            set
            {
                SetValue(YProperty, value);
            }
        }


        public static DependencyProperty ElevationProperty =
           DependencyProperty.Register("Elevation", typeof(double), typeof(Cube3D),
               new PropertyMetadata(0.0, new PropertyChangedCallback(OnPositionChanged)));
        /// <summary>
        /// The elevation above the play plane for the bottom face of this Cube3D, or in other words, 
        /// the Z value of the position (x,y,z).  Used for animations.
        /// </summary>
        public double Elevation
        {
            get
            {
                return (double)GetValue(ElevationProperty);
            }
            set
            {
                SetValue(ElevationProperty, value);
            }
        }


        /// <summary>
        /// The callback for a changed position dependency property.
        /// </summary>
        private static void OnPositionChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Cube3D)sender).OnPositionChanged(e);
        }
        /// <summary>
        /// Called when this Cube3D object's position changes.
        /// </summary>
        /// <param name="e"></param>
        protected void OnPositionChanged(DependencyPropertyChangedEventArgs e)
        {
            Translation.OffsetX = this.X;
            Translation.OffsetY = this.Y;
            Translation.OffsetZ = this.Elevation;
        }




        public static DependencyProperty SizeProperty =
           DependencyProperty.Register("Size", typeof(double), typeof(Cube3D),
               new PropertyMetadata(0.1, new PropertyChangedCallback(OnSizeChanged)));
        public double Size
        {
            get
            {
                return (double)GetValue(SizeProperty);
            }
            set
            {
                SetValue(SizeProperty, value);
            }
        }

        private static void OnSizeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Cube3D)obj).OnSizeChanged(e);
        }
        /// <summary>
        /// Increases or decreases the natural (ie, mass-based) size of this Cube3D object.
        /// </summary>  
        protected void OnSizeChanged(DependencyPropertyChangedEventArgs e)
        {
            double newSize = (double)e.NewValue;
            Sizer.ScaleX = newSize;
            Sizer.ScaleY = newSize;
            Sizer.ScaleZ = newSize;
        }


        
        public static DependencyProperty SwellProperty =
            DependencyProperty.Register("Swell", typeof(double), typeof(Cube3D),
                new PropertyMetadata(1.0, new PropertyChangedCallback(OnSwellChanged)));
        /// <summary>
        /// The additional size transform beyond the standard size.  This is used for the breathing 
        /// animation.
        /// </summary>
        public double Swell
        {
            get
            {
                return (double)GetValue(SwellProperty);
            }
            set
            {
                SetValue(SwellProperty, value);
            }
        }
        private static void OnSwellChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Cube3D)obj).OnSwellChanged(e);
        }
        /// <summary>
        /// Increases or decreases the natural (ie, mass-based) size of this Cube3D object.
        /// </summary>  
        protected void OnSwellChanged(DependencyPropertyChangedEventArgs e)
        {
            double newSwell = (double)e.NewValue;
            Sweller.ScaleX = newSwell;
            Sweller.ScaleY = newSwell;
            Sweller.ScaleZ = newSwell;
        }




        public static DependencyProperty SquashProperty =
            DependencyProperty.Register("Squash", typeof(double), typeof(Cube3D),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnShapeChanged)));
        /// <summary>
        /// The squashiness of this Cube3D.  If Squash is 0.0, this will portray a perfect cube.  A negative 
        /// squash will stretch the cube vertically, while a positive squash will (of course) flatten it.
        /// </summary>
        public double Squash
        {
            get
            {
                return (double)GetValue(SquashProperty);
            }
            set
            {
                SetValue(SquashProperty, value);
            }
        }


        public static DependencyProperty TwistProperty =
           DependencyProperty.Register("Twist", typeof(double), typeof(Cube3D),
               new PropertyMetadata(0.0, new PropertyChangedCallback(OnShapeChanged)));
        /// <summary>
        /// The amount of twistiness in the cube, in terms of multiples of pi.
        /// </summary>
        public double Twist
        {
            get
            {
                return (double)GetValue(TwistProperty);
            }
            set
            {
                SetValue(TwistProperty, value);
            }
        }



        private static void OnShapeChanged(object obj, DependencyPropertyChangedEventArgs e)
        {
            ((Cube3D)obj).OnShapeChanged(e);
        }
        //Called when the shape is changed.  It will redefine the mesh of the cube.
        protected void OnShapeChanged(DependencyPropertyChangedEventArgs e)
        {
            double twistInPi = Twist * Math.PI;
            Mesh.Positions = new Point3DCollection(GetUnitCubePoints(Squash, twistInPi));
            //UpdateTagSize();
        }



        #endregion



        #region Cube3D shape definition members


        /// <summary>
        /// Creates and returns the points of a "unit cube", ie, a 1x1x1 cube.
        /// </summary>         
        protected Point3D[] GetUnitCubePoints(double squash, double twist)
        {
            //Find the maximum Z for the given squash value.
            double maxZ = Math.Abs(1 / (1 + squash));       
            
            //Figure out the difference in Z values per layer.     
            double zStep = maxZ / Layers;

            //Find the angle from radial to radial.
            double angleStep = Math.PI / (Layers * 2);

            //Set up the result structure.
            //The size of the array will account for Layers + 1 slice across the cube, times the 
            //number of layers * 4 around the cube.  In addition, four points will signify the top
            //most corners.
            Point3D[] result = new Point3D[((Layers + 1) * Layers * 4) + 4];
            int ptIdx = 0;

            //Add the points from bottom to top, going around axis at each step.
            for (int layerIdx = 0; layerIdx<= Layers; layerIdx++)
            {
                double z = layerIdx * zStep;
                for (int radialIdx = 0; radialIdx < Layers * 4; radialIdx++)
                {
                    //Establish the squash ratio.
                    double squashRatio = 1.0 + (Math.Sin(Math.PI * (z / maxZ)) * squash);

                    //Calculate the twist at this Z-level.
                    double thisTwist = twist * (z / maxZ);

                    //What angle will this be?
                    double angle = thisTwist + (radialIdx * angleStep);

                    //What is the amplitude at this angle?
                    double hyp = squashRatio * GetAmplitude(radialIdx * angleStep);

                    //Everything needed to describe the point is now in place.  Add the point.
                    Point3D newPt = new Point3D(hyp * Math.Cos(angle), hyp * Math.Sin(angle), z);                    
                    result[ptIdx++] = newPt;
                }
            }

            //The following four points describe the top square of the cube.
            double ang = twist + (Math.PI / 4);
            double amp = Math.Sqrt(2 * (Math.Pow(0.5, 2.0)));
            result[ptIdx] = new Point3D(amp * Math.Cos(ang), amp * Math.Sin(ang), maxZ);    //length-4
            ang += (Math.PI / 2);
            result[ptIdx+1] = new Point3D(amp * Math.Cos(ang), amp * Math.Sin(ang), maxZ);  //length-3
            ang += (Math.PI / 2);
            result[ptIdx+2] = new Point3D(amp * Math.Cos(ang), amp * Math.Sin(ang), maxZ);  //length-2
            ang += (Math.PI / 2);
            result[ptIdx+3] = new Point3D(amp * Math.Cos(ang), amp * Math.Sin(ang), maxZ);  //length-1

            //Phew.  All done.  Return the result.
            return result;
        }

        /// <summary>
        /// Returns the mesh and triangle indices that describe a cube.
        /// </summary>
        /// <param name="squash">The measure by which the cube should be squashed.  A negative number 
        /// will stretch the cube vertically, while a positive number will flatten the cube like a 
        /// pancake.</param>
        /// <param name="twist">The degree to which the cube twists itself up like a churro.  The 
        /// value given must be in multiples of pi.</param>        
        protected MeshGeometry3D GetUnitCube(double squash, double twist)
        {
            Point3D[] pts = GetUnitCubePoints(squash, twist);

            int interval = Layers * 4;

            //The count of triangle indices will indicate a count of squares around the side faces 
            //equal to Layers * Layers * 4, doubled for two triangles per square, tripled for three 
            //points per triangle.  In addition, add six points to describe the top face.
            Int32[] triIndices = new Int32[(Layers*Layers*4*3*2) + 6];
            int triIdx = 0;
            //Console.WriteLine(Layers + "  " + interval + "  " + pts.Length);
            //Create the triangle indices for the side walls.
            for (int zIdx = 0; zIdx < Layers; zIdx++)
            {
                int nextRad = (zIdx + 1) * interval;
                int radIdx;
                for ( radIdx = (zIdx * interval); radIdx < nextRad-1; radIdx++)
                {
                    triIndices[triIdx] = radIdx;
                    triIndices[triIdx + 1] = radIdx + 1;
                    triIndices[triIdx + 2] = radIdx + interval;

                    triIndices[triIdx + 3] = radIdx + 1;
                    triIndices[triIdx + 4] = radIdx + interval + 1;
                    triIndices[triIdx + 5] = radIdx + interval;
                    

                    triIdx += 6;
                }

                //Add the last joint
                //This must be added separately from the radIdx loop because it links the last points
                //in a Z-level to the first points in the same Z-level.
                triIndices[triIdx] = radIdx;
                triIndices[triIdx+1] = radIdx - interval + 1;
                triIndices[triIdx+2] = radIdx + interval;

                triIndices[triIdx+3] = radIdx + interval;
                triIndices[triIdx+4] = radIdx - interval + 1;
                triIndices[triIdx+5] = radIdx + 1;

                triIdx += 6;
            }

            //Describe the top face in two triangles.
            triIndices[triIdx] = pts.Length - 4;
            triIndices[triIdx+2] = pts.Length - 2;
            triIndices[triIdx+1] = pts.Length - 3;

            triIndices[triIdx+3] = pts.Length - 4;
            triIndices[triIdx + 5] = pts.Length - 1;
            triIndices[triIdx + 4] = pts.Length - 2;

            //Establish the collections
            Point3DCollection pt3DCollection = new Point3DCollection(pts);
            Int32Collection intCollection = new Int32Collection(triIndices);
           
            //Set up and return the resulting mesh.
            MeshGeometry3D result = new MeshGeometry3D();
            result.Positions = pt3DCollection;
            result.TriangleIndices = intCollection;
            return result;
        }

        


        /// <summary>
        /// Returns the distance from the center of a square to its edge, at the given angle in radians.
        /// The amplitude distances returned will describe a 1x1 unit square.
        /// </summary>        
        /// <param name="angle">The angle, in radians, at which the distance is being measured.</param>        
        /// <returns>Returns the distance from the center to the edge at that angle.  May return a negative 
        /// number if the respective sin or cos is negative.</returns>
        /// <remarks>Imagine the square transected by two diagonal lines, dividing the square into four 
        /// triagonal wedges.  The distance from the center to the edge of a square at a given angle A is equal 
        /// to (halfSize / cos(A)) on the right and left wedges, and (halfSize / sin(A)) on the top and 
        /// bottom wedges.</remarks>
        private static double GetAmplitude(double angle)
        {
            //Boundary cases occur at diagonals:
            //      ___________
            //      |\       /|
            //      |  \   /  |
            //      |    x    | -----<-start here, angle==0
            //      |  /   \  |
            //      |/_______\|


            //Need the half-size of the square.
            
            angle = angle % (Math.PI * 2);
            if (angle < Math.PI / 4)
                return Math.Abs(0.5 / Math.Cos(angle));
            if (angle >= Math.PI / 4 && angle < (3 * Math.PI) / 4)
                return Math.Abs(0.5 / Math.Sin(angle));
            if (angle >= (3 * Math.PI) / 4 && angle < (5 * Math.PI) / 4)
                return Math.Abs(0.5 / Math.Cos(angle));
            if (angle >= (5 * Math.PI) / 4 && angle < (7 * Math.PI) / 4)
                return Math.Abs(0.5 / Math.Sin(angle));

            //Last possibility is angle>(7*math.pi)/4
            return Math.Abs(0.5 / Math.Cos(angle));
        }



        #endregion




        #region Cube3D nametag members
        /// <summary>
        /// The reference to the nametag's mesh.  The reference is maintained for purpose of updating 
        /// points.
        /// </summary>
        private MeshGeometry3D _NameTagMesh;

        /// <summary>
        /// The visual that is projected on the name tag.  The reference is maintained for purposes of 
        /// changing the tag's text.
        /// </summary>
        private TextBlock _NameTextBlock;
        

        private void SetupNameTag(string text)
        {
            double tagSize = Size;

            Int32[] triIndices = { 0, 2, 3, 0, 1, 2 };
            
            Point[] txtCoords = { new Point(1,0), new Point(0, 0), new Point(0, 1), new Point(1, 1)};
            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions = new Point3DCollection(GetNameTagPoints());            
            mesh.TriangleIndices = new Int32Collection(triIndices);
            mesh.TextureCoordinates = new PointCollection(txtCoords);

            TextBlock txtBlk = new TextBlock();
            txtBlk.Foreground = Brushes.Black;
            txtBlk.Background = new SolidColorBrush(Color.FromScRgb(0.3f, 1.0f, 1.0f, 1.0f));
            txtBlk.Text = text;
            txtBlk.TextAlignment = TextAlignment.Center;           
            txtBlk.FontFamily = new FontFamily("Arial");
            mesh.Freeze();

            VisualBrush vb = new VisualBrush(txtBlk);
            vb.Opacity = 0.8;
            DiffuseMaterial difMat = new DiffuseMaterial(vb);            
            GeometryModel3D geom = new GeometryModel3D(mesh, difMat);
            geom.BackMaterial = new DiffuseMaterial(vb);            

            ModelVisual3D newTag = new ModelVisual3D();
            newTag.Content = geom;            

            this._NameTextBlock = txtBlk;
            this._NameTagMesh = mesh;
            this.Children.Clear();
            this.Children.Add(newTag);
        }

        //Updates the size of the name tag.
        protected void UpdateTagSize()
        {
            _NameTagMesh.Positions = new Point3DCollection(GetNameTagPoints());
            this._NameTextBlock.FontSize = Size / 4;
        }

        //Returns the four points that describe the nametag shape.
        private Point3D[] GetNameTagPoints()
        {
            double size = 1.0;
            Point3D center = new Point3D(0, 0, 1.1);

            Point3D[] result = new Point3D[4];
         
            result[0] = new Point3D(center.X - size, center.Y - (size / 2), center.Z); //Top left
            result[1] = new Point3D(center.X + size, center.Y - (size / 2), center.Z); //Top right
            result[2] = new Point3D(center.X + size, center.Y + (size / 2), center.Z); //Bottom Left
            result[3] = new Point3D(center.X - size, center.Y + (size / 2), center.Z); //Bottom Right
            return result;
        }
        #endregion



    }
}
