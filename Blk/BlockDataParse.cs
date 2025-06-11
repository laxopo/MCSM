using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCSM.VHE;
using NamedBinaryTag;
namespace MCSM
{
    public static class BlockDataParse
    {
        public static int Rotation16(int data)
        {
            return AngleLimit((int)(data * 22.5));
        }

        public static int Rotation4L(int data)
        {
            return AngleLimit(data * 90);
        }

        public static int Rotation4L(int x, int y, int z)
        {
            int data = Math.Abs((int)World.GetLocationSeed(x, y, z)) % 4;
            return AngleLimit(data * 90);
        }

        public static int Rotation4Z(int data)
        {
            switch (data)
            {
                default:
                    return 0;

                case 3:
                    return 180;

                case 4:
                    return 270;

                case 5:
                    return 90;
            }
        }

        public static int Rotation4(int data)
        {
            switch (data)
            {
                default:
                    return 0;

                case 1:
                    return 180;

                case 2:
                    return 90;

                case 3:
                    return 270;
            }
        }

        public static Point Rotation6(int data)
        {
            float x = 0, z = 0;

            switch (data)
            {
                case 0:
                    x = 180;
                    break;

                case 2:
                    x = 90;
                    break;

                case 3:
                case 4:
                case 5:
                    x = 270;
                    z = Rotation4Z(data);
                    break;
            }

            return new Point(x, 0, z);
        }

        public static Point Rotation6Button(int data)
        {
            float x = 0, y = 0, z = 0;

            switch (data)
            {
                case 1:
                    x = 270;
                    z = 90;
                    break;

                case 2:
                    x = 270;
                    z = 270;
                    break;

                case 3:
                    x = 270;
                    z = 180;
                    break;

                case 4:
                    x = 270;
                    break;

                case 5:
                    y = 180;
                    break;
            }

            return new Point(x, y, z);
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

        public static Point Rotation8A(int data)
        {
            float y = 0, z = 0;

            switch (data)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    y = 270;
                    z = Rotation4(data - 1);
                    break;

                case 5:
                    y = 180;
                    z = 270;
                    break;

                case 6:
                    y = 180;
                    z = 180;
                    break;

                case 7:
                    z = 90;
                    break;
            }

            return new Point(0, y, z);
        }

        public static Point GetRotation(BlockDescriptor.RotationType rotationType, int data, Block block)
        {
            switch (rotationType)
            {
                default:
                    return new Point(0, 0, 0);

                case BlockDescriptor.RotationType.R4L:
                    return new Point(0, 0, Rotation4L(data));

                case BlockDescriptor.RotationType.R4L_Rand:
                    return new Point(0, 0, Rotation4L(block.X, block.Y, block.Z));

                case BlockDescriptor.RotationType.R4Z:
                    return new Point(0, 0, Rotation4Z(data));

                case BlockDescriptor.RotationType.R6:
                    return Rotation6(data);

                case BlockDescriptor.RotationType.R6B:
                    return Rotation6Button(data);

                case BlockDescriptor.RotationType.R8:
                    return Rotation8(data);

                case BlockDescriptor.RotationType.R8A:
                    return Rotation8A(data);

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
