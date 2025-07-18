﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM.VHE
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

        public Point Summ(params Point[] add)
        {
            foreach (var pt in add)
            {
                X += pt.X;
                Y += pt.Y;
                Z += pt.Z;
            }

            return this;
        }

        public Point Substract(Point sub)
        {
            X -= sub.X;
            Y -= sub.Y;
            Z -= sub.Z;

            return this;
        }

        public static Point Divide(Point divided, float divider)
        {
            var pt = divided.Copy();

            pt.X /= divider;
            pt.Y /= divider;
            pt.Z /= divider;

            return pt;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public Point Copy()
        {
            return Clone() as Point;
        }

        public Point GetShifted(float dx, float dy, float dz)
        {
            var buf = Copy();
            buf.X += dx;
            buf.Y += dy;
            buf.Z += dz;
            return buf;
        }
    }
}
