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


namespace View
{
    /// <summary>
    /// The visual 3D representation of a food object.
    /// </summary>
    /// <authors>Wesley Oates and Simon Redman</authors>
    /// <date>Nov 17, 2015</date>
    internal class Food3D : ModelVisual3D
    {
        /// <summary>
        /// The game model for this GUI Food3D.  Note that once created, the game model should never change for 
        /// food objects.
        /// </summary>       
        protected internal readonly Cube Model;

        /// <summary>
        /// The standard mesh to use for a food object.
        /// </summary>
        protected static MeshGeometry3D BasicMesh { get; private set; }

        
       /// <summary>
       /// A static constructor will instantiate all necessary static resources before any instances 
       /// are created.
       /// </summary>
        static Food3D()
        {
            Point3D[] pts = new Point3D[4];
            pts[3] = new Point3D(FoodSize, 0.0, 1);
            double ang = (2.0 * Math.PI) / 3.0;
            pts[2] = new Point3D(FoodSize * Math.Cos(ang), FoodSize * Math.Sin(ang), 1);
            ang = (2.0 * Math.PI) * (2.0 / 3.0);
            pts[1] = new Point3D(FoodSize * Math.Cos(ang), FoodSize * Math.Sin(ang), 1);
            pts[0] = new Point3D(0, 0, FoodSize);

            BasicMesh = new MeshGeometry3D();
            BasicMesh.Positions = new Point3DCollection(pts);
            Int32[] triIndices = { 0, 2, 1, 0, 3, 2, 0, 1, 3 };
            BasicMesh.TriangleIndices = new Int32Collection(triIndices);

        }

        /// <summary>
        /// Ths basic color of this Food3D.
        /// </summary>
        public Color Color
        {
            get
            {
                return Brush.Color;
            }
        }

        /// <summary>
        /// Cached reference to the solid color brush object that forms the basis of this Food3D's paint.
        /// </summary>
        protected internal readonly SolidColorBrush Brush;


        /// <summary>
        /// The radial standard size of the food object.
        /// </summary>
        protected const double FoodSize = 5.0;

        /// <summary>
        /// Create a new Food3D object based on the given model.
        /// </summary>        
        public Food3D(Cube model)
        {  
            
            //Create the model
            GeometryModel3D geom = new GeometryModel3D();
            geom.Geometry = BasicMesh;
            Brush = new SolidColorBrush(model.Color);
            geom.Material = new DiffuseMaterial(Brush);
            this.Content = geom;
            
            //Set the data model.            
            Model = model;

            //Set the positioning transform
            this.Transform = new TranslateTransform3D(model.X, model.Y, 0);
            

        }
        

    }
}
