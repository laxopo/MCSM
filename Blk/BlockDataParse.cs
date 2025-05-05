using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
