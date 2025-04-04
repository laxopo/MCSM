using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv.VHE
{
    public class Point : ICloneable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Point() { }

        public Point(Point point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public Point(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point(float[] xyz)
        {
            X = xyz[0];
            Y = xyz[1];
            Z = xyz[2];
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public Point Copy()
        {
            return Clone() as Point;
        }
    }
}
