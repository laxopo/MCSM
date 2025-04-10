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
            int vali = (int)(data * 22.5);
            if (vali >= 360)
            {
                vali -= 360;
            }
            else if (vali < 0)
            {
                vali += 360;
            }

            return vali;
        }
    }
}
