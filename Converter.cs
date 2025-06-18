using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NamedBinaryTag;
using Newtonsoft.Json;
using System.IO;

namespace MCSM
{
    public static class Converter
    {
        public static float CSScale = 37;
        public static float TextureRes = 128;
        public static bool SkyBoxEnable = false;
        public static bool EntityNaming = false;  //(!) true can break down some entity functionality
        public static int Xmin, Ymin, Zmin, Xmax, Ymax, Zmax; //mc coordinates
        public static int Dimension;

        public static bool Aborted { get; private set; }
        public static int BlockCount { get; private set; }
        public static int BlockProcessed { get; private set; }
        public static int BlockCurrent { get; private set; }
        public static int GroupCurrent { get; private set; }
        public static int SolidsCurrent { get; private set; }
        public static int EntitiesCurrent { get; private set; }
        public static ProcessType Process { get; private set; }
        public static Messaging Message { get; private set; } = new Messaging();

        public static Config Config { get; set; }
        public static List<BlockDescriptor> BlockDescriptors { get; set; }
        public static List<VHE.WAD> Wads { get; set; }
        public static List<EntityScript> SignEntities { get; set; }
        public static List<EntityScript> SolidEntities { get; set; }
        public static List<EntityScript> SysEntities { get; set; }
        public static List<ModelScript> Models { get; set; }
        public static VHE.Map Map { get; private set; }
        public static List<BlockGroup> BlockGroups { get; private set; } = new List<BlockGroup>();
        public static World MCWorld { get; set; }


        private static FontDim FontDim = new FontDim();
        private static List<VHE.Entity> GenSysEntities = new List<VHE.Entity>();
        public static Dictionary<Resources, string> Resource = new Dictionary<Resources, string>() {
            {Resources.Models, @"data\models.json"},
            {Resources.Blocks, @"data\blocks.json"},
            {Resources.SignEntities, @"data\sign_entities.json"},
            {Resources.SolidEntities, @"data\solid_entities.json"},
            {Resources.SysEntities, @"data\sys_entities.json"},
            {Resources.Config, @"config.json"}
        };

        public enum Resources
        {
            All,
            Models,
            Wad,
            Blocks,
            SignEntities,
            SolidEntities,
            SysEntities,
            Config
        }

        public enum ProcessType
        {
            Idle,
            ScanBlocks,
            GenerateSolids,
            TextureCheck,
            Done
        }

        public static void BlockInspect(Arguments args)
        {
            int dim = args.Range[0];
            int xmin = args.Range[1];
            int ymin = args.Range[2]; 
            int zmin = args.Range[3];
            int xmax = args.Range[4];
            int ymax = args.Range[5];
            int zmax = args.Range[6];

            var world = new World(args.WorldPath);

            Settings.DebugEnable = false;
            Console.WriteLine("Block data inspect");

            var list = new List<Block>();

            if (args.BlockID < 0)
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    Console.WriteLine();
                    Console.WriteLine("Y = " + y);
                    for (int z = zmin; z <= zmax; z++)
                    {
                        for (int x = xmin; x <= xmax; x++)
                        {
                            var block = world.GetBlock(dim, x, y, z);
                            Console.CursorLeft = (x - xmin) * 3;

                            if (block.ID != 0)
                            {
                                Console.Write(ToHexStr(block.ID));
                            }
                            else
                            {
                                Console.Write("--");
                            }
                        }
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    Console.WriteLine();
                    Console.WriteLine("Y = " + y);
                    for (int z = zmin; z <= zmax; z++)
                    {
                        for (int x = xmin; x <= xmax; x++)
                        {
                            var block = world.GetBlock(dim, x, y, z);

                            if (block.ID != args.BlockID)
                            {
                                if (block.ID == 0)
                                {
                                    Console.Write("- ");
                                }
                                else
                                {
                                    Console.Write("+ ");
                                }
                                continue;
                            }

                            if (args.NBTTag != null)
                            {
                                var ch = MCWorld.GetChunkAtBlock(0, x, z);
                                List<NBT> tes = ch.NBTData.GetTag("Level/" + args.NBTList);
                                foreach (var te in tes)
                                {
                                    int bx = te.GetTag("x");
                                    int by = te.GetTag("y");
                                    int bz = te.GetTag("z");
                                    string bid = te.GetTag("id");

                                    if (bx == x && by == y && bz == z && bid == args.BlockIDName)
                                    {
                                        int c = te.GetTag(args.NBTTag);
                                        Console.Write(ToHex(c) + " ");
                                    }
                                }
                            }
                            else
                            {
                                Console.Write(ToHex(block.Data) + " ");
                            }

                            list.Add(block);
                        }
                        Console.WriteLine();
                    }
                }
            }


            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static Model GetModel(BlockGroup bg, BlockDescriptor bt)
        {
            bg.Type = BlockGroup.SType(bt.ModelClass);
            return BuildModel(bg, bt, false);
        }

        public static VHE.Map ConvertToMap(string worldPath, Arguments args)
        {
            BlockProcessed = 0;
            Aborted = false;
            Dimension = args.Range[0];
            Xmin = args.Range[1];
            Ymin = args.Range[2];
            Zmin = args.Range[3];
            Xmax = args.Range[4];
            Ymax = args.Range[5];
            Zmax = args.Range[6];

            LoadResources(Resources.All);
            Macros.Initialize(CSScale);
            Modelling.Initialize(CSScale, TextureRes, Wads, SolidEntities);
            BlockCount = (Xmax - Xmin + 1) * (Zmax - Zmin + 1) * (Ymax - Ymin + 1);
            BlockCurrent = 0;
            GroupCurrent = 0;
            SolidsCurrent = 0;
            EntitiesCurrent = 0;

            /*if (!CheckWads())
            {
                Aborted = true;
                Process = ProcessType.Done;
            }*/

            MCWorld = new World(worldPath);
            Settings.DebugEnable = false;

            Process = ProcessType.ScanBlocks;
            InitializeMap();
            BlockGroups = new List<BlockGroup>();
            
            var missings = new BlockMissMsg(Message);

            //coordinates of cs map
            for (int z = MapOffZ(Ymin); z <= MapOffZ(Ymax); z++)
            {
                for (int y = MapOffY(Zmin); y <= MapOffY(Zmax); y++)
                {
                    for (int x = MapOffX(Xmin); x <= MapOffX(Xmax); x++)
                    {
                        var block = MCWorld.GetBlock(0, MCCX(x), MCCY(z), MCCZ(y));
                        BlockCurrent++;

                        //ignore
                        if (block.ID >= 8 && block.ID <= 11)
                        {
                            block.ID = 0;
                            block.Data = 0;
                        }

                        //check register
                    bt_chk:
                        var bt = GetBT(block, false);
                        if (block.ID != 0 && bt == null)
                        {
                            var res = missings.Message(block.ID, block.Data, "at " + MCCX(x) + " " + 
                                MCCY(z) + " " + MCCZ(y) + " is unregistered", true, args.Replace > -1);

                            switch (res)
                            {
                                case BlockMissMsg.Result.Retry:
                                    LoadResources(Resources.Blocks);
                                    goto bt_chk;

                                case BlockMissMsg.Result.Skip:
                                    block.Data = 0;
                                    if (args.Replace > -1)
                                    {
                                        block.ID = (byte)args.Replace;
                                        goto bt_chk;
                                    }
                                    else
                                    {
                                        block.ID = 0;
                                    }
                                    break;

                                case BlockMissMsg.Result.Abort:
                                    Aborted = true;
                                    return null;
                            }
                        }

                        if (bt != null && bt.ModelClass == null)
                        {
                            block.ID = 0;
                        }

                        if (block.ID != 0)
                        {
                            BlockProcessed++;
                        }

                        if (bt != null && bt.ModelClass != null)
                        {
                            var type = BlockGroup.SType(bt.ModelClass);
                            block.Name = bt.Name;

                            if (bt.ReplaceID != null)
                            {
                                var idd = Macros.Parse(bt.ReplaceID, null, false, bt, MCWorld, block, false);
                                var ids = idd.Split(':');

                                try
                                {
                                    if (ids.Length > 0)
                                    {
                                        block.ID = Convert.ToByte(ids[0]);
                                    }

                                    if (ids.Length > 1)
                                    {
                                        block.Data = Convert.ToByte(ids[1]);
                                    }
                                }
                                catch { }
                            }

                            switch (type)
                            {
                                case BlockGroup.ModelType.Pane:
                                case BlockGroup.ModelType.Fence:
                                    GroupPaneFence(block, bt, x, y, z);
                                    block.ID = 0;
                                    break;

                                case BlockGroup.ModelType.Door:
                                    if (block.Data < 8)
                                    {
                                        int data = MCWorld.GetBlock(0, MCCX(x), MCCY(z) + 1, MCCZ(y)).Data;
                                        if (data == 8)
                                        {
                                            data = block.Data;
                                        }
                                        else
                                        {
                                            data = block.Data + 8;
                                        }
                                        GroupSingle(block, x, y, z, BlockGroup.ModelType.Door, data);
                                    }
                                    block.ID = 0;
                                    break;

                                case BlockGroup.ModelType.TrapDoor:
                                case BlockGroup.ModelType.Gate:
                                case BlockGroup.ModelType.Grass:
                                case BlockGroup.ModelType.Sign:
                                case BlockGroup.ModelType.Torch:
                                case BlockGroup.ModelType.Stairs:
                                    GroupSingle(block, x, y, z, type, block.Data, bt.DataMask);
                                    break;
                            }
                        }

                        //Normal block
                        GroupNormal(block, x, y, z, bt);
                    }
                    BlockGroups.ForEach(x => x.XClosed = true);

                }
                BlockGroups.ForEach(x => x.YClosed = true);

            }
            BlockGroups.ForEach(x => x.ZClosed = true);

            //Generate cs solids
            Process = ProcessType.GenerateSolids;
            Aborted = false;
            foreach (var bg in BlockGroups)
            {
                GroupCurrent++;

                BlockDescriptor bt;
                while ((bt = GetBT(bg.ID, bg.Data)) == null)
                {
                    var res = missings.Message(bg.ID, bg.Data, "block is not registered", false);
                    switch (res)
                    {
                        case BlockMissMsg.Result.Abort:
                            Aborted = true;
                            return null;
                    }
                }

                bg.Data += bt.DataOffset;

                BuildModel(bg, bt);
            }

            //Generate skybox
            if (SkyBoxEnable)
            {
                var sbx = Xmax - Xmin + 1;
                var sby = Zmax - Zmin + 1;
                var sbz = Ymax - Ymin + 1;

                var model = new Model()
                {
                    Position = new VHE.Point(),
                    Solids = new List<Model.Solid>()
                    {
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(MOffX(0), MOffY(-1), MOffZ(0)),
                            Size = new VHE.Point(sbx, 1, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(MOffX(sbx), MOffY(-1), MOffZ(0)),
                            Size = new VHE.Point(1, sby + 2, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(MOffX(0), MOffY(sby), MOffZ(0)),
                            Size = new VHE.Point(sbx, 1, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(MOffX(-1), MOffY(-1), MOffZ(0)),
                            Size = new VHE.Point(1, sby + 2, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(MOffX(-1), MOffY(-1), MOffZ(sbz)),
                            Size = new VHE.Point(sbx + 2, sby + 2, 1),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(MOffX(-1), MOffY(-1), MOffZ(-1)),
                            Size = new VHE.Point(sbx + 2, sby + 2, 1),
                        }
                    }
                };

                Map.AddSolids(Modelling.GenerateSolids(model, "SKY"));
            }

            

            //Check textures
            Message.Mute = true;
            Process = ProcessType.TextureCheck;
            CheckTextures();
            Process = ProcessType.Done;
            return Map;
        }

        private static void InitializeMap()
        {
            Map = new VHE.Map();
            foreach (var wad in Config.WadFiles)
            {
                Map.AddString("worldspawn", "wad", wad);
            }
        }

        private static int MapOffX(int mcx)
        {
            return MOffX() + mcx - Xmin;
        }

        private static int MapOffY(int mcz)
        {
            return MOffY() + mcz - Zmin;
        }

        private static int MapOffZ(int mcy)
        {
            return MOffZ() + mcy - Ymin;
        }

        private static int MOffX(int x = 0)
        {
            return MOffset(Xmin, Xmax) + x;
        }

        private static int MOffY(int y = 0)
        {
            return MOffset(Zmin, Zmax) + y;
        }

        private static int MOffZ(int z = 0)
        {
            return MOffset(Ymin, Ymax) + z;
        }

        private static int MOffset(int mcmin, int mcmax)
        {
            int c0 = mcmin - mcmax;
            if (c0 % 2 != 0)
            {
                c0--;
            }

            c0 /= 2;
            return c0;
        }
        private static int MCCX(int x)
        {
            return Xmin - MOffset(Xmin, Xmax) + x;
        }

        private static int MCCY(int z)
        {
            return Ymin - MOffset(Ymin, Ymax) + z;
        }

        private static int MCCZ(int y)
        {
            return Zmin - MOffset(Zmin, Zmax) + y;
        }

        /*Grouping*/

        private static void GroupSingle(Block block, int x, int y, int z, string type, int data = -1, int dataMask = 0)
        {
            GroupSingle(block, x, y, z, BlockGroup.SType(type), data, dataMask);
        }

        private static void GroupSingle(Block block, int x, int y, int z, BlockGroup.ModelType type, 
            int data = -1, int dataMask = 0)
        {
            if (data == -1)
            {
                data = block.Data;
            }

            if (dataMask != 0)
            {
                data &= dataMask;
            }

            BlockGroups.Add(new BlockGroup(block, block.ID, data, x, y, z)
            {
                Type = type
            });

            block.ID = 0;
        }

        private static void GroupPaneFence(Block block, BlockDescriptor bt, int x, int y, int z)
        {
            bool px = false, py = false;
            var model = BlockGroup.SType(bt.ModelClass);

            //X
            var paneX = BlockGroups.Find(p => p.Type == model && !p.XClosed &&
                p.Xmax == x && p.Ymin == y && p.Orientation != BlockGroup.Orient.Y && p.ID == block.ID);

            if (paneX == null) //create new pane
            {
                paneX = new BlockGroup(block, block.ID, block.Data, x, y, z);
                paneX.Type = model;

                //X
                var bmx = MCWorld.GetBlock(0, MCCX(x) - 1, MCCY(z), MCCZ(y));
                var btmx = BlockDescriptors.Find(e => e.ID == bmx.ID);
                var nbmx = btmx != null && btmx.ModelClass == "Normal";
                var bpx = MCWorld.GetBlock(0, MCCX(x) + 1, MCCY(z), MCCZ(y));
                var btpx = BlockDescriptors.Find(e => e.ID == bpx.ID);
                var nbpx = btpx != null && btpx.ModelClass == "Normal";
                px = nbmx || nbpx || bpx.ID == block.ID;

                if (px)
                {
                    paneX.Orientation = BlockGroup.Orient.X;
                    paneX.XBegTouch = nbmx;
                    paneX.XEndTouch = nbpx && bpx.ID != block.ID;
                    paneX.XClosed = bpx.ID != block.ID;

                    if (!PaneMerge(paneX, z))
                    {
                        //create new pane
                        BlockGroups.Add(paneX);
                    }
                }
            }
            else //expand existing pane
            {
                paneX.Expand(x, y, z);
                px = true;

                var bp = MCWorld.GetBlock(0, MCCX(x) + 1, MCCY(z), MCCZ(y));
                var btp = BlockDescriptors.Find(e => e.ID == bp.ID);
                var nbp = btp != null && btp.ModelClass == "Normal";

                if (bp.ID != block.ID)
                {
                    paneX.XClosed = true;
                    if (nbp)
                    {
                        paneX.XEndTouch = true;
                    }

                    PaneMerge(paneX, z);
                }
            }


            //Y
            var paneY = BlockGroups.Find(p => p.Type == model && !p.YClosed &&
                p.Ymax == y && p.Xmin == x && p.Orientation != BlockGroup.Orient.X && p.ID == block.ID);

            if (paneY == null) //create new pane
            {
                paneY = new BlockGroup(block, block.ID, block.Data, x, y, z);
                paneY.Type = model;

                //Y
                var bmy = MCWorld.GetBlock(0, MCCX(x), MCCY(z), MCCZ(y) - 1);
                var btmy = BlockDescriptors.Find(e => e.ID == bmy.ID);
                var nbmy = btmy != null && btmy.ModelClass == "Normal";
                var bpy = MCWorld.GetBlock(0, MCCX(x), MCCY(z), MCCZ(y) + 1);
                var btpy = BlockDescriptors.Find(e => e.ID == bpy.ID);
                var nbpy = btpy != null && btpy.ModelClass == "Normal";
                py = nbmy || nbpy || bpy.ID == block.ID;

                if (py)
                {
                    paneY.Orientation = BlockGroup.Orient.Y;
                    paneY.YBegTouch = nbmy;
                    paneY.YEndTouch = nbpy && bpy.ID != block.ID;
                    paneY.YClosed = bpy.ID != block.ID;

                    if (!PaneMerge(paneY, z))
                    {
                        //create new pane
                        BlockGroups.Add(paneY);
                    }
                }
            }
            else //expand existing pane
            {
                paneY.Expand(x, y, z);
                py = true;

                var bp = MCWorld.GetBlock(0, MCCX(x), MCCY(z), MCCZ(y) + 1);
                var btp = BlockDescriptors.Find(e => e.ID == bp.ID);
                var nbp = btp != null && btp.ModelClass == "Normal";

                if (bp.ID != block.ID)
                {
                    paneY.YClosed = true;
                    if (nbp)
                    {
                        paneY.YEndTouch = true;
                    }

                    PaneMerge(paneY, z);
                }
            }

            //None
            if (!px && !py)
            {
                if (!PaneMerge(paneY, z))
                {
                    //create new pane
                    BlockGroups.Add(paneY);
                }
            }

            //Z
            if (model == BlockGroup.ModelType.Fence)
            {
                var pillar = BlockGroups.Find(p => p.Type == model && !p.ZClosed && p.ID == block.ID &&
                    p.Orientation == BlockGroup.Orient.Z && p.Xmin == x && p.Ymin == y && p.Zmax == z);

                if (pillar == null)
                {
                    pillar = new BlockGroup(block, block.ID, block.Data, x, y, z);
                    pillar.Type = model;
                    pillar.Orientation = BlockGroup.Orient.Z;

                    var bpz = MCWorld.GetBlock(0, MCCX(x), MCCY(z) + 1, MCCZ(y));
                    var btmy = BlockDescriptors.Find(e => e.ID == bpz.ID);
                    if (btmy == null || btmy.ID != block.ID)
                    {
                        pillar.ZClosed = true;
                    }

                    BlockGroups.Add(pillar);
                }
                else
                {
                    pillar.Expand(x, y, z);
                    var bpz = MCWorld.GetBlock(0, MCCX(x), MCCY(z) + 1, MCCZ(y));
                    var btmy = BlockDescriptors.Find(e => e.ID == bpz.ID);
                    if (btmy == null || btmy.ID != block.ID)
                    {
                        pillar.ZClosed = true;
                    }
                }
            }
        }

        private static void GroupWall(Block block, BlockDescriptor bt, int x, int y, int z)
        {
            foreach (var bg in BlockGroups)
            {
                if (bg.Type != BlockGroup.ModelType.Wall)
                {
                    continue;
                }
            }

            //new BGs
            var fxm = MCWorld.GetBlock(Dimension, MCCX(x) - 1, y, z).ID != 0;
            var fxp = MCWorld.GetBlock(Dimension, MCCX(x) + 1, y, z).ID != 0;
            var fym = MCWorld.GetBlock(Dimension, MCCX(x), y, z - 1).ID != 0;
            var fyp = MCWorld.GetBlock(Dimension, MCCX(x), y, z + 1).ID != 0;
            var fz = MCWorld.GetBlock(Dimension, MCCX(x), y + 1, z).ID != 0;

            if (!fz)
            {
                bool fx = !(fxm && fxp);
                bool fy = !(fym && fyp);
                bool fxy = fx && (fym ^ fyp);
                bool fyx = fy && (fxm ^ fxp);
                fz = fx || fy || fxy || fyx;
            }

            var orientList = new List<BlockGroup.Orient>();

            if (fxm || fxp)
            {
                orientList.Add(BlockGroup.Orient.X);
            }
            /*else if (fxm)
            {
                orientList.Add(BlockGroup.Orient.Xm);
            }
            else
            {
                orientList.Add(BlockGroup.Orient.Xp);
            }*/

            if (fym || fyp)
            {
                orientList.Add(BlockGroup.Orient.Y);
            }
            /*else if (fym)
            {
                orientList.Add(BlockGroup.Orient.Ym);
            }
            else
            {
                orientList.Add(BlockGroup.Orient.Yp);
            }*/

            if (fz)
            {
                orientList.Add(BlockGroup.Orient.Z);
            }

            orientList.ForEach(o => BlockGroups.Add(
                new BlockGroup(block, block.ID, block.Data, x, y, z) {
                    Orientation = o,
                    Type = BlockGroup.ModelType.Wall
                }));
        }

        private static void GroupNormal(Block block, int x, int y, int z, BlockDescriptor bt)
        {
            bool found = false;
            var cuts = new List<BlockGroup>();
            var types = new BlockGroup.ModelType[] {
                BlockGroup.ModelType.Normal,
                BlockGroup.ModelType.Liquid,
                BlockGroup.ModelType.Rail,
                BlockGroup.ModelType.Slab,
                BlockGroup.ModelType.Special,
                BlockGroup.ModelType.Path
            };

            foreach (var solid in BlockGroups)
            {
                bool typec = false;
                foreach (var t in types)
                {
                    if (t == solid.Type)
                    {
                        typec = true;
                        break;
                    }
                }

                if (!typec)
                {
                    continue;
                }

                bool grXY = false, grZ = false;

                if (bt != null)
                {
                    var gp = bt.GetGroupType();
                    grXY = gp == BlockDescriptor.GroupType.DataXY
                    || gp == BlockDescriptor.GroupType.XY
                    || gp == BlockDescriptor.GroupType.DataXYZ
                    || gp == BlockDescriptor.GroupType.XYZ;

                    grZ = gp == BlockDescriptor.GroupType.DataZ
                    || gp == BlockDescriptor.GroupType.Z
                    || gp == BlockDescriptor.GroupType.DataXYZ
                    || gp == BlockDescriptor.GroupType.XYZ;
                }

                var rngX = x >= solid.Xmin && x < solid.Xmax;
                var rngY = y >= solid.Ymin && y < solid.Ymax;
                var rngZ = z < solid.Zmax;
                var expX = !solid.XClosed && y == solid.Ymax - 1 && x == solid.Xmax;
                var expY = !solid.YClosed && y == solid.Ymax && rngX;
                var expZ = !solid.ZClosed && z == solid.Zmax && rngX && rngY;

                if (expX || expY || expZ || (rngX && rngY && rngZ))
                {
                    var exp = true;
                    if (((expX || expY) && !grXY) || (expZ && !grZ))
                    {
                        exp = false;
                    }

                    if (block.ID != 0 && exp && CompareID(block, solid.ID, solid.Data) && !found)
                    {
                        solid.Expand(x, y, z);
                        found = true;
                    }
                    else
                    {
                        cuts.AddRange(solid.Cut(x, y, z));
                    }
                }
            }

            foreach (var cut in cuts)
            {
                if (cut != null)
                {
                    BlockGroups.Add(cut);
                }
            }

            if (!found && block.ID != 0)
            {
                int data = block.Data;
                if (bt.DataMask > 0)
                {
                    data &= bt.DataMask;
                }
                BlockGroups.Add(new BlockGroup(block, block.ID, data, x, y, z) { 
                    Type = bt.GetSolidType()
                });
            }
        }

        private static bool PaneMerge(BlockGroup pane, int z)
        {
            if (pane.Type == BlockGroup.ModelType.Fence)
            {
                return false;
            }

            if (pane.XClosed || pane.YClosed)
            {
                //looking for the same pane in the previous Z layer
                var paneZ = BlockGroups.Find(pz => pz.Type == BlockGroup.ModelType.Pane &&
                    pz.Xmin == pane.Xmin && pz.Xmax == pane.Xmax &&
                    pz.Ymin == pane.Ymin && pz.Ymax == pane.Ymax && pz.Zmax == z &&
                    pz.XBegTouch == pane.XBegTouch && pz.XEndTouch == pane.XEndTouch &&
                    pz.YBegTouch == pane.YBegTouch && pz.YEndTouch == pane.YEndTouch &&
                    pz.ID == pane.ID && pz.Data == pane.Data);

                if (paneZ != null)
                {
                    paneZ.Zmax++;
                    BlockGroups.Remove(pane);
                    return true;
                }
            }

            return false;
        }

        /*Models*/

        private static Model BuildModel(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            switch (bg.Type)
            {
                case BlockGroup.ModelType.Normal:
                case BlockGroup.ModelType.Liquid:
                case BlockGroup.ModelType.Slab:
                case BlockGroup.ModelType.Path:
                    return ModelNormal(bg, bt, convEnable);

                case BlockGroup.ModelType.Pane:
                    return ModelPane(bg, bt, convEnable);

                case BlockGroup.ModelType.Fence:
                    if (convEnable)
                    {
                        ModelFence(bg, bt);
                    }
                    return new Model();

                case BlockGroup.ModelType.Door:
                    return ModelDoor(bg, bt, convEnable);

                case BlockGroup.ModelType.TrapDoor:
                    return ModelTrapDoor(bg, bt, convEnable);

                case BlockGroup.ModelType.Gate:
                    if (convEnable)
                    {
                        ModelGate(bg, bt);
                    }
                    return new Model();

                case BlockGroup.ModelType.Grass:
                    return ModelGrass(bg, bt, convEnable);

                case BlockGroup.ModelType.Special:
                    return ModelSpecial(bg, bt, convEnable);

                case BlockGroup.ModelType.Sign:
                    if (convEnable)
                    {
                        var text = GetSignText(Dimension, bg);
                        if (!GenerateSignEntity(bg, text))
                        {
                            ModelSign(bg, bt, text);
                        }
                        return new Model();
                    }
                    else
                    {
                        return ModelSign(bg, bt, new string[] { "Text 1", "Text 2" }, false);
                    }

                case BlockGroup.ModelType.Rail:
                    return ModelRail(bg, bt, convEnable);

                case BlockGroup.ModelType.Torch:
                    return ModelTorch(bg, bt, convEnable);

                case BlockGroup.ModelType.Stairs:
                    return ModelStairs(bg, bt, convEnable);

                default:
                    throw new Exception("Undefined model type.");
            }
        }

        private static Model ModelNormal(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            float szz, offz = 0;
            if (bg.Type != BlockGroup.ModelType.Slab)
            {
                szz = bg.Zmax - bg.Zmin;
            }
            else
            {
                szz = 0.5f;
                if (bg.Data >= 8)
                {
                    bg.Data -= 8;
                    offz = 0.5f;
                }
            }

            var size = new VHE.Point(bg.Xmax - bg.Xmin, bg.Ymax - bg.Ymin, szz);

            var model = new Model()
            {
                Origin = VHE.Point.Divide(size, 2),
                Rotation = BlockDataParse.GetRotation(bt.GetRotationType(), bg.Data, bg.Block),
                Position = new VHE.Point(bg.Xmin, bg.Ymin, bg.Zmin + offz),
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = size,
                        TextureLockOffsets = true
                    } 
                }
            };

            if (bg.Type == BlockGroup.ModelType.Liquid)
            {
                model.Solids[0].Size.Z -= 0.125f;
            } 
            else if (bg.Type == BlockGroup.ModelType.Path)
            {
                model.Solids[0].Size.Z -= 0.0625f;
            }

            if (convEnable)
            {
                MapAddObject(Modelling.GenerateSolid(bt, bg, model), bt, bg);
            }

            return model;
        }

        private static Model ModelPane(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            const float th = 0.125f;

            float edgeOffset = 0.5f - th / 2;
            float length = th, rot = 0;
            float beg = 0, end = 0;
            VHE.Point align = new VHE.Point(0, 0, 1);
            VHE.Point offset = new VHE.Point(0.5f, 0.5f, 0);
            var face = bt.GetTextureName(bg, null, "face", null);
            var edge = bt.GetTextureName(bg, null, "edge", null);
            string tl = face, tr = face, tf = face;

            var bti = bt.Copy();
            bti.Textures = new List<BlockDescriptor.TextureKey>() { 
                new BlockDescriptor.TextureKey() 
                { 
                    Key = "vert",
                    Texture = edge
                }
            };

            switch (bg.Orientation)
            {
                case BlockGroup.Orient.X:
                    if (!bg.XBegTouch)
                    {
                        beg = edgeOffset;
                    }
                    else
                    {
                        tl = edge;
                    }
                    if (!bg.XEndTouch)
                    {
                        end = edgeOffset;
                    }
                    else
                    {
                        tr = edge;
                    }
                    length = bg.Xmax - bg.Xmin - beg - end;
                    align = new VHE.Point(1, 0, 1);
                    offset = new VHE.Point(beg, 0.5f, 0);
                    break;

                case BlockGroup.Orient.Y:
                    if (!bg.YBegTouch)
                    {
                        beg = edgeOffset;
                    }
                    else
                    {
                        tl = edge;
                    }
                    if (!bg.YEndTouch)
                    {
                        end = edgeOffset;
                    }
                    else
                    {
                        tr = edge;
                    }
                    length = bg.Ymax - bg.Ymin - beg - end;
                    align = new VHE.Point(1, 0, 1);
                    offset = new VHE.Point(0.5f, beg, 0);
                    rot = 90;
                    break;
            }

            bti.Textures.Add(new BlockDescriptor.TextureKey()
            {
                Key = "face",
                Texture = tf
            });
            bti.Textures.Add(new BlockDescriptor.TextureKey()
            {
                Key = "left",
                Texture = tl
            });
            bti.Textures.Add(new BlockDescriptor.TextureKey()
            {
                Key = "right",
                Texture = tr
            });

            var model = new Model() {
                Solids = 
                { 
                    new Model.Solid() 
                    {
                        Size = new VHE.Point(length, th, bg.Zmax - bg.Zmin),
                        Rotation = new VHE.Point(0, 0, rot),
                        OriginAlign = align,
                        AbsOffset = offset,
                        TextureLockOffsets = true,
                        Faces = new List<Model.Face>
                        {
                            new Model.Face(Model.Faces.Top)
                            {
                                Rotation = 90
                            },
                            new Model.Face(Model.Faces.Bottom)
                            {
                                Rotation = 90
                            }
                        }
                    }
                }
            };

            if (!convEnable)
            {
                return model;
            }

            var solid = Modelling.GenerateSolid(bti, bg, model);
            MapAddObject(solid, bti, bg);

            return model;
        }

        private static void ModelFence(BlockGroup bg, BlockDescriptor bt)
        {
            //horizontal crossbars
            if (bg.Orientation != BlockGroup.Orient.Z)
            {
                float zmin = bg.Zmin + 0.375f;
                float zmax = bg.Zmin + 0.5625f;
                float xmin = bg.Xmin + 0.4375f;
                float xmax = bg.Xmax - 0.4375f;
                float ymin = bg.Ymin + 0.4375f;
                float ymax = bg.Ymax - 0.4375f;

                if (bg.Orientation == BlockGroup.Orient.X)
                {
                    if (bg.XBegTouch)
                    {
                        xmin = bg.Xmin;
                    }
                    else
                    {
                        xmin = bg.Xmin + 0.5f;
                    }

                    if (bg.XEndTouch)
                    {
                        xmax = bg.Xmax;
                    }
                    else
                    {
                        xmax = bg.Xmax - 0.5f;
                    }
                }
                else if (bg.Orientation == BlockGroup.Orient.Y)
                {
                    if (bg.YBegTouch)
                    {
                        ymin = bg.Ymin;
                    }
                    else
                    {
                        ymin = bg.Ymin + 0.5f;
                    }

                    if (bg.YEndTouch)
                    {
                        ymax = bg.Ymax;
                    }
                    else
                    {
                        ymax = bg.Ymax - 0.5f;
                    }
                }

                if (bg.Orientation != BlockGroup.Orient.None)
                {
                    var mdl = new Model() {
                        Solids = { new Model.Solid() {
                            Size = new VHE.Point(xmax - xmin, ymax - ymin, zmax - zmin),
                            TextureLockOffsets = true
                            } 
                        },
                        Position = new VHE.Point(xmin, ymin, zmin),
                    };
                    MapAddObject(Modelling.GenerateSolids(bt, bg, mdl), bt, bg);

                    mdl.Position.Z += 0.375f;
                    MapAddObject(Modelling.GenerateSolids(bt, bg, mdl), bt, bg);
                }
            }
            else //vertical pillars
            {
                var mdl = new Model() {
                    Solids = {
                        new Model.Solid() {
                            Size = new VHE.Point(0.25f, 0.25f, bg.Zmax - bg.Zmin),
                            AbsOffset = new VHE.Point(0.375f, 0.375f, 0),
                            TextureLockOffsets = true
                        },
                    },       
                };
                MapAddObject(Modelling.GenerateSolids(bt, bg, mdl), bt, bg);
            };
        }

        private static Model ModelDoor(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            const float th = 0.1875f;

            float rotate = 0, mRotate = 0, origOffset = 0;
            bool mirror = false;

            switch (bg.Data)
            {
                case 0:
                case 13:
                    break;

                case 1:
                case 14:
                    rotate = 90;
                    break;

                case 2:
                case 15:
                    rotate = 180;
                    break;

                case 3:
                case 12:
                    rotate = 270;
                    break;

                case 4:
                case 9:
                    rotate = 90;
                    break;

                case 5:
                case 10:
                    rotate = 180;
                    mirror = true;
                    break;

                case 6:
                case 11:
                    rotate = 270;
                    mirror = true;
                    break;

                case 7:
                case 8:
                    mirror = true;
                    break;
            }

            if (mirror)
            {
                origOffset = 1;
                mRotate = 180;
            }

            var model = new Model()
            {
                Origin = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = new VHE.Point(0, 0, rotate),
                Solids =
                {
                    new Model.Solid() //door
                    {
                        Name = "_main",
                        Size = new VHE.Point(th, 1, 2),
                        OriginAlign = new VHE.Point(1, 1, 1),
                        OriginRotOffset = new VHE.Point(th / 2, 0.5f, 0),
                        Rotation =  new VHE.Point(0, 0, mRotate),
                        TextureOriented = true,
                        Faces = new List<Model.Face>()
                        {
                            new Model.Face(Model.Faces.Top)
                            {
                                OffsetU = 13,
                                OffsetV = 16
                            },
                            new Model.Face(Model.Faces.Bottom)
                            {
                                OffsetU = 13,
                                OffsetV = 16
                            },
                            new Model.Face(Model.Faces.Rear)
                            {
                                OffsetU = 13,
                            }
                        }
                    },
                    new Model.Solid() //origin
                    {
                        Name = "origin",
                        Size = new VHE.Point(0.125f, 0.125f, 0.125f),
                        OriginAlign = new VHE.Point(0, 0, 0),
                        Offset = new VHE.Point(th / 2, origOffset, 1),
                    }
                },
                TextureKeys =
                {
                    new BlockDescriptor.TextureKey()
                    {
                        SolidName = "origin",
                        Texture = "origin"
                    }
                }
            };

            if (!convEnable)
            {
                return model;
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);

            return model;
        }

        private static Model ModelTrapDoor(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            var rot = BlockDataParse.Rotation4Z((bg.Block.Data & 3) + 2);
            float offz = 0;

            if ((bg.Block.Data & 8) != 0)
            {
                offz = 1 - 0.1875f;
            }

            var model = new Model()
            {
                Name = "TrapDoor",
                Origin = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = new VHE.Point(0, 0, rot),
                Solids = new List<Model.Solid>()
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(1, 1, 0.1875f),
                        Offset = new VHE.Point(0, 0, offz)
                    },
                    new Model.Solid()
                    {
                        Name = "origin",
                        Size = new VHE.Point(0.1875f, 0.1875f, 0.1875f),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Offset = new VHE.Point(0.5f, 1, offz)
                    }
                },
                TextureKeys = new List<BlockDescriptor.TextureKey>()
                {
                    new BlockDescriptor.TextureKey()
                    {
                        SolidName = "origin",
                        Texture = "origin"
                    }
                }
            };

            if (convEnable)
            {
                MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);
            }

            return model;
        }

        private static void ModelGate(BlockGroup bg, BlockDescriptor bt)
        {
            float rot = 0;
            if ((bg.Data & 1) != 0)
            {
                rot = 90;
            }

            var model = new Model()
            {
                Name = "Gate",
                Offset = new VHE.Point(0, 0, -0.005f),
                Origin = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = new VHE.Point(0, 0, rot),
                Solids = new List<Model.Solid>()
                {
                    new Model.Solid()
                    {
                        Name = "_L",
                        Size = new VHE.Point(0.125f, 0.125f, 0.6875f),
                        OriginAlign = new VHE.Point(1, 0, 1),
                        Offset = new VHE.Point(0, 0.5f, 0.3125f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "_U",
                        Size = new VHE.Point(0.375f, 0.125f, 0.1875f),
                        OriginAlign = new VHE.Point(1, 0, 1),
                        Offset = new VHE.Point(0.125f, 0.5f, 0.75f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "_D",
                        Size = new VHE.Point(0.375f, 0.125f, 0.1875f),
                        OriginAlign = new VHE.Point(1, 0, 1),
                        Offset = new VHE.Point(0.125f, 0.5f, 0.375f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "_C",
                        Size = new VHE.Point(0.125f, 0.125f, 0.1875f),
                        OriginAlign = new VHE.Point(1, 0, 1),
                        Offset = new VHE.Point(0.375f, 0.5f, 0.5625f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "origin",
                        Size = new VHE.Point(0.125f, 0.125f, 0.1875f),
                        OriginAlign = new VHE.Point(-0.8f, 0, 1),
                        Offset = new VHE.Point(0, 0.5f, 0.5625f),
                    }
                },
                TextureKeys = new List<BlockDescriptor.TextureKey>()
                {
                    new BlockDescriptor.TextureKey()
                    {
                        SolidName = "origin",
                        Texture = "origin"
                    }
                }
            };

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);
            model.Rotation.Z += 180;
            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);
        }

        private static Model ModelGrass(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            float len = (float)Math.Sqrt(2);
            float[] worldOffset = new float[2];

            if (bt.WorldOffset)
            {
                worldOffset = World.GetBlockXZOffset(MCCX(bg.Xmin), MCCZ(bg.Ymin));
            }

            var texture = Modelling.GetTexture(Wads, bt.GetTextureName(bg));
            if (texture == null)
            {
                return null;
            }

            float height = texture.Height / (int)TextureRes;

            var model = new Model()
            {
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(len, 0, height),
                        AbsOffset = new VHE.Point(0.5f + worldOffset[0], 0.5f + worldOffset[1], 0),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Rotation = new VHE.Point(0, 0, 45),
                        TextureOriented = true,
                        TexturedFaces = new Model.Faces[]
                        {
                            Model.Faces.Front,
                            Model.Faces.Rear
                        },
                        Faces = new List<Model.Face>
                        {
                            new Model.Face(Model.Faces.Front)
                            {
                                StretchU = true,
                                Frame = true,
                            },
                            new Model.Face(Model.Faces.Rear)
                            {
                                StretchU = true,
                                Frame = true,
                            }
                        }
                    },
                    new Model.Solid()
                    {
                        Size = new VHE.Point(len, 0, height),
                        AbsOffset = new VHE.Point(0.5f + worldOffset[0], 0.5f + worldOffset[1], 0),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Rotation = new VHE.Point(0, 0, -45),
                        TextureOriented = true,
                        TexturedFaces = new Model.Faces[]
                        {
                            Model.Faces.Front,
                            Model.Faces.Rear
                        },
                        Faces = new List<Model.Face>
                        {
                            new Model.Face(Model.Faces.Front)
                            {
                                StretchU = true,
                                Frame = true,
                            },
                            new Model.Face(Model.Faces.Rear)
                            {
                                StretchU = true,
                                Frame = true,
                            }
                        }
                    }
                },
            };

            if (!convEnable)
            {
                return model;
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);

            return model;
        }

        private static Model ModelSpecial(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            var modelName = Macros.Parse(bt.ModelName, bg, false, bt, MCWorld, bg.Block, false);
            var modelScr = Models.Find(x => x.Name == modelName);
            if (modelScr == null)
            {
                throw new Exception("Model not found");
            }

            var model = modelScr.ToModel(bt, bg, MCWorld, bg.Block);

            if (bt.GetRotationType() != BlockDescriptor.RotationType.None)
            {
                var rot = BlockDataParse.GetRotation(bt.GetRotationType(), bg.Data, bg.Block);
                model.Rotation.Summ(rot);
            }

            if (bt.WorldOffset)
            {
                var worldOffset = World.GetBlockXZOffset(MCCX(bg.Xmin), MCCZ(bg.Ymin));
                
                foreach (var sld in model.Solids)
                {
                    sld.AbsOffset.X += worldOffset[0];
                    sld.AbsOffset.Y += worldOffset[1];
                }
            }

            if (!convEnable)
            {
                return model;
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);

            return model;
        }

        private static Model ModelSign(BlockGroup bg, BlockDescriptor bt, string[] text, bool convEnable = true)
        {
            /*generate base model*/
            const float tScale = 0.66f;
            const float msx = 1, msy = 0.081f, msz = 0.49f;
            const float ssz = 0.59f;

            bool ground = bt.GetSolidTK("stick") != null;

            VHE.Point morigAligin, moffset;
            float rotate = 0;

            if (ground)
            {
                morigAligin = new VHE.Point(0, 0, 1);
                moffset = new VHE.Point(0.5f, 0.5f, ssz);
                rotate = BlockDataParse.Rotation16(bg.Data);
            }
            else
            {
                morigAligin = new VHE.Point(0, 1, 0);
                moffset = new VHE.Point(0.5f, 0, 0.5f);

                switch (bg.Data)
                {
                    case 2:
                        rotate = 180;
                        break;

                    case 3:
                        break;

                    case 4:
                        rotate = 90;
                        break;

                    case 5:
                        rotate = 270;
                        break;

                    default:
                        throw new Exception("Invalid block data value (sign). Must be equal 2...5");
                }
            }

            string mainName = "main";
            if (!ground)
            {
                mainName = "_" + mainName;
            }

            var main = new Model.Solid()
            {
                Name = mainName,
                Size = new VHE.Point(msx, msy, msz),
                OriginAlign = morigAligin,
                Offset = moffset,
                TextureScale = tScale
            };

            var stick = new Model.Solid()
            {
                Name = "stick",
                Size = new VHE.Point(msy, msy, ssz),
                OriginAlign = new VHE.Point(0, 0, 1),
                Offset = new VHE.Point(0.5f, 0.5f, 0),
                TextureScale = tScale
            };

            var model = new Model() 
            { 
                Origin = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = new VHE.Point(0, 0, rotate),
                Solids = new List<Model.Solid>() 
                { 
                    main 
                } 
            };

            if (ground)
            {
                model.Solids.Add(stick);
            }

            /*generate text*/
            if (text.Length > 0 && convEnable)
            {
                var textModel = new Model()
                {
                    Origin = model.Origin,
                    Rotation = model.Rotation
                };

                string fontTextureName = "{font";
                var texture = Modelling.GetTexture(Wads, fontTextureName);
                const int sch = 8; //char size in font pixels
                float mScale = 1.0f / 16 * tScale; //sign pixel scale
                float pScale = 0.25f * mScale; //font pixel scale
                float min = Modelling.MinSize;

                //char offsets
                float zoff = Modelling.GetEdgeMax(msz, morigAligin.Z) + moffset.Z - mScale;// - sch * pScale;
                float yoff = Modelling.GetEdgeMax(msy, morigAligin.Y) + moffset.Y;
                var fontScale = texture.Width / 16;

                bool uwarn = false;

                for (int r = 0; r < text.Length; r++)
                {
                    var row = text[r];
                    
                    //unicode to ascii convert
                    while (row.IndexOf("\\u") != -1)
                    {
                        int idx = row.IndexOf("\\u");
                        if (row.Length <= idx + 5)
                        {
                            Message.Write("Warning [{0}, {1}, {2}]: invalid unicode char code",
                                    bg.Block.X, bg.Block.Y, bg.Block.Z);
                            break;
                        }

                        var strcode = row.Substring(idx + 2, 4);

                        uint ascii;
                        try
                        {
                            ascii = uint.Parse(strcode, System.Globalization.NumberStyles.HexNumber) & 0x00FF;
                        }
                        catch
                        {
                            Message.Write("Warning [{0}, {1}, {2}]: invalid unicode char code",
                                    bg.Block.X, bg.Block.Y, bg.Block.Z);
                            break;
                        }

                        if (!uwarn)
                        {
                            Message.Write("Warning [{0}, {1}, {2}]: unicode char(s) converted to ASCII",
                                bg.Block.X, bg.Block.Y, bg.Block.Z);
                            uwarn = true;
                        }

                        row = row.Replace("\\u" + strcode, ((char)ascii).ToString());
                    }

                    //char offset and row align
                    float tzoff = (sch + 2) * r * pScale;
                    float txl = (float)(FontDim.GetStringWidth(texture, row) * pScale) + (row.Length - 1) * pScale;
                    float xShift = 0;

                    //generate char solids
                    for (int ch = 0; ch < row.Length; ch++)
                    {
                        int ascii = row[ch]; //char ascii code
                        var dim = FontDim.GetCharDim(texture, ascii);

                        //texture offset
                        float otx = 0, oty = 0;

                        //char size & offset
                        float oy = dim.OffsetY * pScale;
                        float sx = dim.Width * pScale;
                        float sy = dim.Height * pScale;
                        if (dim.Width == 1)
                        {
                            sx *= 1.3f;
                            otx = -0.325f * texture.Height / 128;
                        }
                        if (dim.Height == 1)
                        {
                            sy *= 1.3f;
                            oty = -0.325f * texture.Height / 128;
                        }

                        //char full offset x
                        float txoff = 0.5f - txl / 2 + xShift + sx / 2;

                        if (sx != 0 && sy != 0)
                        {
                            var chr = new Model.Solid()
                            {
                                Name = "char",
                                Size = new VHE.Point(sx, min * 2, sy),
                                OriginAlign = new VHE.Point(0, 0, -1),
                                Offset = new VHE.Point(txoff, yoff, zoff - tzoff - oy),
                                TextureScale = pScale / fontScale * 128 * (TextureRes / 16),
                                TexturedFaces = new Model.Faces[]
                                {
                                    Model.Faces.Front
                                },
                                    Faces = new List<Model.Face>()
                                {
                                    new Model.Face(Model.Faces.Front)
                                    {
                                        Texture = fontTextureName,
                                        OffsetU = ascii % 16 * fontScale + otx,
                                        OffsetV = ascii / 16 * fontScale + oty + dim.OffsetY * texture.Height / 128,
                                        UnscaledOffset = true
                                    }
                                }
                            };

                            textModel.Solids.Add(chr);
                        }
                        else
                        {
                            sx = 3 * pScale;
                        }

                        //update char row offset x
                        xShift += sx + 1 * pScale;
                    }
                }

                var entity = new VHE.Entity("func_illusionary")
                {
                    Parameters = new List<VHE.Entity.Parameter>()
                    {
                        new VHE.Entity.Parameter("rendermode", 4, VHE.Entity.Type.Int),
                        new VHE.Entity.Parameter("renderamt", 255, VHE.Entity.Type.Int)
                    }
                };

                MapAddObject(Modelling.GenerateSolids(null, bg, textModel), entity, bg);
            }

            if (!convEnable)
            {
                return model;
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);

            return model;
        }

        private static Model ModelRail(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            VHE.Point solidSize, solidRotOff, solidRot, rotation = new VHE.Point();
            float texRot = 0;
            var bti = bt.Copy();
            bti.Textures = new List<BlockDescriptor.TextureKey>();

            var szx = bg.Xmax - bg.Xmin;
            var szy = bg.Ymax - bg.Ymin;

            //solid
            if (bg.Data < 2 || bg.Data > 5) //horizontal
            {
                solidSize = new VHE.Point(szx, szy, 0);
                solidRotOff = new VHE.Point();
                solidRot = new VHE.Point();
            }
            else //angle
            {
                solidSize = new VHE.Point(1.42f, 1, 0);
                solidRotOff = new VHE.Point(0, 0, 0.03f);
                solidRot = new VHE.Point(0, -45, 0);

                texRot = 90;
            }

            //texture
            if (bg.Data < 6)
            {
                bti.Textures.Add(new BlockDescriptor.TextureKey()
                {
                    Texture = bt.GetTextureName(bg, null, "straight")
                });
            }
            else
            {
                bti.Textures.Add(new BlockDescriptor.TextureKey()
                {
                    Texture = bt.GetTextureName(bg, null, "turn")
                });
            }

            //rotating
            switch (bg.Data)
            {
                case 1:
                    texRot = 90;
                    break;

                case 2:
                case 3:
                case 4:
                case 5:
                    rotation = new VHE.Point(0, 0, BlockDataParse.Rotation4Z(bg.Data));
                    break;

                case 6:
                case 7:
                case 8:
                case 9:
                    rotation = new VHE.Point(0, 0, BlockDataParse.Rotation4L(bg.Data - 6));
                    break;
            }


            var model = new Model()
            {
                Name = "Rail",
                Origin = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = rotation,
                Solids = new List<Model.Solid>()
                {
                    new Model.Solid()
                    {
                        Size = solidSize,
                        OriginRotOffset = solidRotOff,
                        Rotation = solidRot,
                        TexturedFaces = new Model.Faces[]
                        {
                            Model.Faces.Top,
                            Model.Faces.Bottom
                        },
                        Faces = new List<Model.Face>()
                        {
                            new Model.Face(Model.Faces.Top)
                            {
                                Rotation = texRot
                            },
                            new Model.Face(Model.Faces.Bottom)
                            {
                                Rotation = texRot
                            }
                        }
                    }
                }
            };

            if (convEnable)
            {
                MapAddObject(Modelling.GenerateSolids(bti, bg, model), bti, bg);
            }

            return model;
        }

        private static Model ModelTorch(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            VHE.Point offset, origin, lpos;
            float rotY = 0, rotZ = 0;

            if (bg.Data == 5)
            {
                origin = new VHE.Point(0.5f, 0.5f, 0);
                offset = new VHE.Point(0.5f, 0.5f, 0);
                lpos = new VHE.Point(bg.Xmin + offset.X, bg.Ymin + offset.Y, bg.Zmin + 0.65f);
            }
            else if (bg.Data > 0 && bg.Data < 5)
            {
                origin = new VHE.Point(0.5f, 0.5f, 0);
                offset = new VHE.Point(0, 0.5f, 0.22f);
                rotY = 15;

                switch (bg.Data)
                {
                    case 1:
                        rotZ = 0;
                        break;

                    case 2:
                        rotZ = 180;
                        break;

                    case 3:
                        rotZ = 90;
                        break;

                    case 4:
                        rotZ = 270;
                        break;
                }

                lpos = new VHE.Point(offset);
                lpos.Z += 0.65f;
                lpos.X += 0.18f;
                Modelling.Rotate3D(lpos, origin, 0, 0, rotZ);
                lpos.Summ(new VHE.Point(bg.Xmin, bg.Ymin, bg.Zmin));
            }
            else
            {
                return null;
            }

            var model = new Model()
            {
                Name = "Torch",
                Origin = origin,
                Rotation = new VHE.Point(0, 0, rotZ),
                Solids = new List<Model.Solid>()
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(0.125f, 0.125f, 0.625f),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Offset = offset,
                        Rotation = new VHE.Point(0, rotY, 0),
                        Faces = new List<Model.Face>()
                    }
                }
            };

            for (int i = 0; i < 6; i++)
            {
                var face = model.Solids[0].Face(i);

                if (i == 1)
                {
                    face.OffsetU = 7;
                    face.OffsetV = 14;
                }
                else
                {
                    face.OffsetU = 7;
                    face.OffsetV = 6;
                }
            }

            if (!convEnable)
            {
                return model;
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);

            var light = new VHE.Entity("light")
            {
                Parameters = new List<VHE.Entity.Parameter>()
                {
                    new VHE.Entity.Parameter("_light", new int[] { 255, 255, 128, 100 }, VHE.Entity.Type.IntArray),
                    new VHE.Entity.Parameter("_falloff", 0, VHE.Entity.Type.Int),
                    new VHE.Entity.Parameter("_fade", 1.0f, VHE.Entity.Type.Float),
                    new VHE.Entity.Parameter("style", 0, VHE.Entity.Type.Int),
                }
            };

            MapAddEntity(light, lpos);

            return model;
        }

        private static Model ModelStairs(BlockGroup bg, BlockDescriptor bt, bool convEnable = true)
        {
            bool IsPinRight(int dat, int bdata)
            {
                switch (dat)
                {
                    case 0:
                        return bdata == 2;
                       
                    case 1:
                        return bdata == 3;
                       
                    case 2:
                        return bdata == 1;
                       
                    case 3:
                        return bdata == 0;
                }

                return false;
            }

            bool IsSide(int dat, int bdata)
            {
                switch (dat)
                {
                    case 0:
                    case 1:
                        return bdata == 2 || bdata == 3;


                    case 2:
                    case 3:
                        return bdata == 0 || bdata == 1;
                }

                return false;
            }

            bool BlockCheck(Block blk, bool hdata)
            {
                if (blk.ID != bg.ID)
                {
                    return false;
                }

                if (hdata != blk.Data > 3)
                {
                    return false;
                }

                return true;
            }

            bool DataCheck(Block blk, bool hdata, ref int dataout)
            {
                bool bc = BlockCheck(blk, hdata);

                if (blk.Data > 3)
                {
                    dataout -= 4;
                }

                return bc;
            }

            const float cut = 0.002f;
            float zbase = 0;
            var data = bg.Data;
            bool highData = false;
            if (data > 3)
            {
                zbase = 0.5f;
                data -= 4;
                highData = true;
            }

            bool bfr = false, br = false, bl = false;
            int[] pins = new int[] { 1, 1, 0, 0 };

            if (convEnable)
            {
                var points = new VHE.PointInt[]
                {
                    new VHE.PointInt(bg.Block.X, bg.Block.Y, bg.Block.Z), //front
                    new VHE.PointInt(bg.Block.X, bg.Block.Y, bg.Block.Z), //rear
                    new VHE.PointInt(bg.Block.X, bg.Block.Y, bg.Block.Z), //right
                    new VHE.PointInt(bg.Block.X, bg.Block.Y, bg.Block.Z)  //left
                };

                switch (data)
                {
                    case 0:
                        points[0].X = bg.Block.X - 1;
                        points[1].X = bg.Block.X + 1;
                        points[2].Z = bg.Block.Z + 1;
                        points[3].Z = bg.Block.Z - 1;
                        break;

                    case 1:
                        points[0].X = bg.Block.X + 1;
                        points[1].X = bg.Block.X - 1;
                        points[2].Z = bg.Block.Z - 1;
                        points[3].Z = bg.Block.Z + 1;
                        break;

                    case 2:
                        points[0].Z = bg.Block.Z - 1;
                        points[1].Z = bg.Block.Z + 1;
                        points[2].X = bg.Block.X - 1;
                        points[3].X = bg.Block.X + 1;
                        break;

                    case 3:
                        points[0].Z = bg.Block.Z + 1;
                        points[1].Z = bg.Block.Z - 1;
                        points[2].X = bg.Block.X + 1;
                        points[3].X = bg.Block.X - 1;
                        break;

                    default:
                        throw new Exception("Bad block data");
                }

                var blocks = new List<Block>();
                foreach (var pt in points)
                {
                    blocks.Add(MCWorld.GetBlock(Dimension, pt.X, pt.Y, pt.Z));
                }

                bfr = blocks[0].ID != 0;
                br = blocks[2].ID != 0;
                bl = blocks[3].ID != 0;

                if (BlockCheck(blocks[3], highData) && blocks[3].Data == bg.Data)
                {
                    pins[0] = 2;
                    pins[2] = -2;
                }

                if (BlockCheck(blocks[2], highData) && blocks[2].Data == bg.Data)
                {
                    pins[1] = 2;
                    pins[3] = -2;
                }

                for (int i = 0; i < 2; i++)
                {
                    var block = blocks[i];
                    int bdata = block.Data;

                    if (!DataCheck(block, highData, ref bdata))
                    {
                        continue;
                    }

                    if (!IsSide(data, bdata))
                    {
                        continue;
                    }

                    switch (i)
                    {
                        case 0: //front
                            if (IsPinRight(data, bdata))
                            {
                                if (pins[3] == 0)
                                {
                                    pins[3] = 1;
                                }
                            }
                            else
                            {
                                if (pins[2] == 0)
                                {
                                    pins[2] = 1;
                                }
                            }
                            break;

                        case 1: //rear
                            pins[2] = 0;
                            pins[3] = 0;
                            if (IsPinRight(data, bdata))
                            {
                                if (pins[0] == 1)
                                {
                                    pins[0] = 0;
                                }
                            }
                            else
                            {
                                if (pins[1] == 1)
                                {
                                    pins[1] = 0;
                                }
                            }
                            break;
                    }
                }
            }

            var rot = BlockDataParse.Rotation4(data);

            float bhrsy = 0, bhroy = 0;
            float bhflsy = 0, bhfloy = 0, bhflx = 0;
            float bhfrsy = 0, bhfroy = 0, bhfrx = 0;

            if (!highData)
            {
                if ((pins[0] <= 0 && !bl) || (pins[1] <= 0 && !br))
                {
                    bhrsy = cut;
                }

                if (pins[0] <= 0 && !bl)
                {
                    bhroy = cut;
                }

                if (pins[2] <= 0)
                {
                    if (!bl)
                    {
                        bhflsy += cut;
                        bhfloy = cut;
                    }

                    if (!bfr)
                    {
                        bhflx = cut;
                    }
                }

                if (pins[3] <= 0)
                {
                    if (!br)
                    {
                        bhfrsy = cut;
                    }

                    if (!bfr)
                    {
                        bhfrx = cut;
                    }
                }
            }

            var model = new Model()
            {
                Name = "Stairs",
                Origin = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = new VHE.Point(0, 0, rot),
                Solids = new List<Model.Solid>()
                {
                    new Model.Solid()
                    {
                        Name = "_baseL",
                        Size = new VHE.Point(1, 1, 0.25f),
                        Offset = new VHE.Point(0, 0, zbase),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "_baseHR",
                        Size = new VHE.Point(0.5f, 1 - bhrsy, 0.25f),
                        Offset = new VHE.Point(0.5f, bhroy, zbase + 0.25f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "_baseHFL",
                        Size = new VHE.Point(0.5f - bhflx, 0.5f - bhflsy, 0.25f),
                        Offset = new VHE.Point(bhflx, bhfloy, zbase + 0.25f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                    new Model.Solid()
                    {
                        Name = "_baseHFR",
                        Size = new VHE.Point(0.5f - bhfrx, 0.5f - bhfrsy, 0.25f),
                        Offset = new VHE.Point(bhfrx, 0.5f - bhfroy, zbase + 0.25f),
                        TextureLockOffsets = true,
                        TextureLockRotanion = true
                    },
                }
            };

            for (int i = 0; i < 4; i++)
            {
                var pin = pins[i];
                if (pin <= 0)
                {
                    continue;
                }

                float sx = 0, sy = 0;
                if (!highData)
                {
                    switch (i)
                    {
                        case 0:
                            if (pins[1] <= 0)
                            {
                                sy = cut;
                            }
                            if (pins[2] <= 0)
                            {
                                sx = cut;
                            }
                            break;

                        case 1:
                            if (pins[0] <= 0)
                            {
                                sy = cut;
                            }
                            if (pins[3] <= 0)
                            {
                                sx = cut;
                            }
                            break;

                        case 2:
                            if (pins[0] <= 0)
                            {
                                sx = cut;
                            }
                            if (pins[3] <= 0)
                            {
                                sy = cut;
                            }
                            break;

                        case 3:
                            if (pins[1] <= 0)
                            {
                                sx = cut;
                            }
                            if (pins[2] <= 0)
                            {
                                sy = cut;
                            }
                            break;
                    }
                }
                
                switch (i)
                {
                    case 0:
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "H",
                            Size = new VHE.Point(0.5f - sx, 0.5f - sy, 0.25f),
                            Offset = new VHE.Point(0.5f + sx, 0, 0.75f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "L",
                            Size = new VHE.Point(0.5f, 0.5f, 0.25f),
                            Offset = new VHE.Point(0.5f, 0, 0.5f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        break;

                    case 1:
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "H",
                            Size = new VHE.Point(0.5f - sx, 0.5f - sy, 0.25f),
                            Offset = new VHE.Point(0.5f + sx, 0.5f + sy, 0.75f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "L",
                            Size = new VHE.Point(0.5f, 0.5f, 0.25f),
                            Offset = new VHE.Point(0.5f, 0.5f, 0.5f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        break;

                    case 2:
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "H",
                            Size = new VHE.Point(0.5f - sx, 0.5f - sy, 0.25f),
                            Offset = new VHE.Point(0, 0, 0.75f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "L",
                            Size = new VHE.Point(0.5f, 0.5f, 0.25f),
                            Offset = new VHE.Point(0, 0, 0.5f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        break;

                    case 3:
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "H",
                            Size = new VHE.Point(0.5f - sx, 0.5f - sy, 0.25f),
                            Offset = new VHE.Point(0, 0.5f + sy, 0.75f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        model.Solids.Add(new Model.Solid()
                        {
                            Name = "_pin" + i + "L",
                            Size = new VHE.Point(0.5f, 0.5f, 0.25f),
                            Offset = new VHE.Point(0, 0.5f, 0.5f - zbase),
                            TextureLockOffsets = true,
                            TextureLockRotanion = true
                        });
                        break;
                }
            }

            if (!convEnable)
            {
                return model;
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt, bg);

            return model;
        }

        /*Map methods*/

        private static void MapAddEntity(VHE.Entity entity, VHE.Point position)
        {
            MapAddEntity(entity, position.X, position.Y, position.Z);
        }

        private static void MapAddEntity(VHE.Entity entity, float x, float y, float z)
        {
            x *= CSScale;
            y *= -CSScale;
            z *= CSScale;

            var pos = new VHE.Point(x, y, z);
            entity.Parameters.Add(new VHE.Entity.Parameter("origin", pos, VHE.Entity.Type.Point));

            Map.Data.Add(entity);
        }

        private static void MapAddObject(VHE.Solid solid, BlockDescriptor bt, BlockGroup bg)
        {
            MapAddObject(new List<VHE.Solid>() { solid }, bt, bg); 
        }

        private static void MapAddObject(List<VHE.Solid> solids, BlockDescriptor bt, BlockGroup bg)
        {
            var solidList = new List<VHE.Solid>(solids);
            var includes = new List<VHE.Solid>();

            //get included solids
            foreach (var sld in solidList)
            {
                foreach (var inc in sld.IncludedSolids)
                {
                    var incs = solidList.FindAll(x => x.Name == inc);
                    if (incs.Count == 0)
                    {
                        continue;
                    }

                    foreach (var incsld in incs)
                    {
                        if (includes.Contains(incsld))
                        {
                            continue;
                        }

                        includes.Add(incsld);
                    }
                }
            }

            //build internal & included entities
            var ies = new List<VHE.Solid>();
            for (int i = 0; i < solidList.Count; i++)
            {
                var sld = solidList[i];

                if (sld.Entity == null)
                {
                    continue;
                }

                var ie = GetEntity(SolidEntities, sld.Entity);
                if (ie == null)
                {
                    continue;
                }

                ies.Add(sld);

                if (sld.HasOrigin)
                {
                    ies.Add(solidList[i + 1]);
                    i++;
                }

                foreach (var inc in sld.IncludedSolids)
                {
                    var incs = includes.FindAll(x => x.Name == inc);
                    foreach (var incsld in incs)
                    {
                        if (ies.Contains(incsld) || sld == incsld)
                        {
                            continue;
                        }

                        ies.Add(incsld);
                    }

                    
                }

                var entity = GenerateEntity(ie, bg, "E" + Map.Data.Count);
                MapAddObject(ies, entity, bg);
            }

            //remove builded solids with ies
            ies.ForEach(s => solidList.Remove(s));

            //build solids & entities
            var se = GetEntity(SolidEntities, bt.Entity);
            if (se != null)
            {
                var entity = GenerateEntity(se, bg, "E" + Map.Data.Count);
                MapAddObject(solidList, entity, bg);
            }
            else
            {
                solidList.ForEach(s => Map.AddSolid(s));
                SolidsCurrent += solidList.Count;
            }

            //build system entities
            foreach (var syse in bt.SysEntities)
            {
                var et = GetEntity(SysEntities, syse);
                var sysEntity = GenerateEntity(et, bg, "E" + Map.Data.Count, bt);
                if (sysEntity == null)
                {
                    continue;
                }

                bool found = false;
                foreach (var gse in GenSysEntities)
                {
                    if (gse.Compare(sysEntity, "origin"))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    MapAddEntity(sysEntity, GenSysEntities.Count * 2, -1, 0);
                    GenSysEntities.Add(sysEntity);
                }
            }
        }

        private static void MapAddObject(List<VHE.Solid> solids, VHE.Entity entity, BlockGroup bg)
        {
            var ent = entity.Copy();
            solids.ForEach(s => ent.AddSolid(s));
            Map.CreateEntity(ent);
            EntitiesCurrent += solids.Count;
        }

        private static EntityScript GetEntity(List<EntityScript> list, string entityName)
        {
            if (entityName != null)
            {
                return list.Find(x => x.Macros.ToUpper() == entityName.ToUpper());
            }

            return null;
        }

        private static BlockDescriptor GetBT(int id, int data)
        {
            var blk = new Block() { ID = (byte)id, Data = (byte)data };
            return GetBT(blk);
        }

        private static BlockDescriptor GetBT(Block block, bool suppressData = false)
        {
            int maskedData = block.Data;

            bool CheckData(BlockDescriptor bt)
            {
                if (bt.DataMask > 0)
                {
                    maskedData &= bt.DataMask;
                }

                if (bt.DataMax > 0 && maskedData > bt.DataMax)
                {
                    return false;
                }

                if (suppressData)
                {
                    block.Data = (byte)maskedData;
                }

                return true;
            }

            BlockDescriptor Engine()
            {
                foreach (var bt in BlockDescriptors)
                {
                    if (bt.ID == block.ID)
                    {
                        var chk = CheckData(bt);
                        if (bt.DataExceptions != null)
                        {
                            if (bt.DataExceptions.Contains(maskedData))
                            {
                                continue;
                            }
                        }

                        if (bt.Data == -1) //Ignore the data value
                        {
                            if (!chk)
                            {
                                if (bt.IgnoreExcluded)
                                {
                                    return new BlockDescriptor();
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            return bt;
                        }
                        else
                        {
                            if (bt.Data != block.Data)
                            {
                                continue;
                            }

                            if (chk)
                            {
                                return bt;
                            }

                            if (bt.IgnoreExcluded)
                            {
                                return new BlockDescriptor();
                            }
                        }
                    }
                }

                return null;
            }

            BlockDescriptor b;
            while ((b = Engine()) != null)
            {
                if (b.ReferenceID <= 0)
                {
                    break;
                }

                block.ID = (byte)b.ReferenceID;

                if (b.ReferenceData > 0)
                {
                    block.Data = (byte)b.ReferenceData;
                }
                
                maskedData = block.Data;
            }

            return b;
        }

        private static bool CompareID(Block block, int id, int data)
        {
            if (block.ID != id)
            {
                return false;
            }

            foreach (var bt in BlockDescriptors)
            {
                if (bt.ID != block.ID)
                {
                    continue;
                }

                var gp = bt.GetGroupType();
                var grF = gp == BlockDescriptor.GroupType.XY
                    || gp == BlockDescriptor.GroupType.XYZ
                    || gp == BlockDescriptor.GroupType.Z;

                if (bt.Data == -1 && grF) //Ignore the data value
                {
                    return true;
                }

                int dat;
                if (bt.DataMask != 0)
                {
                    dat = data & bt.DataMask;
                }
                else
                {
                    if (bt.DataMax != 0 && data > bt.DataMax)
                    {
                        dat = -1;
                    }
                    else
                    {
                        dat = data;
                    }
                }

                if (block.Data == dat)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] GetSignText(int dimension, BlockGroup bg)
        {
            var x = bg.Block.X;
            var y = bg.Block.Y;
            var z = bg.Block.Z;

            var buf = new List<string>();
            var chunk = MCWorld.GetChunkAtBlock(dimension, x, z);
            List<NBT> tes = chunk.NBTData.GetTag("Level/TileEntities");

            foreach (var te in tes)
            {
                if (te.GetTag("id") == "minecraft:sign")
                {
                    int tex = te.GetTag("x");
                    int tey = te.GetTag("y");
                    int tez = te.GetTag("z");

                    if (x == tex && y == tey && z == tez)
                    {
                        for (int i = 4; i >= 1; i--)
                        {
                            string row = te.GetTag("Text" + i);
                            if (row == null)
                            {
                                continue;
                            }

                            int idx = row.IndexOf("\"text\":");
                            int beg = row.IndexOf("\"", idx + 7);
                            int end = row.IndexOf("\"", beg + 1);
                            row = row.Substring(beg + 1, end - beg - 1);

                            if (row != "")
                            {
                                buf.Add(row);
                            }
                        }

                        buf.Reverse();
                        break;
                    }
                }
            }

            return buf.ToArray();
        }

        private static bool GenerateSignEntity(BlockGroup bg, string[] text)
        {
            if (text.Length == 0)
            {
                return false;
            }

            try
            {
                var se = Macros.GetSignEntity(SignEntities, text);

                if (se != null)
                {
                    Map.Data.Add(GenerateEntity(se, bg));
                    return true;
                }
            }
            catch (Exception e)
            {
                if (e.InnerException == Macros.Exceptions.ESNotFound)
                {
                    Message.Write("Undefined macros {0} at {1} {2} {3}", e.Message,
                        bg.Block.X, bg.Block.Y, bg.Block.Z);
                }
            }
            
            return false;
        }

        private static VHE.Entity GenerateEntity(EntityScript entityTemplate, BlockGroup bg, string name = null, BlockDescriptor bt = null)
        {
            var entity = new VHE.Entity(entityTemplate.ClassName);

            foreach (var part in entityTemplate.Parameters)
            {
                var par = new VHE.Entity.Parameter(part.Name);
                par.SetType(part.ValueType);

                string value = part.Value;
                try
                {
                    value = Macros.EntityValue(value, bg, bt);
                    par.SetValue(value);
                    entity.Parameters.Add(par);
                }
                catch (Exception e)
                {
                    if (e == Macros.Exceptions.SubMacrosUndef)
                    {
                        Message.Write(e.InnerException.Message + e.Message + 
                            " at " + bg.Block.X + " " + bg.Block.Y + " " + bg.Block.Z);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }

            if (EntityNaming && name != null)
            {
                entity.AddParameter("targetname", name, VHE.Entity.Type.String);
            }

            return entity;
        }

        /**/

        private static char ToHex(int value)
        {
            if (value < 10)
            {
                return (char)(value + 0x30);
            }
            else
            {
                return (char)(value + 0x37);
            }
        }

        private static string ToHexStr(int value)
        {
            if (value < 0x10)
            {
                return "0" + ToHex(value);
            }
            else
            {
                return "" + ToHex(value / 16) + ToHex(value % 16);
            }
        }

        /**/

        public static void LoadResources(Resources res)
        {
            if (res == Resources.Config || res == Resources.All)
            {
                Config = JsonConvert.DeserializeObject<Config>(
                    File.ReadAllText(Resource[Resources.Config]));

                CSScale = Config.BlockScale;
                TextureRes = Config.TextureResolution;
            }

            if (res == Resources.Wad || res == Resources.All)
            {
                Wads = new List<VHE.WAD>();
                foreach (var wad in Config.WadFiles)
                {
                    Wads.Add(new VHE.WAD(wad));
                }
            }

            if (res == Resources.Blocks || res == Resources.All)
            {
                BlockDescriptors = JsonConvert.DeserializeObject<List<BlockDescriptor>>(
                    File.ReadAllText(Resource[Resources.Blocks]));
            }

            if (res == Resources.Models || res == Resources.All)
            {
                Models = JsonConvert.DeserializeObject<List<ModelScript>>(
                    File.ReadAllText(Resource[Resources.Models]));
            }

            if (res == Resources.SignEntities || res == Resources.All)
            {
                SignEntities = JsonConvert.DeserializeObject<List<EntityScript>>(
                    File.ReadAllText(Resource[Resources.SignEntities]));
            }

            if (res == Resources.SolidEntities || res == Resources.All)
            {
                SolidEntities = JsonConvert.DeserializeObject<List<EntityScript>>(
                    File.ReadAllText(Resource[Resources.SolidEntities]));
            }

            if (res == Resources.SysEntities || res == Resources.All)
            {
                SysEntities = JsonConvert.DeserializeObject<List<EntityScript>>(
                    File.ReadAllText(Resource[Resources.SysEntities]));
            }
        }

        public static VHE.WAD.Texture[] GetTextures(string textureName)
        {
            var list = new List<VHE.WAD.Texture>();

            if (textureName == null)
            {
                return list.ToArray();
            }

            foreach (var wad in Wads)
            {
                var tex = wad.GetTexture(textureName);
                if (tex != null)
                {
                    list.Add(tex);
                }
            }

            return list.ToArray();
        }

        private static void CheckTextures()
        {
            SolidsCurrent = 0;
            var missTex = new List<string>();
            var conflicTex = new List<string>();
            var conflictWads = new List<string>();

            foreach (var entity in Map.Data)
            {
                foreach (var par in entity.Parameters)
                {
                    if (par.ValueType != VHE.Entity.Type.SolidArray)
                    {
                        continue;
                    }

                    var solids = par.Value as List<VHE.Solid>;
                    foreach (var sld in solids)
                    {
                        foreach (var face in sld.Faces)
                        {
                            if (face.Texture == null)
                            {
                                continue;
                            }

                            var res = GetTextures(face.Texture);

                            if (res.Length == 0)
                            {
                                if (missTex.Contains(face.Texture))
                                {
                                    continue;
                                }

                                missTex.Add(face.Texture);
                            }
                            else if (res.Length > 1)
                            {
                                if (conflicTex.Contains(face.Texture))
                                {
                                    continue;
                                }

                                conflicTex.Add(face.Texture);
                                string wads = "";
                                foreach (var tex in res)
                                {
                                    wads += Path.GetFileNameWithoutExtension(tex.WAD.FilePath) + "$";
                                }

                                conflictWads.Add(wads);
                            }
                        }

                        SolidsCurrent++;
                    }
                }
            }

            if (missTex.Count > 0)
            {
                Message.Write("Warning: found missing textures:");
                foreach (var tex in missTex)
                {
                    Message.Write(tex);
                }
            }

            if (conflicTex.Count > 0)
            {
                Message.Write("Warning: found texture conflicts:");
                Message.Write("-Name-\t\t-Found in wads-");
                for (int i = 0; i < conflicTex.Count; i++)
                {
                    var wads = conflictWads[i].Split('$');
                    string row = conflicTex[i] + "\t\t";

                    for (int w = 1; w < wads.Length; w++)
                    {
                        row += wads[w];
                        if (w != wads.Length - 1)
                        {
                            row += ", ";
                        }
                    }

                    Message.Write(row);
                }
            }
        }
    }
}
