using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NamedBinaryTag;

namespace MCSMapConv
{
    public class BlockGroup : BGroupID
    {
        public Block Block { get; set; }
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
        public ModelType Type { get; set; }

        public enum Orient
        {
            None,
            X,
            Y,
            Z
        }

        public enum ModelType
        {
            Normal,
            Pane,
            Fence,
            Door,
            Liquid,
            Grass,
            Special,
            Sign
        }

        public static Dictionary<string, ModelType> SType = new Dictionary<string, ModelType>() {
            { "NORMAL", ModelType.Normal },
            { "PANE", ModelType.Pane },
            { "FENCE", ModelType.Fence },
            { "DOOR", ModelType.Door },
            { "LIQUID", ModelType.Liquid },
            { "GRASS", ModelType.Grass },
            { "SPECIAL", ModelType.Special },
            { "SIGN", ModelType.Sign },
        };

        public BlockGroup() { }

        public BlockGroup(int id, int dat, int x, int y, int z)
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
                if (Type == ModelType.Pane)
                {
                    Orientation = Orient.X;
                }
            }

            if (y >= Ymax)
            {
                Ymax = y + 1;
                if (Type == ModelType.Pane)
                {
                    Orientation = Orient.Y;
                }
            }

            if (z >= Zmax)
            {
                Zmax = z + 1;
            }
        }

        public BlockGroup[] Cut(int x, int y, int z)
        {
            BlockGroup[] solids = new BlockGroup[2];

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
                    solids[0] = new BlockGroup(BlockID, BlockData, Xmin, y, z);
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
                    solids[1] = new BlockGroup(BlockID, BlockData, Xmin, Ymin, z);
                    solids[1].Xmax = Xmax;
                    solids[1].Ymax = y;
                    solids[1].XClosed = true;
                    solids[1].YClosed = true;
                }

                if (x > Xmin)
                {
                    solids[0] = new BlockGroup(BlockID, BlockData, Xmin, y, z);
                    solids[0].Xmax = x;
                    solids[0].XClosed = true;
                }
            }

            return solids;
        }

        public static ModelType GetSolidType(string typeName)
        {
            return SType[typeName.ToUpper()];
        }
    }
}
