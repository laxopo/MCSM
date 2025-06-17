using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NamedBinaryTag;

namespace MCSM
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

        private static Dictionary<string, ModelType> STypeDictionary;

        public enum Orient
        {
            None,
            X,
            Xm,
            Xp,
            Y,
            Ym,
            Yp,
            Z
        }

        public enum ModelType
        {
            Normal,
            Path,
            Slab,
            Pane,
            Fence,
            Wall,
            Door,
            Gate,
            TrapDoor,
            Liquid,
            Grass,
            Special,
            Sign,
            Rail,
            Torch,
            Stairs,
        }

        public static ModelType SType(string type)
        {
            if (STypeDictionary == null)
            {
                STypeDictionary = new Dictionary<string, ModelType>();
                foreach (var key in Enum.GetNames(typeof(ModelType)))
                {
                    STypeDictionary.Add(key.ToUpper(), (ModelType)Enum.Parse(typeof(ModelType), key));
                }
            }

            return STypeDictionary[type.ToUpper()];
        }

        public BlockGroup() 
        {
            Block = new Block();
        }

        public BlockGroup(Block block, int id = -1, int data = -1, int x = 0, int y = 0, int z = 0)
        {
            if (block == null)
            {
                Block = new Block();
            }
            else
            {
                Block = block;
            }
            
            if (id == -1)
            {
                id = Block.ID;
            }

            if (data == -1)
            {
                data = Block.Data;
            }

            ID = id;
            Data = data;
            Name = block.Name;
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
                    solids[0] = new BlockGroup(Block, ID, Data, Xmin, y, z);
                    solids[0].Xmax = x;
                    solids[0].XClosed = true;
                    solids[0].Type = Type;
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
                    solids[1] = new BlockGroup(Block, ID, Data, Xmin, Ymin, z);
                    solids[1].Xmax = Xmax;
                    solids[1].Ymax = y;
                    solids[1].XClosed = true;
                    solids[1].YClosed = true;
                    solids[1].Type = Type;
                }

                if (x > Xmin)
                {
                    solids[0] = new BlockGroup(Block, ID, Data, Xmin, y, z);
                    solids[0].Xmax = x;
                    solids[0].XClosed = true;
                }
            }

            return solids;
        }

        public static ModelType GetSolidType(string typeName)
        {
            return SType(typeName);
        }
    }
}
