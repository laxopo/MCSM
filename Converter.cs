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
        public static bool SkyBoxEnable = true;

        public static int Xmin, Ymin, Zmin, Xmax, Ymax, Zmax; //mc coordinates

        public static bool Aborted { get; private set; }
        public static int BlockCount { get; private set; }

        private static List<BlockTexture> Blocks;
        private static List<VHE.WAD> Wads;
        private static List<EntityTemplate> SignEntities;
        private static List<EntityTemplate> SolidEntities;
        private static List<Model> Models;

        private static World MCWorld;
        private static VHE.Map Map;
        private static List<BlockGroup> BlockGroups;

        public static Dictionary<Resources, string> Resource = new Dictionary<Resources, string>() {
            {Resources.Models, "models.json"},
            {Resources.Textures, "blocks.json"},
            {Resources.SignEntities, "objects.json"},
            {Resources.SolidEntities, "solid_entities.json"}
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

        public static VHE.Map ConvertToMap(World world, int mcxmin, int mcymin, int mczmin, int mcxmax, int mcymax, int mczmax)
        {
            Settings.DebugEnable = !Debuging;
            BlockCount = 0;
            Aborted = false;
            MCWorld = world;
            Xmin = mcxmin;
            Ymin = mcymin;
            Zmin = mczmin;
            Xmax = mcxmax;
            Ymax = mcymax;
            Zmax = mczmax;

            LoadResources(Resources.All);
            Modelling.Initialize(CSScale, TextureRes, Map, Wads, Models, SolidEntities);

            Map = new VHE.Map();
            Map.AddString("worldspawn", "wad", @"\cstrike\minecraft.wad");
            BlockGroups = new List<BlockGroup>();

            /*TEST*/
            /*var bas = new Model()
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

            var btx = Blocks.Find(t => t.ID == 1);

            var mdl = new Model()
            {
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(1.41f, 0.01f, 2),
                        Offset = new VHE.Point(0.4f, 0.5f, 0),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Rotation = new VHE.Point(0, 0, -45),
                        Faces = new Model.Face[]
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
                },
                Position = new VHE.Point(1, 1, 0)
            };

            var mdl2 = new Model()
            {
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(1.41f, 0.01f, 2),
                        Offset = new VHE.Point(0.3f, 0.45f, 0.85f),
                        OriginAlign = new VHE.Point(0, 0, 1),
                        Rotation = new VHE.Point(0, 0, -45),
                        Faces = new Model.Face[]
                        {
                            new Model.Face(Model.Faces.Front)
                            {
                                StretchU = true,
                                //Frame = true,
                            }
                        }
                    },
                },
                Position = new VHE.Point(1, 1, 0)
            };

            Map.AddSolid(Modelling.GenerateSolid(mdl, "brick2"));
            //Map.AddSolid(Modelling.GenerateSolid(mdl2, "brick2"));

            Map.AddSolid(Modelling.GenerateSolid(bas, "blockgold"));
            //Map.AddSolid(Modelling.CreateSolid(new VHE.Point(4, 0, 0), ms, new string[] { "!lava" }));
            return Map;
            /*TEST END*/

            var missings = new BlockMissMsg();
            
            //coordinates of cs map
            for (int z = 0; z <= Ymax - Ymin; z++)
            {
                for (int y = 0; y <= Zmax - Zmin; y++)
                {
                    for (int x = 0; x <= Xmax - Xmin; x++)
                    {
                        var block = world.GetBlock(0, x + Xmin, z + Ymin, y + Zmin);

                        //object
                        if (block.ID == 63)
                        {
                            GenerateSignEntity(block, x, y, z);
                            block.ID = 0;
                        }

                        //check register
                    bt_chk:
                        var btm = GetBT(block.ID, block.Data);
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

                        if (btm != null && btm.Model == null)
                        {
                            block.ID = 0;
                        }

                        if (block.ID != 0)
                        {
                            BlockCount++;
                        }

                        var bt = Blocks.Find(a => a.ID == block.ID);

                        if (bt != null)
                        {
                            switch (bt.Model.ToUpper())
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
                                        BlockGroups.Add(new BlockGroup(block.ID, data, x, y, z) { 
                                            Type = BlockGroup.SolidType.Door
                                        });
                                    }
                                    block.ID = 0;
                                    break;

                                case "GRASS":
                                    var grassSld = new BlockGroup(block.ID, block.Data, x, y, z) { 
                                        Type = BlockGroup.SolidType.Grass,
                                    };
                                    var txt = Modelling.GetTexture(Wads, bt.GetTextureName(block.Data));
                                    if (txt != null && txt.Height != -1)
                                    {
                                        int h = txt.Height / (int)TextureRes;
                                        if (h != 0)
                                        {
                                            grassSld.Zmax = grassSld.Zmin + h;
                                        }
                                    }
                                    BlockGroups.Add(grassSld);
                                    block.ID = 0;
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
            Aborted = false;
            foreach (var mcsolid in BlockGroups)
            {
            pBegin:
                var bt = GetBT(mcsolid.BlockID, mcsolid.BlockData);
                if (bt == null)
                {
                    var res = missings.Message(mcsolid.BlockID, mcsolid.BlockData, "texture is not registered", false);
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
                        var res = missings.Message(mcsolid.BlockID, mcsolid.BlockData, 
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

                switch (mcsolid.Type)
                {
                    case BlockGroup.SolidType.Normal:
                    case BlockGroup.SolidType.Liquid:
                        GenerateModelNormal(mcsolid, bt);
                        break;

                    case BlockGroup.SolidType.Pane:
                        GenerateModelPane(mcsolid, bt);
                        break;

                    case BlockGroup.SolidType.Fence:
                        GenerateModelFence(mcsolid, bt);
                        break;

                    case BlockGroup.SolidType.Door:
                        GenerateModelDoor(mcsolid, bt);
                        break;

                    case BlockGroup.SolidType.Grass:
                        GenerateModelGrass(mcsolid, bt);
                        break;

                    case BlockGroup.SolidType.Special:
                        GenerateModelSpecial(mcsolid, bt);
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
                            Offset = new VHE.Point(0, -1, 0),
                            Size = new VHE.Point(sbx, 1, sbz),
                        },
                        new Model.Solid()
                        {
                            Offset = new VHE.Point(sbx, -1, 0),
                            Size = new VHE.Point(1, sby + 2, sbz),
                        },
                        new Model.Solid()
                        {
                            Offset = new VHE.Point(0, sby, 0),
                            Size = new VHE.Point(sbx, 1, sbz),
                        },
                        new Model.Solid()
                        {
                            Offset = new VHE.Point(-1, -1, 0),
                            Size = new VHE.Point(1, sby + 2, sbz),
                        },
                        new Model.Solid()
                        {
                            Offset = new VHE.Point(-1, -1, sbz),
                            Size = new VHE.Point(sbx + 2, sby + 2, 1),
                        },
                        new Model.Solid()
                        {
                            Offset = new VHE.Point(-1, -1, -1),
                            Size = new VHE.Point(sbx + 2, sby + 2, 1),
                        }
                    }
                };

                Map.AddSolids(Modelling.GenerateSolids(model, "sky"));
            }

            return Map;
        }

        private static void GroupPaneFence(Block block, BlockTexture bt, int x, int y, int z)
        {
            bool px = false, py = false;
            var model = BlockGroup.SType[bt.Model.ToUpper()];

            //X
            var paneX = BlockGroups.Find(p => p.Type == model && !p.XClosed &&
                p.Xmax == x && p.Ymin == y && p.Orientation != BlockGroup.Orient.Y && p.BlockID == block.ID);

            if (paneX == null) //create new pane
            {
                paneX = new BlockGroup(block.ID, block.Data, x, y, z);
                paneX.Type = model;

                //X
                var bmx = MCWorld.GetBlock(0, x + Xmin - 1, z + Ymin, y + Zmin);
                var btmx = Blocks.Find(e => e.ID == bmx.ID);
                var nbmx = btmx != null && btmx.Model == "Normal";
                var bpx = MCWorld.GetBlock(0, x + Xmin + 1, z + Ymin, y + Zmin);
                var btpx = Blocks.Find(e => e.ID == bpx.ID);
                var nbpx = btpx != null && btpx.Model == "Normal";
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
                var btp = Blocks.Find(e => e.ID == bp.ID);
                var nbp = btp != null && btp.Model == "Normal";

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
                var btmy = Blocks.Find(e => e.ID == bmy.ID);
                var nbmy = btmy != null && btmy.Model == "Normal";
                var bpy = MCWorld.GetBlock(0, x + Xmin, z + Ymin, y + Zmin + 1);
                var btpy = Blocks.Find(e => e.ID == bpy.ID);
                var nbpy = btpy != null && btpy.Model == "Normal";
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
                var btp = Blocks.Find(e => e.ID == bp.ID);
                var nbp = btp != null && btp.Model == "Normal";

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
            if (model == BlockGroup.SolidType.Fence)
            {
                var pillar = BlockGroups.Find(p => p.Type == model && !p.ZClosed && p.BlockID == block.ID &&
                    p.Orientation == BlockGroup.Orient.Z && p.Xmin == x && p.Ymin == y && p.Zmax == z);

                if (pillar == null)
                {
                    pillar = new BlockGroup(block.ID, block.Data, x, y, z);
                    pillar.Type = model;
                    pillar.Orientation = BlockGroup.Orient.Z;

                    var bpz = MCWorld.GetBlock(0, x + Xmin, z + Ymin + 1, y + Zmin);
                    var btmy = Blocks.Find(e => e.ID == bpz.ID);
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
                    var btmy = Blocks.Find(e => e.ID == bpz.ID);
                    if (btmy == null || btmy.ID != block.ID)
                    {
                        pillar.ZClosed = true;
                    }
                }
            }
        }

        private static void GroupNormal(Block block, int x, int y, int z, BlockTexture bt)
        {
            bool found = false;
            BlockGroup[] cuts = new BlockGroup[2];
            foreach (var solid in BlockGroups)
            {
                if (solid.Type != BlockGroup.SolidType.Normal && solid.Type != BlockGroup.SolidType.Liquid)
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

        private static void GenerateModelNormal(BlockGroup mcsolid, BlockTexture bt)
        {
            var model = new Model()
            {
                Solids = 
                { 
                    new Model.Solid() 
                    {
                        Size = new VHE.Point(
                        mcsolid.Xmax - mcsolid.Xmin,
                        mcsolid.Ymax - mcsolid.Ymin,
                        mcsolid.Zmax - mcsolid.Zmin)
                    } 
                }
            };

            if (mcsolid.Type == BlockGroup.SolidType.Liquid)
            {
                model.Solids[0].Size.Z -= 0.125f;
            }

            MapAddObject(Modelling.GenerateSolid(bt, mcsolid, model), bt);
        }

        private static void GenerateModelPane(BlockGroup mcsolid, BlockTexture bt)
        {
            const float th = 0.125f;

            float edgeOffset = 0.5f - th / 2;
            float length = th, rot = 0;
            float beg = 0, end = 0;
            VHE.Point align = new VHE.Point(0, 0, 1);
            VHE.Point offset = new VHE.Point(0.5f, 0.5f, 0);
            var face = bt.GetTextureName("face", null);
            var edge = bt.GetTextureName("edge", null);
            string tl = face, tr = face, tf = face;

            var bti = bt.Copy();
            bti.Textures = new List<BlockTexture.TextureKey>() { 
                new BlockTexture.TextureKey() 
                { 
                    Key = "vert",
                    Texture = edge
                }
            };

            switch (mcsolid.Orientation)
            {
                case BlockGroup.Orient.X:
                    if (!mcsolid.XBegTouch)
                    {
                        beg = edgeOffset;
                    }
                    else
                    {
                        tl = edge;
                    }
                    if (!mcsolid.XEndTouch)
                    {
                        end = edgeOffset;
                    }
                    else
                    {
                        tr = edge;
                    }
                    length = mcsolid.Xmax - mcsolid.Xmin - beg - end;
                    align = new VHE.Point(1, 0, 1);
                    offset = new VHE.Point(beg, 0.5f, 0);
                    break;

                case BlockGroup.Orient.Y:
                    if (!mcsolid.YBegTouch)
                    {
                        beg = edgeOffset;
                    }
                    else
                    {
                        tl = edge;
                    }
                    if (!mcsolid.YEndTouch)
                    {
                        end = edgeOffset;
                    }
                    else
                    {
                        tr = edge;
                    }
                    length = mcsolid.Ymax - mcsolid.Ymin - beg - end;
                    align = new VHE.Point(1, 0, 1);
                    offset = new VHE.Point(0.5f, beg, 0);
                    rot = 90;
                    break;
            }

            bti.Textures.Add(new BlockTexture.TextureKey()
            {
                Key = "face",
                Texture = tf
            });
            bti.Textures.Add(new BlockTexture.TextureKey()
            {
                Key = "left",
                Texture = tl
            });
            bti.Textures.Add(new BlockTexture.TextureKey()
            {
                Key = "right",
                Texture = tr
            });

            var model = new Model() {
                Solids = 
                { 
                    new Model.Solid() 
                    {
                        Size = new VHE.Point(length, th, mcsolid.Zmax - mcsolid.Zmin),
                        Rotation = new VHE.Point(0, 0, rot),
                        OriginAlign = align,
                        Offset = offset,
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

            var solid = Modelling.GenerateSolid(bti, mcsolid, model);
            MapAddObject(solid, bti);
        }

        private static void GenerateModelFence(BlockGroup mcsolid, BlockTexture bt)
        {
            //horizontal crossbars
            if (mcsolid.Orientation != BlockGroup.Orient.Z)
            {
                float zmin = mcsolid.Zmin + 0.375f;
                float zmax = mcsolid.Zmin + 0.5625f;
                float xmin = mcsolid.Xmin + 0.4375f;
                float xmax = mcsolid.Xmax - 0.4375f;
                float ymin = mcsolid.Ymin + 0.4375f;
                float ymax = mcsolid.Ymax - 0.4375f;

                if (mcsolid.Orientation == BlockGroup.Orient.X)
                {
                    if (mcsolid.XBegTouch)
                    {
                        xmin = mcsolid.Xmin;
                    }
                    else
                    {
                        xmin = mcsolid.Xmin + 0.5f;
                    }

                    if (mcsolid.XEndTouch)
                    {
                        xmax = mcsolid.Xmax;
                    }
                    else
                    {
                        xmax = mcsolid.Xmax - 0.5f;
                    }
                }
                else if (mcsolid.Orientation == BlockGroup.Orient.Y)
                {
                    if (mcsolid.YBegTouch)
                    {
                        ymin = mcsolid.Ymin;
                    }
                    else
                    {
                        ymin = mcsolid.Ymin + 0.5f;
                    }

                    if (mcsolid.YEndTouch)
                    {
                        ymax = mcsolid.Ymax;
                    }
                    else
                    {
                        ymax = mcsolid.Ymax - 0.5f;
                    }
                }

                if (mcsolid.Orientation != BlockGroup.Orient.None)
                {
                    var mdl = new Model() {
                        Solids = { new Model.Solid() {
                            Size = new VHE.Point(xmax - xmin, ymax - ymin, zmax - zmin),
                            TextureLockOffsets = true
                            } 
                        },
                        Position = new VHE.Point(xmin, ymin, zmin),
                    };
                    Map.AddSolid(Modelling.GenerateSolid(bt, mcsolid, mdl));

                    mdl.Position.Z += 0.375f;
                    Map.AddSolid(Modelling.GenerateSolid(bt, mcsolid, mdl));
                }
            }
            else //vertical pillars
            {
                var mdl = new Model() {
                    Solids = {
                        new Model.Solid() {
                            Size = new VHE.Point(0.25f, 0.25f, mcsolid.Zmax - mcsolid.Zmin),
                            Offset = new VHE.Point(0.375f, 0.375f, 0),
                            TextureLockOffsets = true
                        },
                    },       
                };
                Map.AddSolid(Modelling.GenerateSolid(bt, mcsolid, mdl));
            }
        }

        private static void GenerateModelDoor(BlockGroup mcsolid, BlockTexture bt)
        {
            const float th = 0.1875f;

            float rotate = 0, orx = 0, ory = 0;
            bool mirror = false;

            switch (mcsolid.BlockData)
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
                Modelling.RotatingOffset(ref orx, ref ory, rotate);
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
                        Size = new VHE.Point(0.125f, 0.125f, 0.125f),
                        Offset = new VHE.Point(0 + orx, th / 2 + ory, 1),
                        OriginAlign = new VHE.Point(0, 0, 0),
                        OriginRotOffset = new VHE.Point(0.5f, 0.5f - th / 2, 0),
                        Rotation = new VHE.Point(0, 0, rotate),
                        Textures =
                        {
                            new BlockTexture.TextureKey()
                            {
                                Texture = "origin"
                            }
                        }
                    }
                },
            };

            MapAddObject(Modelling.GenerateSolids(bt, mcsolid, model), bt);
        }

        private static void GenerateModelGrass(BlockGroup mcsolid, BlockTexture bt)
        {
            float len = (float)Math.Sqrt(2);

            var worldOffset = World.GetBlockXZOffset(mcsolid.Xmin + Xmin, mcsolid.Ymin + Zmin);
            var texture = Modelling.GetTexture(Wads, bt.GetTextureName(mcsolid.BlockData));
            float height = texture.Height / (int)TextureRes;

            var model = new Model()
            {
                Solids =
                {
                    new Model.Solid()
                    {
                        Size = new VHE.Point(len, 0, height),
                        Offset = new VHE.Point(0.5f + worldOffset[0], 0.5f + worldOffset[1], 0),
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
                        Offset = new VHE.Point(0.5f + worldOffset[0], 0.5f + worldOffset[1], 0),
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

            MapAddObject(Modelling.GenerateSolids(bt, mcsolid, model), bt);
        }

        private static void GenerateModelSpecial(BlockGroup mcsolid, BlockTexture bt)
        {
            var model = Models.Find(x => x.Name == bt.ModelName);

            if (bt.WorldOffset)
            {
                var worldOffset = World.GetBlockXZOffset(mcsolid.Xmin + Xmin, mcsolid.Ymin + Zmin);
                
                foreach (var sld in model.Solids)
                {
                    sld.Offset.X += worldOffset[0];
                    sld.Offset.Y += worldOffset[1];
                }
            }

            MapAddObject(Modelling.GenerateSolids(bt, mcsolid, model), bt);
        }

        private static VHE.Map.Solid CreateSolid(float xmin, float ymin, float zmin, float xmax, float ymax, float zmax, 
            string texture, bool rotate)
        {
            if (xmax <= xmin || ymax <= ymin || zmax <= zmin)
            {
                throw new Exception("Invalid dimension points");
            }

            var solid = new VHE.Map.Solid();
            VHE.Point au, av;

            if (rotate)
            {
                au = new VHE.Point(0, -1, 0);
                av = new VHE.Point(-1, 0, 0);
            }
            else
            {
                au = new VHE.Point(1, 0, 0);
                av = new VHE.Point(0, -1, 0);
            }

            //top
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = au,
                AxisV = av,
                ScaleU = CSScale / TextureRes,
                ScaleV = CSScale / TextureRes,
                Texture = texture,
                Vertexes = new VHE.Point[] {
                            new VHE.Point(xmin * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Point(xmax * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Point(xmax * CSScale, -ymax * CSScale, zmax * CSScale),
                        }
            });

            //bottom
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = au,
                AxisV = av,
                ScaleU = CSScale / TextureRes,
                ScaleV = CSScale / TextureRes,
                Texture = texture,
                Vertexes = new VHE.Point[] {
                            new VHE.Point(xmin * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Point(xmax * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Point(xmax * CSScale, -ymin * CSScale, zmin * CSScale),
                        }
            });

            //left
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Point(0, 1, 0),
                AxisV = new VHE.Point(0, 0, -1),
                ScaleU = CSScale / TextureRes,
                ScaleV = CSScale / TextureRes,
                Texture = texture,
                Vertexes = new VHE.Point[] {
                            new VHE.Point(xmin * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Point(xmin * CSScale, -ymax * CSScale, zmax * CSScale),
                            new VHE.Point(xmin * CSScale, -ymax * CSScale, zmin * CSScale),
                        }
            });

            //right
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Point(0, 1, 0),
                AxisV = new VHE.Point(0, 0, -1),
                ScaleU = CSScale / TextureRes,
                ScaleV = CSScale / TextureRes,
                Texture = texture,
                Vertexes = new VHE.Point[] {
                            new VHE.Point(xmax * CSScale, -ymin * CSScale, zmin * CSScale),
                            new VHE.Point(xmax * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Point(xmax * CSScale, -ymax * CSScale, zmax * CSScale),
                        }
            });

            //rear
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Point(1, 0, 0),
                AxisV = new VHE.Point(0, 0, -1),
                ScaleU = CSScale / TextureRes,
                ScaleV = CSScale / TextureRes,
                Texture = texture,
                Vertexes = new VHE.Point[] {
                            new VHE.Point(xmax * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Point(xmin * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Point(xmin * CSScale, -ymin * CSScale, zmin * CSScale),
                        }
            });

            //front
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Point(1, 0, 0),
                AxisV = new VHE.Point(0, 0, -1),
                ScaleU = CSScale / TextureRes,
                ScaleV = CSScale / TextureRes,
                Texture = texture,
                Vertexes = new VHE.Point[] {
                            new VHE.Point(xmax * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Point(xmin * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Point(xmin * CSScale, -ymax * CSScale, zmax * CSScale),
                        }
            });

            return solid;
        }

        private static void MapAddObject(VHE.Map.Solid solid, BlockTexture bt, 
            int blockData = 0, float x = 0, float y = 0, float z = 0)
        {
            MapAddObject(new List<VHE.Map.Solid>() { solid }, bt, blockData, x, y, z);
        }

        private static void MapAddObject(List<VHE.Map.Solid> solids, BlockTexture bt, 
            int blockData = 0, float x = 0, float y = 0, float z = 0)
        {
            var se = GetSolidEntity(bt);
            if (se != null)
            {
                var entity = GenerateEntity(se, blockData, x, y, z);
                solids.ForEach(s => entity.AddSolid(s));
                Map.CreateEntity(entity);
            }
            else
            {
                solids.ForEach(s => Map.AddSolid(s));
            }
        }


        private static EntityTemplate GetSolidEntity(BlockTexture bt)
        {
            if (bt.Entity != null)
            {
                return SolidEntities.Find(x => x.Macros.ToUpper() == bt.Entity.ToUpper());
            }

            return null;
        }

        private static bool PaneMerge(BlockGroup pane, int z)
        {
            if (pane.Type == BlockGroup.SolidType.Fence)
            {
                return false;
            }

            if (pane.XClosed || pane.YClosed)
            {
                //looking for the same pane in the previous Z layer
                var paneZ = BlockGroups.Find(pz => pz.Type == BlockGroup.SolidType.Pane &&
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

        private static BlockTexture GetBT(int id, int data)
        {
            int CheckData(BlockTexture bt)
            {
                if (bt.DataMask != 0)
                {
                    return data & bt.DataMask;
                }
                else
                {
                    if (bt.DataMax != 0 && data > bt.DataMax)
                    {
                        return -1;
                    }
                    else
                    {
                        return data;
                    }
                }
            }

            foreach (var bt in Blocks)
            {
                if (bt.ID == id)
                {
                    if (bt.Data == -1) //Ignore the data value
                    {
                        if (bt.IgnoreExcluded && CheckData(bt) == -1)
                        {
                            return new BlockTexture();
                        }

                        return bt;
                    }

                    if (bt.Data == CheckData(bt))
                    {
                        return bt;
                    }

                    if (bt.IgnoreExcluded)
                    {
                        return new BlockTexture();
                    }
                }
            }

            return null;
        }

        private static bool CompareID(Block block, int id, int data)
        {
            foreach (var bt in Blocks)
            {
                if (block.ID == id)
                {
                    if (bt.Data == -1) //Ignore the data value
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
            }

            return false;
        }

        private static void GenerateSignEntity(Block block, int x, int y, int z)
        {
            var chunk = MCWorld.GetChunkAtBlock(0, block.X, block.Z);
            List<NBT> tes = chunk.NBTData.GetTag("Level/TileEntities");

            foreach (var te in tes)
            {
                if (te.GetTag("id") == "minecraft:sign")
                {
                    int tex = te.GetTag("x");
                    int tey = te.GetTag("y");
                    int tez = te.GetTag("z");

                    if (block.X == tex && block.Y == tey && block.Z == tez)
                    {
                        string text = "";
                        for (int i = 1; i <= 4; i++)
                        {
                            string buf = te.GetTag("Text" + i);
                            int idx = buf.IndexOf("\"text\":");
                            int beg = buf.IndexOf("\"", idx + 7);
                            int end = buf.IndexOf("\"", beg + 1);
                            buf = buf.Substring(beg + 1, end - beg - 1);
                            text += buf + " ";
                        }

                        if (text.IndexOf("$") == 0)
                        {
                            int mend = text.IndexOf(" ");
                            var macros = text.Substring(1, mend - 1);

                            var objt = SignEntities.Find(o => o.Macros == macros);
                            if (objt == null)
                            {
                                Console.WriteLine("Undefined macros {0} at {1} {2} {3}", macros,
                                    block.X, block.Y, block.Z);
                            }
                            else
                            {
                                Map.Data.Add(GenerateEntity(objt, block, x, y, z));
                            }
                        }
                    }
                }
            }
        }

        private static VHE.Entity GenerateEntity(EntityTemplate entityTemplate, int blockData, float x, float y, float z)
        {
            var block = new Block() { 
                ID = 0,
                Data = (byte)blockData
            };

            return GenerateEntity(entityTemplate, block, x, y, z);
        }

        private static VHE.Entity GenerateEntity(EntityTemplate entityTemplate, Block block, float x, float y, float z)
        {
            var entity = new VHE.Entity(entityTemplate.ClassName);

            foreach (var part in entityTemplate.Parameters)
            {
                var par = new VHE.Entity.Parameter(part.Name);

                par.SetType(part.ValueType);

                string value = part.Value;

                while (value.IndexOf("{") != -1)
                {
                    int beg = value.IndexOf("{");
                    int end = value.IndexOf("}", beg + 1);

                    if (beg == -1 || end == -1)
                    {
                        break;
                    }

                    List<string> args = new List<string>();
                    var mac = value.Substring(beg + 1, end - beg - 1);

                    int sep = mac.IndexOf(" ");
                    if (sep == -1)
                    {
                        args.Add(mac);
                    }
                    else
                    {
                        int ab = 0;
                        while (sep != -1)
                        {
                            args.Add(mac.Substring(ab, sep - ab));
                            ab = sep + 1;
                            sep = mac.IndexOf(" ", ab);

                            if (sep == -1)
                            {
                                args.Add(mac.Substring(ab, mac.Length - ab));
                            }
                        }
                    }

                    value = value.Remove(beg, end - beg + 1);

                    string res = "";
                    switch (args[0].ToUpper())
                    {
                        case "ANGLE":
                            int vali = ((int)((-block.Data * 22.5f) + 90));
                            if (vali >= 360)
                            {
                                vali -= 360;
                            }
                            else if (vali < 0)
                            {
                                vali += 360;
                            }
                            res = vali.ToString();
                            break;

                        case "X":
                            double val = x + 0.5;
                            if (args.Count > 1)
                            {
                                val += Convert.ToDouble(args[1]);
                            }
                            res = (val * CSScale).ToString();
                            break;

                        case "Y":
                            val = -y - 0.5;
                            if (args.Count > 1)
                            {
                                val += Convert.ToDouble(args[1]);
                            }
                            res = (val * CSScale).ToString();
                            break;

                        case "Z":
                            val = z + 0.5;
                            if (args.Count > 1)
                            {
                                val += Convert.ToDouble(args[1]);
                            }
                            res = (val * CSScale).ToString();
                            break;

                        default:
                            if (block.ID == 0)
                            {
                                Console.WriteLine("Undefined submacros \"{0}\" at {1} {2} {3}", mac, x, y, z);
                            }
                            else
                            {
                                Console.WriteLine("Undefined submacros \"{0}\" at block {1} {2} {3}", mac,
                                    block.X, block.Y, block.Z);
                            }
                            break;
                    }

                    value = value.Insert(beg, res);
                }

                par.SetValue(value);
                entity.Parameters.Add(par);
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
                if (sld.Type != BlockGroup.SolidType.Normal)
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
                if (pane.Type != BlockGroup.SolidType.Pane)
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

        /**/

        private static void LoadResources(Resources res)
        {
            if (res == Resources.Wad || res == Resources.All)
            {
                Wads = new List<VHE.WAD>();
                Wads.Add(new VHE.WAD(@"D:\games\Counter-Strike 1.6 Chrome v3.11\cstrike\minecraft.wad"));
                Wads.Add(new VHE.WAD(@"D:\games\Counter-Strike 1.6 Chrome v3.11\cstrike\cstrike.wad"));
            }
            if (res == Resources.Textures || res == Resources.All)
            {
                Blocks = JsonConvert.DeserializeObject<List<BlockTexture>>(
                    File.ReadAllText(Resource[Resources.Textures]));
            }
            if (res == Resources.Models || res == Resources.All)
            {
                Models = JsonConvert.DeserializeObject<List<Model>>(
                    File.ReadAllText(Resource[Resources.Models]));
            }
            if (res == Resources.SignEntities || res == Resources.All)
            {
                SignEntities = JsonConvert.DeserializeObject<List<EntityTemplate>>(
                    File.ReadAllText(Resource[Resources.SignEntities]));
            }
            if (res == Resources.SignEntities || res == Resources.All)
            {
                SolidEntities = JsonConvert.DeserializeObject<List<EntityTemplate>>(
                    File.ReadAllText(Resource[Resources.SolidEntities]));
            }
        }
    }
}
