using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv.VHE
{
    public class Point2D : ICloneable
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point2D() { }

        public Point2D(Point2D point)
        {
            X = point.X;
            Y = point.Y;
        }

        public Point2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Point2D(float[] xy)
        {
            X = xy[0];
            Y = xy[1];
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public Point2D Copy()
        {
            return Clone() as Point2D;
        }

        public Point2D GetShifted(float dx, float dy)
        {
            var buf = Copy();
            buf.X += dx;
            buf.Y += dy;
            return buf;
        }
    }
}
