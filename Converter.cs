using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NamedBinaryTag;
using Newtonsoft.Json;
using System.IO;

namespace MCSMapConv
{
    public static class Converter
    {
        public static bool Debuging = false;
        public static float CSScale = 37;
        public static float TextureRes = 128;
        public static bool SkyBoxEnable = false;
        public static bool EntityNaming = false;  //(!) true can break down some entity functionality

        public static int Xmin, Ymin, Zmin, Xmax, Ymax, Zmax; //mc coordinates
        public static int Dimension;

        public static Config Config { get; private set; }
        public static bool Aborted { get; private set; }
        public static int BlockCount { get; private set; }
        public static int BlockProcessed { get; private set; }
        public static int BlockCurrent { get; private set; }
        public static int GroupCurrent { get; private set; }
        public static int SolidsCurrent { get; private set; }
        public static int EntitiesCurrent { get; private set; }
        public static ProcessType Process { get; private set; }
        public static Messaging Message { get; private set; } = new Messaging();

        private static List<BlockDescriptor> BlockDescriptors;
        private static List<VHE.WAD> Wads;
        private static List<EntityScript> SignEntities;
        private static List<EntityScript> SolidEntities;
        private static List<ModelScript> Models;
        private static FontDim FontDim = new FontDim();

        private static World MCWorld;
        public static VHE.Map Map { get; private set; }
        public static List<BlockGroup> BlockGroups { get; private set; } = new List<BlockGroup>();

        public static Dictionary<Resources, string> Resource = new Dictionary<Resources, string>() {
            {Resources.Models, @"data\models.json"},
            {Resources.Textures, @"data\blocks.json"},
            {Resources.SignEntities, @"data\sign_entities.json"},
            {Resources.SolidEntities, @"data\solid_entities.json"}
        };

        public enum Resources
        {
            All,
            Models,
            Wad,
            Textures,
            SignEntities,
            SolidEntities
        }

        public enum ProcessType
        {
            Idle,
            ScanBlocks,
            GenerateSolids,
            Done
        }

        public static void LoadConfig(string filePath)
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));

            CSScale = Config.BlockScale;
            TextureRes = Config.TextureResolution;
        }

        public static VHE.Map ConvertToMap(string worldPath, int dimension, 
            int mcxmin, int mcymin, int mczmin, int mcxmax, int mcymax, int mczmax)
        {
            BlockProcessed = 0;
            Aborted = false;
            Dimension = dimension;
            Xmin = mcxmin;
            Ymin = mcymin;
            Zmin = mczmin;
            Xmax = mcxmax;
            Ymax = mcymax;
            Zmax = mczmax;

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
            //BlockInspect(MCWorld, 0x1A,  430, 56, -345, 437, 56, -338); //TEST

            Process = ProcessType.ScanBlocks;
            Map = new VHE.Map();
            foreach (var wad in Config.WadFiles)
            {
                Map.AddString("worldspawn", "wad", wad);
            }
            
            BlockGroups = new List<BlockGroup>();

            /*TEST*/
            var bas = new Model()
            {
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(4, 4, 1),
                        OriginAlign = new VHE.Point(0, 0, -1)
                    }
                },
                Position = new VHE.Point(0, 0, 0)
            };

            var mdl = new Model()
            {
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(0.5f, 0.25f, 1),
                        OriginAlign = new VHE.Point(1, 1, 1),
                        Offset = new VHE.Point(-1, -1, 0),
                        //OriginRotOffset = new VHE.Point(1, 1, 0),
                        Rotation = new VHE.Point(0, 0, -22),
                        //AbsOffset = new VHE.Point(0, 1, 0)
                    },
                    new Model.Solid()
                    {
                        Size = new VHE.Point(0.5f, 0.25f, 1),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Offset = new VHE.Point(1, 1, 0),
                        //OriginRotOffset = new VHE.Point(0, 0, 0),
                        Rotation = new VHE.Point(0, 0, 22),
                        //AbsOffset = new VHE.Point(0, 1, 0)
                    }
                    
                },
                Position = new VHE.Point(0, 0, 0),
                //Origin = new VHE.Point(-1, -1, 0),
                //Rotation = new VHE.Point(0, 0, -45)
            };

            Map.AddSolids(Modelling.GenerateSolids(mdl, "pumpkin_face"));
            Map.AddSolids(Modelling.GenerateSolids(bas, "blockgold"));
            //Map.AddSolid(Modelling.GenerateSolid(mdl2, "brick2"));
            //Map.AddSolid(Modelling.CreateSolid(new VHE.Point(4, 0, 0), ms, new string[] { "!lava" }));
            Process = ProcessType.Done;
            return Map;
            /*TEST END*/
            
            var missings = new BlockMissMsg(Message);
            
            //coordinates of cs map
            for (int z = 0; z <= Ymax - Ymin; z++)
            {
                for (int y = 0; y <= Zmax - Zmin; y++)
                {
                    for (int x = 0; x <= Xmax - Xmin; x++)
                    {
                        var block = MCWorld.GetBlock(0, x + Xmin, z + Ymin, y + Zmin);
                        BlockCurrent++;

                        //check register
                    bt_chk:
                        var btm = GetBT(block, true);
                        if (block.ID != 0 && btm == null)
                        {
                            var res = missings.Message(block.ID, block.Data, "at " + (x + Xmin) + " " + 
                                (z + Ymin) + " " + (y + Zmin) + " is unregistered", true);

                            switch (res)
                            {
                                case BlockMissMsg.Result.Retry:
                                    LoadResources(Resources.Textures);
                                    goto bt_chk;

                                case BlockMissMsg.Result.Skip:
                                    block.ID = 0;
                                    block.Data = 0;
                                    break;

                                case BlockMissMsg.Result.Abort:
                                    Aborted = true;
                                    return null;
                            }
                        }

                        if (btm != null && btm.ModelClass == null)
                        {
                            block.ID = 0;
                        }

                        if (block.ID != 0)
                        {
                            BlockProcessed++;
                        }

                        var bt = BlockDescriptors.Find(a => a.ID == block.ID);

                        if (bt != null)
                        {
                            var type = bt.ModelClass.ToUpper();
                            switch (type)
                            {
                                case "PANE":
                                case "FENCE":
                                    GroupPaneFence(block, bt, x, y, z);
                                    block.ID = 0;
                                    break;

                                case "DOOR":
                                    if (block.Data < 8)
                                    {
                                        int data = MCWorld.GetBlock(0, x + Xmin, z + Ymin + 1, y + Zmin).Data;
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

                                case "GRASS":
                                case "SIGN":
                                    GroupSingle(block, x, y, z, type);
                                    break;
                            }
                        }

                        //Normal block
                        GroupNormal(block, x, y, z, btm);

                        if (Debuging)
                        {
                            Debug(x, y, z);
                        }
                    }
                    BlockGroups.ForEach(x => x.XClosed = true);

                }
                BlockGroups.ForEach(x => x.YClosed = true);

            }
            BlockGroups.ForEach(x => x.ZClosed = true);

            if (Debuging)
            {
                Debug(Xmax - Xmin, Zmax - Zmin, Ymax - Ymin);
            }

            //Generate cs solids
            Process = ProcessType.GenerateSolids;
            Aborted = false;
            foreach (var bg in BlockGroups)
            {
                GroupCurrent++;
                //System.Threading.Thread.Sleep(10);

            pBegin:
                var bt = GetBT(bg.BlockID, bg.BlockData);
                if (bt == null)
                {
                    var res = missings.Message(bg.BlockID, bg.BlockData, "texture is not registered", false);
                    switch (res)
                    {
                        case BlockMissMsg.Result.Retry:
                            LoadResources(Resources.Textures);
                            goto pBegin;

                        case BlockMissMsg.Result.Abort:
                            Aborted = true;
                            return null;
                    }
                }

                pWad:
                foreach (var tex in bt.Textures)
                {
                    bool found = false;
                    foreach (var wad in Wads)
                    {
                        if (wad.Textures.Find(t => t.Name.ToUpper() == tex.Texture.ToUpper()) == null)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        var res = missings.Message(bg.BlockID, bg.BlockData, 
                            "wad texture " + tex.Texture + " not found", false);
                        switch (res)
                        {
                            case BlockMissMsg.Result.Retry:
                                LoadResources(Resources.Wad);
                                goto pWad;

                            case BlockMissMsg.Result.Abort:
                                Aborted = true;
                                return null;
                        }
                    }
                }

                switch (bg.Type)
                {
                    case BlockGroup.ModelType.Normal:
                    case BlockGroup.ModelType.Liquid:
                        ModelNormal(bg, bt);
                        break;

                    case BlockGroup.ModelType.Pane:
                        ModelPane(bg, bt);
                        break;

                    case BlockGroup.ModelType.Fence:
                        ModelFence(bg, bt);
                        break;

                    case BlockGroup.ModelType.Door:
                        ModelDoor(bg, bt);
                        break;

                    case BlockGroup.ModelType.Grass:
                        ModelGrass(bg, bt);
                        break;

                    case BlockGroup.ModelType.Special:
                        ModelSpecial(bg, bt);
                        break;

                    case BlockGroup.ModelType.Sign:
                        var text = GetSignText(Dimension, bg);
                        if (!GenerateSignEntity(bg, text))
                        {
                            ModelSign(bg, bt, text);
                        }
                        break;

                    default:
                        throw new Exception("Undefined model type.");
                }
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
                            AbsOffset = new VHE.Point(0, -1, 0),
                            Size = new VHE.Point(sbx, 1, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(sbx, -1, 0),
                            Size = new VHE.Point(1, sby + 2, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(0, sby, 0),
                            Size = new VHE.Point(sbx, 1, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(-1, -1, 0),
                            Size = new VHE.Point(1, sby + 2, sbz),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(-1, -1, sbz),
                            Size = new VHE.Point(sbx + 2, sby + 2, 1),
                        },
                        new Model.Solid()
                        {
                            AbsOffset = new VHE.Point(-1, -1, -1),
                            Size = new VHE.Point(sbx + 2, sby + 2, 1),
                        }
                    }
                };

                Map.AddSolids(Modelling.GenerateSolids(model, "sky"));
            }

            Process = ProcessType.Done;
            return Map;
        }

        /*Grouping*/

        private static void GroupSingle(Block block, int x, int y, int z, string type, int data = -1)
        {
            GroupSingle(block, x, y, z, BlockGroup.SType[type], data);
        }

        private static void GroupSingle(Block block, int x, int y, int z, BlockGroup.ModelType type, int data = -1)
        {
            if (data == -1)
            {
                data = block.Data;
            }

            BlockGroups.Add(new BlockGroup(block.ID, data, x, y, z)
            {
                Block = block,
                Type = type
            });

            block.ID = 0;
        }

        private static void GroupPaneFence(Block block, BlockDescriptor bt, int x, int y, int z)
        {
            bool px = false, py = false;
            var model = BlockGroup.SType[bt.ModelClass.ToUpper()];

            //X
            var paneX = BlockGroups.Find(p => p.Type == model && !p.XClosed &&
                p.Xmax == x && p.Ymin == y && p.Orientation != BlockGroup.Orient.Y && p.BlockID == block.ID);

            if (paneX == null) //create new pane
            {
                paneX = new BlockGroup(block.ID, block.Data, x, y, z);
                paneX.Type = model;

                //X
                var bmx = MCWorld.GetBlock(0, x + Xmin - 1, z + Ymin, y + Zmin);
                var btmx = BlockDescriptors.Find(e => e.ID == bmx.ID);
                var nbmx = btmx != null && btmx.ModelClass == "Normal";
                var bpx = MCWorld.GetBlock(0, x + Xmin + 1, z + Ymin, y + Zmin);
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

                var bp = MCWorld.GetBlock(0, x + Xmin + 1, z + Ymin, y + Zmin);
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
                p.Ymax == y && p.Xmin == x && p.Orientation != BlockGroup.Orient.X && p.BlockID == block.ID);

            if (paneY == null) //create new pane
            {
                paneY = new BlockGroup(block.ID, block.Data, x, y, z);
                paneY.Type = model;

                //Y
                var bmy = MCWorld.GetBlock(0, x + Xmin, z + Ymin, y + Zmin - 1);
                var btmy = BlockDescriptors.Find(e => e.ID == bmy.ID);
                var nbmy = btmy != null && btmy.ModelClass == "Normal";
                var bpy = MCWorld.GetBlock(0, x + Xmin, z + Ymin, y + Zmin + 1);
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

                var bp = MCWorld.GetBlock(0, x + Xmin, z + Ymin, y + Zmin + 1);
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
                var pillar = BlockGroups.Find(p => p.Type == model && !p.ZClosed && p.BlockID == block.ID &&
                    p.Orientation == BlockGroup.Orient.Z && p.Xmin == x && p.Ymin == y && p.Zmax == z);

                if (pillar == null)
                {
                    pillar = new BlockGroup(block.ID, block.Data, x, y, z);
                    pillar.Type = model;
                    pillar.Orientation = BlockGroup.Orient.Z;

                    var bpz = MCWorld.GetBlock(0, x + Xmin, z + Ymin + 1, y + Zmin);
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
                    var bpz = MCWorld.GetBlock(0, x + Xmin, z + Ymin + 1, y + Zmin);
                    var btmy = BlockDescriptors.Find(e => e.ID == bpz.ID);
                    if (btmy == null || btmy.ID != block.ID)
                    {
                        pillar.ZClosed = true;
                    }
                }
            }
        }

        private static void GroupNormal(Block block, int x, int y, int z, BlockDescriptor bt)
        {
            bool found = false;
            BlockGroup[] cuts = new BlockGroup[2];
            foreach (var solid in BlockGroups)
            {
                if (solid.Type != BlockGroup.ModelType.Normal && solid.Type != BlockGroup.ModelType.Liquid)
                {
                    continue;
                }

                var rngX = x >= solid.Xmin && x < solid.Xmax;
                var rngY = y >= solid.Ymin && y < solid.Ymax;
                var rngZ = z < solid.Zmax;
                var expX = !solid.XClosed && y == solid.Ymax - 1 && x == solid.Xmax;
                var expY = !solid.YClosed && y == solid.Ymax && rngX;
                var expZ = !solid.ZClosed && z == solid.Zmax && rngX && rngY;
                if (expX || expY || expZ || (rngX && rngY && rngZ))
                {
                    //if (solid.BlockID == block.ID && solid.BlockData == block.Data && !found)
                    if (CompareID(block, solid.BlockID, solid.BlockData) && !found)
                    {
                        solid.Expand(x, y, z);
                        found = true;
                    }
                    else
                    {
                        cuts = solid.Cut(x, y, z);
                    }
                }
            }

            bool added = false;
            foreach (var cut in cuts)
            {
                if (cut != null)
                {
                    BlockGroups.Add(cut);
                    added = true;
                }
            }

            if (!found && block.ID != 0)
            {
                BlockGroups.Add(new BlockGroup(block.ID, block.Data, x, y, z) { 
                    Type = bt.GetSolidType()
                });
                var last = BlockGroups.Last();
                BlockGroups.Remove(last);
                BlockGroups.Insert(0, last);
                added = true;
            }

            if (added)
            {
                int idmax = -1;
                foreach (var sld in BlockGroups)
                {
                    if (sld.TestID > idmax || (sld.TestID != -1 && idmax == -1))
                    {
                        idmax = sld.TestID;
                    }
                }
                foreach (var sld in BlockGroups)
                {
                    if (sld.TestID == -1)
                    {
                        sld.TestID = ++idmax;
                    }
                }
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
                    pz.BlockID == pane.BlockID && pz.BlockData == pane.BlockData);

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

        private static void ModelNormal(BlockGroup bg, BlockDescriptor bt)
        {
            var model = new Model()
            {
                Solids = 
                { 
                    new Model.Solid() 
                    {
                        Size = new VHE.Point(
                        bg.Xmax - bg.Xmin,
                        bg.Ymax - bg.Ymin,
                        bg.Zmax - bg.Zmin)
                    } 
                }
            };

            if (bg.Type == BlockGroup.ModelType.Liquid)
            {
                model.Solids[0].Size.Z -= 0.125f;
            }

            MapAddObject(Modelling.GenerateSolid(bt, bg, model), bt);
        }

        private static void ModelPane(BlockGroup bg, BlockDescriptor bt)
        {
            const float th = 0.125f;

            float edgeOffset = 0.5f - th / 2;
            float length = th, rot = 0;
            float beg = 0, end = 0;
            VHE.Point align = new VHE.Point(0, 0, 1);
            VHE.Point offset = new VHE.Point(0.5f, 0.5f, 0);
            var face = bt.GetTextureName(bg.BlockData, null, "face", null);
            var edge = bt.GetTextureName(bg.BlockData, null, "edge", null);
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

            var solid = Modelling.GenerateSolid(bti, bg, model);
            MapAddObject(solid, bti);
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
                    MapAddObject(Modelling.GenerateSolids(bt, bg, mdl), bt);

                    mdl.Position.Z += 0.375f;
                    MapAddObject(Modelling.GenerateSolids(bt, bg, mdl), bt);
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
                MapAddObject(Modelling.GenerateSolids(bt, bg, mdl), bt);
            }
        }

        private static void ModelDoor(BlockGroup bg, BlockDescriptor bt)
        {
            const float th = 0.1875f;

            float rotate = 0, orx = 0, ory = 0;
            bool mirror = false;

            switch (bg.BlockData)
            {
                case 0:
                case 13:
                    rotate = 270;
                    mirror = true;
                    break;

                case 1:
                case 14:
                    mirror = true;
                    break;

                case 2:
                case 15:
                    rotate = 90;
                    mirror = true;
                    break;

                case 3:
                case 12:
                    rotate = 180;
                    mirror = true;
                    break;

                case 4:
                case 9:
                    break;

                case 5:
                case 10:
                    rotate = 90;
                    break;

                case 6:
                case 11:
                    rotate = 180;
                    break;

                case 7:
                case 8:
                    rotate = 270;
                    break;
            }

            if (mirror)
            {
                Modelling.Rotate2D(ref orx, ref ory, rotate);
            }

            var model = new Model()
            {
                Solids =
                {
                    new Model.Solid() //door
                    {
                        Size = new VHE.Point(1, th, 2),
                        OriginAlign = new VHE.Point(1, 1, 1),
                        OriginRotOffset = new VHE.Point(0.5f, 0.5f, 0),
                        Rotation = new VHE.Point(0, 0, rotate),
                        TextureOriented = true,
                        Faces = new List<Model.Face>
                        {
                            new Model.Face(Model.Faces.Front)
                            {
                                MirrorV = mirror
                            },
                            new Model.Face(Model.Faces.Rear)
                            {
                                MirrorV = mirror
                            },
                            new Model.Face(Model.Faces.Top)
                            {
                                Rotation = 90
                            },
                            new Model.Face(Model.Faces.Bottom)
                            {
                                Rotation = 90
                            }
                        }
                    },
                    new Model.Solid() //origin
                    {
                        Name = "origin",
                        Size = new VHE.Point(0.125f, 0.125f, 0.125f),
                        AbsOffset = new VHE.Point(0 + orx, th / 2 + ory, 1),
                        OriginAlign = new VHE.Point(0, 0, 0),
                        OriginRotOffset = new VHE.Point(0.5f, 0.5f - th / 2, 0),
                        Rotation = new VHE.Point(0, 0, rotate),
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

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt);
        }

        private static void ModelGrass(BlockGroup bg, BlockDescriptor bt)
        {
            float len = (float)Math.Sqrt(2);

            var worldOffset = World.GetBlockXZOffset(bg.Xmin + Xmin, bg.Ymin + Zmin);
            var texture = Modelling.GetTexture(Wads, bt.GetTextureName(bg.BlockData));
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

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt);
        }

        private static void ModelSpecial(BlockGroup bg, BlockDescriptor bt)
        {
            var modelScr = Models.Find(x => x.Name == bt.ModelName);
            if (modelScr == null)
            {
                throw new Exception("Model not found");
            }

            var model = modelScr.ToModel();


            if (bt.WorldOffset)
            {
                var worldOffset = World.GetBlockXZOffset(bg.Xmin + Xmin, bg.Ymin + Zmin);
                
                foreach (var sld in model.Solids)
                {
                    sld.AbsOffset.X += worldOffset[0];
                    sld.AbsOffset.Y += worldOffset[1];
                }
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt);
        }

        private static void ModelSign(BlockGroup bg, BlockDescriptor bt, string[] text)
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
                rotate = BlockDataParse.Rotation16(bg.BlockData);
            }
            else
            {
                morigAligin = new VHE.Point(0, 1, 0);
                moffset = new VHE.Point(0.5f, 0, 0.5f);

                switch (bg.BlockData)
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
                        throw new Exception("Invalid block data value (sign)");
                }
            }

            var main = new Model.Solid()
            {
                Name = "main",
                Size = new VHE.Point(msx, msy, msz),
                OriginAlign = morigAligin,
                Offset = moffset,
                OriginRotOffset = new VHE.Point(0.5f, 0.5f, 0),
                Rotation = new VHE.Point(0, 0, rotate),
                TextureScale = tScale
            };

            var stick = new Model.Solid()
            {
                Name = "stick",
                Size = new VHE.Point(msy, msy, ssz),
                OriginAlign = new VHE.Point(0, 0, 1),
                Rotation = new VHE.Point(0, 0, rotate),
                AbsOffset = new VHE.Point(0.5f, 0.5f, 0),
                TextureScale = tScale
            };

            var model = new Model() { Solids = new List<Model.Solid>() { main } };

            if (ground)
            {
                model.Solids.Add(stick);
            }

            MapAddObject(Modelling.GenerateSolids(bt, bg, model), bt);

            /*generate text*/
            if (text.Length > 0)
            {
                string fontTextureName = "{font";
                var texture = Modelling.GetTexture(Wads, fontTextureName);
                const int sch = 8; //char size in font pixels
                float mScale = 1.0f / 16 * tScale; //sign pixel scale
                float pScale = 0.25f * mScale; //font pixel scale
                float min = Modelling.MinSize;

                var textModel = new Model();

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
                            oty = (float)(-0.325 * texture.Height / 128);
                        }

                        //char full offset x
                        float txoff = 0.5f - txl / 2 + xShift + sx / 2;

                        var chr = new Model.Solid()
                        {
                            Name = "char",
                            Size = new VHE.Point(sx, min * 2, sy),
                            OriginAlign = new VHE.Point(0, 0, -1),
                            Offset = new VHE.Point(txoff, yoff, zoff - tzoff - oy),
                            OriginRotOffset = new VHE.Point(0.5f, 0.5f, 0),
                            Rotation = new VHE.Point(0, 0, rotate),
                            TextureScale = pScale / fontScale * 128 * (TextureRes / 16),
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

                        //update char row offset x
                        xShift += sx + 1 * pScale;

                        textModel.Solids.Add(chr);
                    }
                }

                var btt = new BlockDescriptor()
                {
                    Entity = "illusionary"
                };

                MapAddObject(Modelling.GenerateSolids(null, bg, textModel), btt);
            }
        }

        /*Map methods*/

        private static void MapAddObject(VHE.Map.Solid solid, BlockDescriptor bt, 
            int blockData = 0, float x = 0, float y = 0, float z = 0)
        {
            MapAddObject(new List<VHE.Map.Solid>() { solid }, bt, blockData, x, y, z);
        }

        private static void MapAddObject(List<VHE.Map.Solid> solids, BlockDescriptor bt, 
            int blockData = 0, float x = 0, float y = 0, float z = 0)
        {
            var se = GetSolidEntity(bt);
            var t = Map.GetSolidsCount().Solids;

            if (se != null)
            {
                var entity = GenerateEntity(se, blockData, x, y, z, "E" + Map.Data.Count);
                solids.ForEach(s => entity.AddSolid(s));
                Map.CreateEntity(entity);
                EntitiesCurrent += solids.Count;
            }
            else
            {
                solids.ForEach(s => Map.AddSolid(s));
                SolidsCurrent += solids.Count;
            }

            var test = Map.GetSolidsCount().Solids;
        }

        private static EntityScript GetSolidEntity(BlockDescriptor bt)
        {
            if (bt.Entity != null)
            {
                return SolidEntities.Find(x => x.Macros.ToUpper() == bt.Entity.ToUpper());
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

                if (bt.Data == -1 && !bt.GroupByData) //Ignore the data value
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

            var block = bg.Block;
            var x = bg.Xmin;
            var y = bg.Ymin;
            var z = bg.Zmin;

            try
            {
                var se = Macros.GetSignEntity(SignEntities, text);

                if (se != null)
                {
                    Map.Data.Add(GenerateEntity(se, block, x, y, z));
                    return true;
                }
            }
            catch (Exception e)
            {
                if (e.InnerException == Macros.Exceptions.ESNotFound)
                {
                    Message.Write("Undefined macros {0} at {1} {2} {3}", e.Message,
                        block.X, block.Y, block.Z);
                }
            }
            
            return false;
        }

        private static VHE.Entity GenerateEntity(EntityScript entityTemplate, int blockData, float x, float y, float z, string name = null)
        {
            var block = new Block() { 
                ID = 0,
                Data = (byte)blockData
            };

            return GenerateEntity(entityTemplate, block, x, y, z, name);
        }

        private static VHE.Entity GenerateEntity(EntityScript entityTemplate, Block block, float x, float y, float z, string name = null)
        {
            var entity = new VHE.Entity(entityTemplate.ClassName);

            foreach (var part in entityTemplate.Parameters)
            {
                var par = new VHE.Entity.Parameter(part.Name);
                par.SetType(part.ValueType);

                string value = part.Value;

                try
                {
                    value = Macros.EntityValue(value, block.Data, x, y, z);
                    par.SetValue(value);
                    entity.Parameters.Add(par);
                }
                catch (Exception e)
                {
                    if (e == Macros.Exceptions.SubMacrosUndef)
                    {
                        Message.Write(e.InnerException.Message + e.Message + " at " + x + " " + y + " " + z);
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

        /*Debug*/

        private static void Debug(int x, int y, int z)
        {
            Settings.DebugEnable = false;
            Console.CursorVisible = false;

            //Build empty field
            for (int cz = 0; cz <= Ymax - Ymin; cz++)
            {
                for (int cy = 0; cy <= Zmax - Zmin; cy++)
                {
                    for (int cx = 0; cx <= Xmax - Xmin; cx++)
                    {
                        if (cx == x && cy == y && cz == z)
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }

                        Console.SetCursorPosition(cx + (Xmax - Xmin + 4) * cz + 1, cy + 1);
                        Console.Write(".");

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition((Xmax - Xmin + 4) * cz + 1, Zmax - Zmin + 2);
                Console.Write("z=" + cz);

                Console.SetCursorPosition((Xmax - Xmin + 4) * cz + 1, 0);
                for (int i = 0; i <= Xmax - Xmin; i++)
                {
                    Console.Write(ToHex(i));
                }
                for (int i = 0; i <= Zmax - Zmin; i++)
                {
                    Console.SetCursorPosition((Xmax - Xmin + 4) * cz, i + 1);
                    Console.Write(ToHex(i));
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            //Render solids
            for (int i = 0; i < BlockGroups.Count; i++)
            {
                var sld = BlockGroups[i];
                if (sld.Type != BlockGroup.ModelType.Normal)
                {
                    continue;
                }

                for (int cz = sld.Zmin; cz < sld.Zmax; cz++)
                {
                    for (int cy = sld.Ymin; cy < sld.Ymax; cy++)
                    {
                        for (int cx = sld.Xmin; cx < sld.Xmax; cx++)
                        {
                            if (cx == x && cy == y && cz == z)
                            {
                                Console.BackgroundColor = ConsoleColor.Blue;
                            }
                            else
                            {
                                if (sld.ZClosed)
                                {
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                }
                                else if (sld.YClosed)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                                else if (sld.XClosed)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                }
                            }

                            Console.SetCursorPosition(cx + (Xmax - Xmin + 4) * cz + 1, cy + 1);
                            Console.Write(ToHex(sld.TestID));
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.White;

                        }
                    }
                }
            }


            //Render panes
            char[,,] matrix = new char[Xmax - Xmin + 1, Zmax - Zmin + 1, Ymax - Ymin + 1];

            for (int i = 0; i < BlockGroups.Count; i++)
            {
                var pane = BlockGroups[i];
                if (pane.Type != BlockGroup.ModelType.Pane)
                {
                    continue;
                }

                for (int cz = pane.Zmin; cz < pane.Zmax; cz++)
                {
                    for (int cy = pane.Ymin; cy < pane.Ymax; cy++)
                    {
                        for (int cx = pane.Xmin; cx < pane.Xmax; cx++)
                        {
                            if (pane.Orientation == BlockGroup.Orient.X)
                            {
                                bool left = false, right = false;
                                if (cx == pane.Xmin)
                                {
                                    if (pane.XBegTouch)
                                    {
                                        left = true;
                                    }

                                    if (cx != pane.Xmax - 1)
                                    {
                                        right = true;
                                    }
                                    else
                                    {
                                        if (pane.XEndTouch)
                                        {
                                            right = true;
                                        }
                                    }
                                }

                                if (cx == pane.Xmax - 1)
                                {
                                    if (pane.XEndTouch)
                                    {
                                        right = true;
                                    }

                                    if (cx != pane.Xmin)
                                    {
                                        left = true;
                                    }
                                    else
                                    {
                                        if (pane.XBegTouch)
                                        {
                                            left = true;
                                        }
                                    }
                                }

                                if (cx > pane.Xmin && cx < pane.Xmax - 1)
                                {
                                    left = true;
                                    right = true;
                                }

                                matrix[cx, cy, cz] = CharPaneComb(matrix[cx, cy, cz], left, right, false, false);

                            }
                            else if (pane.Orientation == BlockGroup.Orient.Y)
                            {
                                bool up = false, down = false;
                                if (cy == pane.Ymin)
                                {
                                    if (pane.YBegTouch)
                                    {
                                        up = true;
                                    }

                                    if (cy != pane.Ymax - 1)
                                    {
                                        down = true;
                                    }
                                    else
                                    {
                                        if (pane.YEndTouch)
                                        {
                                            down = true;
                                        }
                                    }
                                }

                                if (cy == pane.Ymax - 1)
                                {
                                    if (pane.YEndTouch)
                                    {
                                        down = true;
                                    }

                                    if (cy != pane.Ymin)
                                    {
                                        up = true;
                                    }
                                    else if (pane.YBegTouch)
                                    {
                                        up = true;
                                    }
                                }

                                if (cy > pane.Ymin && cy < pane.Ymax - 1)
                                {
                                    up = true;
                                    down = true;
                                }

                                matrix[cx, cy, cz] = CharPaneComb(matrix[cx, cy, cz], false, false, up, down);
                            }
                            else
                            {
                                matrix[cx, cy, cz] = CharPaneComb(matrix[cx, cy, cz], false, false, false, false);
                            }
                        }
                    }
                }
            }

            for (int cz = 0; cz <= Ymax - Ymin; cz++)
            {
                for (int cy = 0; cy <= Zmax - Zmin; cy++)
                {
                    for (int cx = 0; cx <= Xmax - Xmin; cx++)
                    {
                        if (cx == x && cy == y && cz == z)
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                        }

                        if (matrix[cx, cy, cz] != '\0')
                        {
                            Console.SetCursorPosition(cx + (Xmax - Xmin + 4) * cz + 1, cy + 1);
                            Console.Write(matrix[cx, cy, cz]);
                        }

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }


            Console.SetCursorPosition(0, Zmax - Zmin + 4);
            ClearCurrentConsoleLine();
            Console.WriteLine(" X={0}  Y={1}  Z={2}", x, y, z);
            Console.CursorVisible = true;
            Console.ReadKey();
        }

        private static char CharPaneComb(char ch, bool l, bool r, bool u, bool d)
        {
            bool bl = false, br = false, bu = false, bd = false;

            switch (ch)
            {
                case '│': // DU
                    bu = true;
                    bd = true;
                    break;

                case '─': // RL
                    br = true;
                    bl = true;
                    break;

                case '┤': // DUL
                    bu = true;
                    bd = true;
                    bl = true;
                    break;

                case '┴': // URL
                    bu = true;
                    br = true;
                    bl = true;
                    break;

                case '┬': // DRL
                    bd = true;
                    br = true;
                    bl = true;
                    break;

                case '├': // DUR
                    bu = true;
                    br = true;
                    bd = true;
                    break;

                case '┐': // DL
                    bl = true;
                    bd = true;
                    break;

                case '└': // UR
                    bu = true;
                    bd = true;
                    break;

                case '┘': // UL
                    bu = true;
                    bl = true;
                    break;

                case '┌': // DR
                    bd = true;
                    br = true;
                    break;

                case '┼': // DURL
                    bu = true;
                    bd = true;
                    br = true;
                    bl = true;
                    break;

                case 'v':
                    bd = true;
                    break;

                case '^':
                    bu = true;
                    break;

                case '>':
                    br = true;
                    break;

                case '<':
                    bl = true;
                    break;
            }

            bl = bl | l;
            br = br | r;
            bu = bu | u;
            bd = bd | d;

            int res = Convert.ToInt32(bl) | Convert.ToInt32(br) << 1 |
                Convert.ToInt32(bu) << 2 | Convert.ToInt32(bd) << 3;

            switch (res)
            {
                case 0b0001: return '<';
                case 0b0010: return '>';
                case 0b0100: return '^';
                case 0b1000: return 'v';

                case 0b0011: return '─';
                case 0b1100: return '│';

                case 0b0101: return '┘';
                case 0b1001: return '┐';
                case 0b0110: return '└';
                case 0b1010: return '┌';

                case 0b1110: return '├';
                case 0b1101: return '┤';
                case 0b1011: return '┬';
                case 0b0111: return '┴';

                case 0b1111: return '┼';

                default: return '*';
            }
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

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

        private static void BlockInspect(World world, int id, int xmin, int ymin, int zmin, int xmax, int ymax, int zmax)
        {
            Debuging = true;
            Settings.DebugEnable = false;
            Console.WriteLine("Debug mode. Block data inspect");

            var list = new List<Block>();

            if (id == -1)
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    Console.WriteLine();
                    Console.WriteLine("Y = " + y);
                    for (int z = zmin; z <= zmax; z++)
                    {
                        for (int x = xmin; x <= xmax; x++)
                        {
                            var block = world.GetBlock(0, x, y, z);
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
                            var block = world.GetBlock(0, x, y, z);

                            if (block.ID != id)
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

                            var ch = MCWorld.GetChunkAtBlock(0, x, z);
                            List<NBT> tes = ch.NBTData.GetTag("Level/TileEntities");
                            foreach (var te in tes)
                            {
                                int bx = te.GetTag("x");
                                int by = te.GetTag("y");
                                int bz = te.GetTag("z");
                                string bid = te.GetTag("id");

                                if (bx == x && by == y && bz == z && bid == "minecraft:bed")
                                {
                                    int c = te.GetTag("color");
                                    Console.Write(ToHex(c) + " ");
                                }
                            }

                            //Console.Write(ToHex(block.Data) + " ");
                            list.Add(block);
                        }
                        Console.WriteLine();
                    }
                }
            }
            

            Console.WriteLine("Done");
            while(true)
            {
                Console.ReadKey();
            }
        }

        /**/

        private static void LoadResources(Resources res)
        {
            if (res == Resources.Wad || res == Resources.All)
            {
                Wads = new List<VHE.WAD>();
                foreach (var wad in Config.WadFiles)
                {
                    Wads.Add(new VHE.WAD(wad));
                }
            }
            if (res == Resources.Textures || res == Resources.All)
            {
                BlockDescriptors = JsonConvert.DeserializeObject<List<BlockDescriptor>>(
                    File.ReadAllText(Resource[Resources.Textures]));
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
            if (res == Resources.SignEntities || res == Resources.All)
            {
                SolidEntities = JsonConvert.DeserializeObject<List<EntityScript>>(
                    File.ReadAllText(Resource[Resources.SolidEntities]));
            }
        }

        private static bool CheckWads()
        {
            bool fault = false;
            Console.Write("Checking WADs...");

            var texList = new List<string>();

            //check texture availability in wads
            foreach (var bd in BlockDescriptors)
            {
                var texs = bd.GetTexureNamesList();
                
                foreach (var tex in texs)
                {
                    if (!texList.Contains(tex))
                    {
                        texList.Add(tex);
                    }
                }

                foreach (var tex in texs)
                {
                    if (Modelling.GetTexture(Wads, tex) == null)
                    {
                        if (!fault)
                        {
                            fault = true;
                            Console.WriteLine();
                        }

                        Console.WriteLine("Error: texture \"{0}\" not found. ID:{1}, Data:{2}",
                            tex, bd.ID, bd.Data);
                        Console.ReadKey();
                    }
                }
            }

            //validate textures
            foreach (var texName in texList)
            {
                var tex = Modelling.GetTexture(Wads, texName);
                if (tex == null)
                {
                    continue;
                }

                //transparent
                if (tex.Transparent)
                {
                    var rgb = tex.Data.Palette[255];

                    if (rgb.R != 0 || rgb.G != 0 || rgb.B != 255)
                    {
                        Console.WriteLine("Error: invalid tansparent color in texture \"{0}\"", texName);
                    }
                }
            }

            if (!fault)
            {
                Console.WriteLine("OK");
            }

            return !fault;
        }
    }
}
