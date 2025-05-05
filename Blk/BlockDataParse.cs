using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSMapConv.VHE;
namespace MCSMapConv
{
    public static class BlockDataParse
    {
        public static int Rotation16(int data)
        {
            return AngleLimit((int)(data * 22.5));
        }

        public static int Rotation4(int data)
        {
            return AngleLimit(data * 90);
        }

        public static Point Rotation6(int data)
        {
            float x = 0, y = 0;

            switch (data)
            {
                case 1:
                    x = 180;
                    break;

                case 2:
                    x = 270;
                    break;

                case 3:
                    x = 90;
                    break;

                case 4:
                    y = 90;
                    break;

                case 5:
                    y = 270;
                    break;
            }

            return new Point(x, y, 0);
        }

        public static Point Rotation8(int data)
        {
            float x = 0, z = 0;

            int ang = data % 4;
            if (data > 3)
            {
                x = 180;
            }

            switch (ang)
            {
                case 1:
                    z = 180;
                    break;

                case 2:
                    z = 90;
                    break;

                case 3:
                    z = 270;
                    break;
            }

            return new Point(x, 0, z);
        }

        public static Point GetRotation(BlockDescriptor.RotationType rotationType, int data)
        {
            switch (rotationType)
            {
                default:
                    return new Point(0, 0, 0);

                case BlockDescriptor.RotationType.R4:
                    return new Point(0, 0, Rotation4(data));

                case BlockDescriptor.RotationType.R6:
                    return Rotation6(data);

                case BlockDescriptor.RotationType.R8:
                    return Rotation8(data);

                case BlockDescriptor.RotationType.R16:
                    return new Point(0, 0, Rotation16(data));
            }
        }

        /**/

        private static int AngleLimit(int value)
        {
            if (value >= 360)
            {
                return value - 360;
            }
            else if (value < 0)
            {
                return value + 360;
            }

            return value;
        }
    }
}
