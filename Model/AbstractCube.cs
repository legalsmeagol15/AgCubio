using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Model
{
    public abstract class AbstractCube
    {
        public readonly int Uid;


        public int X { get; set; }

        public int Y { get; set; }


        public Color Color { get; set; }
        
        public AbstractCube(int id, int x, int y, Color color)
        {
            this.Uid = id;
            this.X = x;
            this.Y = y;
            this.Color = color;
        }
    }
}
