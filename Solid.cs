using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class Solid : SolidID
    {
        public int Xmin { get; set; }
        public int Xmax { get; set; }
        public int Ymin { get; set; }
        public int Ymax { get; set; }
        public int Zmin { get; set; }
        public int Zmax { get; set; }
        public bool XClosed { get; set; }
        public bool YClosed { get; set; }
        public bool ZClosed { get; set; }
        public bool XBegTouch { get; set; }
        public bool YBegTouch { get; set; }
        public bool XEndTouch { get; set; }
        public bool YEndTouch { get; set; }
        public Orient Orientation { get; set; }
        public SolidType Type { get; set; }

        public enum Orient
        {
            None,
            X,
            Y,
            Z
        }

        public enum SolidType
        {
            Normal,
            Pane,
            Fence
        }

        public static Dictionary<string, SolidType> SType = new Dictionary<string, SolidType>() {
            { "Normal", SolidType.Normal },
            { "Pane", SolidType.Pane },
            { "Fence", SolidType.Fence }
        };

        public Solid() { }

        public Solid(int id, int dat, int x, int y, int z)
        {
            BlockID = id;
            BlockData = dat;
            Xmin = x;
            Xmax = x + 1;
            Ymin = y;
            Ymax = y + 1;
            Zmin = z;
            Zmax = z + 1;
        }

        public void Expand(int x, int y, int z)
        {
            if (x >= Xmax)
            {
                Xmax = x + 1;
                if (Type == SolidType.Pane)
                {
                    Orientation = Orient.X;
                }
            }

            if (y >= Ymax)
            {
                Ymax = y + 1;
                if (Type == SolidType.Pane)
                {
                    Orientation = Orient.Y;
                }
            }

            if (z >= Zmax)
            {
                Zmax = z + 1;
            }
        }

        public Solid[] Cut(int x, int y, int z)
        {
            Solid[] solids = new Solid[2];

            if (!XClosed)
            {
                XClosed = x >= Xmax;
            }
            else if (!YClosed)
            {
                YClosed = true;

                if (x > Xmin)
                {
                    Ymax--;
                    solids[0] = new Solid(BlockID, BlockData, Xmin, y, z);
                    solids[0].Xmax = x;
                    solids[0].XClosed = true;
                }
            } 
            else if (!ZClosed)
            {
                ZClosed = true;

                if (x > Xmin || y > Ymin)
                {
                    Zmax--;
                }

                if (y > Ymin)
                {
                    solids[1] = new Solid(BlockID, BlockData, Xmin, Ymin, z);
                    solids[1].Xmax = Xmax;
                    solids[1].Ymax = y;
                    solids[1].XClosed = true;
                    solids[1].YClosed = true;
                }

                if (x > Xmin)
                {
                    solids[0] = new Solid(BlockID, BlockData, Xmin, y, z);
                    solids[0].Xmax = x;
                    solids[0].XClosed = true;
                }
            }

            return solids;
        }
    }
}
