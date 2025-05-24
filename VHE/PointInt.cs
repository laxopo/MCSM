using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM.VHE
{
    public class PointInt : ICloneable
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public PointInt() { }

        public PointInt(PointInt point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public PointInt(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public PointInt(int[] xyz)
        {
            X = xyz[0];
            Y = xyz[1];
            Z = xyz[2];
        }

        public PointInt Summ(PointInt add)
        {
            X += add.X;
            Y += add.Y;
            Z += add.Z;

            return this;
        }

        public PointInt Substract(PointInt sub)
        {
            X -= sub.X;
            Y -= sub.Y;
            Z -= sub.Z;

            return this;
        }

        public static PointInt Divide(PointInt divided, int divider)
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

        public PointInt Copy()
        {
            return Clone() as PointInt;
        }

        public PointInt GetShifted(int dx, int dy, int dz)
        {
            var buf = Copy();
            buf.X += dx;
            buf.Y += dy;
            buf.Z += dz;
            return buf;
        }
    }
}
