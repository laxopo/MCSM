using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM.VHE
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

        public Point2D Substract(Point2D substractor)
        {
            X -= substractor.X;
            Y -= substractor.Y;

            return this;
        }

        public Point2D Delta(Point2D substracted)
        {
            X = substracted.X - X;
            Y = substracted.Y - Y;
            return this;
        }

        public Point2D Summ(Point2D addition)
        {
            X += addition.X;
            Y += addition.Y;
            return this;
        }

        public Point2D Multiply(Point2D addition)
        {
            X *= addition.X;
            Y *= addition.Y;
            return this;
        }

        public Point2D Multiply(float x, float y)
        {
            X *= x;
            Y *= y;
            return this;
        }

        public Point2D Divide(Point2D divider)
        {
            X /= divider.X;
            Y /= divider.Y;
            return this;
        }

        public static Point2D Delta(Point2D pt1, Point2D pt2)
        {
            return new Point2D()
            {
                X = pt2.X - pt1.X,
                Y = pt2.Y - pt1.Y
            };
        }

        public static Point2D Summ(Point2D pt1, Point2D pt2)
        {
            return new Point2D()
            {
                X = pt2.X + pt1.X,
                Y = pt2.Y + pt1.Y
            };
        }

        public static Point2D Multiply(Point2D add1, Point2D add2)
        {
            return new Point2D()
            {
                X = add1.X * add2.X,
                Y = add1.Y * add2.Y
            };
        }
    }
}
