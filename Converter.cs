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
        public static float TextureSize = 128;
        public static bool SkyBoxEnable = true;

        public static bool Aborted;
        public static int BlockCount;

        public static List<BlockTexture> Blocks;
        public static VHE.WAD Wad;
        public static List<ObjectTemplate> ObjectTemplates;

        public static Dictionary<Resources, string> Resource = new Dictionary<Resources, string>() {
            {Resources.Models, "models.json"},
            {Resources.Wad, @"D:\games\Counter-Strike 1.6 Chrome v3.11\cstrike\minecraft.wad"},
            {Resources.Textures, "blocks.json"},
            {Resources.Objects, "objects.json"}
        };

        public enum Resources
        {
            All,
            Models,
            Wad,
            Textures,
            Objects
        }

        public static VHE.Map ConvertToMap(World world, int mcxmin, int mcymin, int mczmin, int mcxmax, int mcymax, int mczmax)
        {
            Settings.DebugEnable = !Debuging;
            BlockCount = 0;
            Aborted = false;
            LoadResources(Resources.All);

            VHE.Map map = new VHE.Map();
            map.Textures.Add(@"\cstrike\minecraft.wad");

            var missings = new BlockMissMsg();
            var mcsolids = new List<Solid>();

            //coordinates of cs map
            for (int z = 0; z <= mcymax - mcymin; z++)
            {
                for (int y = 0; y <= mczmax - mczmin; y++)
                {
                    for (int x = 0; x <= mcxmax - mcxmin; x++)
                    {
                        var block = world.GetBlock(0, x + mcxmin, z + mcymin, y + mczmin);
                        
                        //object
                        if (block.ID == 63)
                        {
                            SignObjects(world, block, x, y, z, map);
                            block.ID = 0;
                        }

                        //check register
                        bt_chk:
                        if (block.ID != 0 && GetBT(block.ID, block.Data) == null)
                        {
                            var res = missings.Message(block.ID, block.Data, "at " + (x + mcxmin) + " " + 
                                (z + mcymin) + " " + (y + mczmin) + " is unregistered", true);

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

                        if (block.ID != 0)
                        {
                            BlockCount++;
                        }

                        //Pane
                        var bt0 = Blocks.Find(bt => bt.ID == block.ID && (bt.Model == "Pane" || bt.Model == "Fence"));
                        bool isPane = bt0 != null;

                        if (isPane)
                        {
                            bool px = false, py = false;
                            var model = Solid.SType[bt0.Model];

                            //X
                            var paneX = mcsolids.Find(p => p.Type == model && !p.XClosed &&
                                p.Xmax == x && p.Ymin == y && p.Orientation != Solid.Orient.Y && p.BlockID == block.ID);

                            if (paneX == null) //create new pane
                            {
                                paneX = new Solid(block.ID, block.Data, x, y, z);
                                paneX.Type = model;

                                //X
                                var bmx = world.GetBlock(0, x + mcxmin - 1, z + mcymin, y + mczmin);
                                var btmx = Blocks.Find(e => e.ID == bmx.ID);
                                var nbmx = btmx != null && btmx.Model == "Normal";
                                var bpx = world.GetBlock(0, x + mcxmin + 1, z + mcymin, y + mczmin);
                                var btpx = Blocks.Find(e => e.ID == bpx.ID);
                                var nbpx = btpx != null && btpx.Model == "Normal";
                                px = nbmx || nbpx || bpx.ID == block.ID;

                                if (px)
                                {
                                    paneX.Orientation = Solid.Orient.X;
                                    paneX.XBegTouch = nbmx;
                                    paneX.XEndTouch = nbpx && bpx.ID != block.ID;
                                    paneX.XClosed = bpx.ID != block.ID;

                                    if (!PaneMerge(mcsolids, paneX, z))
                                    {
                                        //create new pane
                                        mcsolids.Add(paneX);
                                    }
                                }
                            }
                            else //expand existing pane
                            {
                                paneX.Expand(x, y, z);
                                px = true;

                                var bp = world.GetBlock(0, x + mcxmin + 1, z + mcymin, y + mczmin);
                                var btp = Blocks.Find(e => e.ID == bp.ID);
                                var nbp = btp != null && btp.Model == "Normal";

                                if (bp.ID != block.ID)
                                {
                                    paneX.XClosed = true;
                                    if (nbp)
                                    {
                                        paneX.XEndTouch = true;
                                    }

                                    PaneMerge(mcsolids, paneX, z);
                                }
                            }


                            //Y
                            var paneY = mcsolids.Find(p => p.Type == model && !p.YClosed &&
                                p.Ymax == y && p.Xmin == x && p.Orientation != Solid.Orient.X && p.BlockID == block.ID);

                            if (paneY == null) //create new pane
                            {
                                paneY = new Solid(block.ID, block.Data, x, y, z);
                                paneY.Type = model;

                                //Y
                                var bmy = world.GetBlock(0, x + mcxmin, z + mcymin, y + mczmin - 1);
                                var btmy = Blocks.Find(e => e.ID == bmy.ID);
                                var nbmy = btmy != null && btmy.Model == "Normal";
                                var bpy = world.GetBlock(0, x + mcxmin, z + mcymin, y + mczmin + 1);
                                var btpy = Blocks.Find(e => e.ID == bpy.ID);
                                var nbpy = btpy != null && btpy.Model == "Normal";
                                py = nbmy || nbpy || bpy.ID == block.ID;

                                if (py)
                                {
                                    paneY.Orientation = Solid.Orient.Y;
                                    paneY.YBegTouch = nbmy;
                                    paneY.YEndTouch = nbpy && bpy.ID != block.ID;
                                    paneY.YClosed = bpy.ID != block.ID;

                                    if (!PaneMerge(mcsolids, paneY, z))
                                    {
                                        //create new pane
                                        mcsolids.Add(paneY);
                                    }
                                }
                            }
                            else //expand existing pane
                            {
                                paneY.Expand(x, y, z);
                                py = true;

                                var bp = world.GetBlock(0, x + mcxmin, z + mcymin, y + mczmin + 1);
                                var btp = Blocks.Find(e => e.ID == bp.ID);
                                var nbp = btp != null && btp.Model == "Normal";

                                if (bp.ID != block.ID)
                                {
                                    paneY.YClosed = true;
                                    if (nbp)
                                    {
                                        paneY.YEndTouch = true;
                                    }

                                    PaneMerge(mcsolids, paneY, z);
                                }
                            }

                            //None
                            if (!px && !py)
                            {
                                if (!PaneMerge(mcsolids, paneY, z))
                                {
                                    //create new pane
                                    mcsolids.Add(paneY);
                                }
                            }

                            //Z
                            if (model == Solid.SolidType.Fence)
                            {
                                var pillar = mcsolids.Find(p => p.Type == model && !p.ZClosed && p.BlockID == block.ID &&
                                    p.Orientation == Solid.Orient.Z && p.Xmin == x && p.Ymin == y && p.Zmax == z);

                                if (pillar == null)
                                {
                                    pillar = new Solid(block.ID, block.Data, x, y, z);
                                    pillar.Type = model;
                                    pillar.Orientation = Solid.Orient.Z;

                                    var bpz = world.GetBlock(0, x + mcxmin, z + mcymin + 1, y + mczmin);
                                    var btmy = Blocks.Find(e => e.ID == bpz.ID);
                                    if (btmy == null || btmy.ID != block.ID)
                                    {
                                        pillar.ZClosed = true;
                                    }

                                    mcsolids.Add(pillar);
                                }
                                else
                                {
                                    pillar.Expand(x, y, z);
                                    var bpz = world.GetBlock(0, x + mcxmin, z + mcymin + 1, y + mczmin);
                                    var btmy = Blocks.Find(e => e.ID == bpz.ID);
                                    if (btmy == null || btmy.ID != block.ID)
                                    {
                                        pillar.ZClosed = true;
                                    }
                                }
                            }
                        }

                        //Normal block
                        bool found = false;
                        Solid[] cuts = new Solid[2];
                        foreach (var solid in mcsolids)
                        {
                            if (solid.Type != Solid.SolidType.Normal)
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
                                if (solid.BlockID == block.ID && solid.BlockData == block.Data && !found && !isPane)
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
                                mcsolids.Add(cut);
                                added = true;
                            }
                        }

                        if (!found && block.ID != 0  && block.ID != 63 && !isPane)
                        {
                            mcsolids.Add(new Solid(block.ID, block.Data, x, y, z));
                            var last = mcsolids.Last();
                            mcsolids.Remove(last);
                            mcsolids.Insert(0, last);
                            added = true;
                        }

                        if (added)
                        {
                            int idmax = -1;
                            foreach (var sld in mcsolids)
                            {
                                if (sld.TestID > idmax || (sld.TestID != -1 && idmax == -1))
                                {
                                    idmax = sld.TestID;
                                }
                            }
                            foreach (var sld in mcsolids)
                            {
                                if (sld.TestID == -1)
                                {
                                    sld.TestID = ++idmax;
                                }
                            }
                        }

                        if (Debuging)
                        {
                            Debug(x, y, z, mcxmin, mcxmax, mcymin, mcymax, mczmin, mczmax, mcsolids);
                        }
                    }
                    mcsolids.ForEach(x => x.XClosed = true);

                }
                mcsolids.ForEach(x => x.YClosed = true);

            }
            mcsolids.ForEach(x => x.ZClosed = true);

            if (Debuging)
            {
                Debug(mcxmax - mcxmin, mczmax - mczmin, mcymax - mcymin,
                    mcxmin, mcxmax, mcymin, mcymax, mczmin, mczmax, mcsolids);
            }

            //Generate cs solids
            Aborted = false;
            foreach (var mcsolid in mcsolids)
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
                            break;
                    }
                    if (Aborted)
                    {
                        break;
                    }
                }

                switch (mcsolid.Type)
                {
                    case Solid.SolidType.Normal:
                        var solid = CreateSolid(mcsolid.Xmin, mcsolid.Ymin, mcsolid.Zmin,
                            mcsolid.Xmax, mcsolid.Ymax, mcsolid.Zmax, bt, false);
                        map.Solids.Add(solid);
                        break;
                    case Solid.SolidType.Pane:
                        GenerateModelPane(map, mcsolid, bt);
                        break;
                    case Solid.SolidType.Fence:
                        GenerateModelFence(map, mcsolid, bt);
                        break;
                }
            }

            //Generate skybox
            if (SkyBoxEnable)
            {
                var sbx = mcxmax - mcxmin + 1;
                var sby = mczmax - mczmin + 1;
                var sbz = mcymax - mcymin + 1; 

                map.Solids.Add(CreateSolid(-1, -1, 0, sbx + 1, 0, sbz + 1, "SKY", false));
                map.Solids.Add(CreateSolid(-1, sby, 0, sbx + 1, sby + 1, sbz + 1, "SKY", false));
                map.Solids.Add(CreateSolid(-1, 0, 0, 0, sby, sbz + 1, "SKY", false));
                map.Solids.Add(CreateSolid(sbx, 0, 0, sbx + 1, sby, sbz + 1, "SKY", false));
                map.Solids.Add(CreateSolid(0, 0, sbz, sbx, sby, sbz + 1, "SKY", false));
            }

            return map;
        }

        private static void GenerateModelPane(VHE.Map map, Solid mcsolid, BlockTexture bt)
        {
            float xmin = mcsolid.Xmin + 0.4375f;
            float ymin = mcsolid.Ymin + 0.4375f;
            float xmax = mcsolid.Xmax - 0.4375f;
            float ymax = mcsolid.Ymax - 0.4375f;

            if (mcsolid.Orientation == Solid.Orient.X)
            {
                if (mcsolid.XBegTouch)
                {
                    xmin = mcsolid.Xmin;
                }
                if (mcsolid.XEndTouch)
                {
                    xmax = mcsolid.Xmax;
                }
            }
            else if (mcsolid.Orientation == Solid.Orient.Y)
            {
                if (mcsolid.YBegTouch)
                {
                    ymin = mcsolid.Ymin;
                }
                if (mcsolid.YEndTouch)
                {
                    ymax = mcsolid.Ymax;
                }
            }

            var textureSide = GetTextureName(bt, "side");
            var textureFace = GetTextureName(bt, "face");

            var solid = CreateSolid(xmin, ymin, mcsolid.Zmin, xmax, ymax, mcsolid.Zmax, textureFace,
                mcsolid.Orientation == Solid.Orient.X);

            solid.Faces[0].Texture = textureSide;
            solid.Faces[1].Texture = textureSide;

            if (mcsolid.Orientation == Solid.Orient.X)
            {
                if (mcsolid.XBegTouch)
                {
                    solid.Faces[2].Texture = textureSide;
                }
                if (mcsolid.XEndTouch)
                {
                    solid.Faces[3].Texture = textureSide;
                }
            }
            else if (mcsolid.Orientation == Solid.Orient.Y)
            {
                if (mcsolid.YBegTouch)
                {
                    solid.Faces[5].Texture = textureSide;
                }
                if (mcsolid.YEndTouch)
                {
                    solid.Faces[4].Texture = textureSide;
                }
            }

            map.Solids.Add(solid);
        }

        private static void GenerateModelFence(VHE.Map map, Solid mcsolid, BlockTexture bt)
        {
            //horizontal crossbars
            if (mcsolid.Orientation != Solid.Orient.Z)
            {
                float zmin = mcsolid.Zmin + 0.375f;
                float zmax = mcsolid.Zmin + 0.5625f;
                float xmin = mcsolid.Xmin + 0.4375f;
                float xmax = mcsolid.Xmax - 0.4375f;
                float ymin = mcsolid.Ymin + 0.4375f;
                float ymax = mcsolid.Ymax - 0.4375f;

                if (mcsolid.Orientation == Solid.Orient.X)
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
                else if (mcsolid.Orientation == Solid.Orient.Y)
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

                if (mcsolid.Orientation != Solid.Orient.None)
                {
                    map.Solids.Add(CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, bt, false));

                    zmin += 0.375f;
                    zmax += 0.375f;
                    map.Solids.Add(CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, bt, false));
                }
            }
            else //vertical pillars
            {
                map.Solids.Add(CreateSolid(mcsolid.Xmin + 0.375f, mcsolid.Ymin + 0.375f, mcsolid.Zmin,
                    mcsolid.Xmin + 0.625f, mcsolid.Ymin + 0.625f, mcsolid.Zmax, bt, false));
            }
        }

        private static VHE.Map.Solid CreateSolid(float xmin, float ymin, float zmin, float xmax, float ymax, float zmax, 
            BlockTexture bt, bool rotate)
        {
            var solid = CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, (string)null, rotate);

            solid.Faces[0].Texture = GetTextureName(bt, "top", "vert", null);
            solid.Faces[1].Texture = GetTextureName(bt, "bottom", "vert", null);
            solid.Faces[2].Texture = GetTextureName(bt, "left", "side", null);
            solid.Faces[3].Texture = GetTextureName(bt, "rith", "side", null);
            solid.Faces[4].Texture = GetTextureName(bt, "rear", "side", null);
            solid.Faces[5].Texture = GetTextureName(bt, "front", "side", null);

            return solid;
        }

        private static VHE.Map.Solid CreateSolid(float xmin, float ymin, float zmin, float xmax, float ymax, float zmax, 
            string texture, bool rotate)
        {
            if (xmax <= xmin || ymax <= ymin || zmax <= zmin)
            {
                throw new Exception("Invalid dimension points");
            }

            var solid = new VHE.Map.Solid();
            VHE.Vector au, av;

            if (rotate)
            {
                au = new VHE.Vector(0, -1, 0);
                av = new VHE.Vector(-1, 0, 0);
            }
            else
            {
                au = new VHE.Vector(1, 0, 0);
                av = new VHE.Vector(0, -1, 0);
            }

            //top
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = au,
                AxisV = av,
                ScaleU = CSScale / TextureSize,
                ScaleV = CSScale / TextureSize,
                Texture = texture,
                Vertexes = new VHE.Vector[] {
                            new VHE.Vector(xmin * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Vector(xmax * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Vector(xmax * CSScale, -ymax * CSScale, zmax * CSScale),
                        }
            });

            //bottom
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = au,
                AxisV = av,
                ScaleU = CSScale / TextureSize,
                ScaleV = CSScale / TextureSize,
                Texture = texture,
                Vertexes = new VHE.Vector[] {
                            new VHE.Vector(xmin * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Vector(xmax * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Vector(xmax * CSScale, -ymin * CSScale, zmin * CSScale),
                        }
            });

            //left
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Vector(0, 1, 0),
                AxisV = new VHE.Vector(0, 0, -1),
                ScaleU = CSScale / TextureSize,
                ScaleV = CSScale / TextureSize,
                Texture = texture,
                Vertexes = new VHE.Vector[] {
                            new VHE.Vector(xmin * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Vector(xmin * CSScale, -ymax * CSScale, zmax * CSScale),
                            new VHE.Vector(xmin * CSScale, -ymax * CSScale, zmin * CSScale),
                        }
            });

            //right
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Vector(0, 1, 0),
                AxisV = new VHE.Vector(0, 0, -1),
                ScaleU = CSScale / TextureSize,
                ScaleV = CSScale / TextureSize,
                Texture = texture,
                Vertexes = new VHE.Vector[] {
                            new VHE.Vector(xmax * CSScale, -ymin * CSScale, zmin * CSScale),
                            new VHE.Vector(xmax * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Vector(xmax * CSScale, -ymax * CSScale, zmax * CSScale),
                        }
            });

            //rear
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Vector(1, 0, 0),
                AxisV = new VHE.Vector(0, 0, -1),
                ScaleU = CSScale / TextureSize,
                ScaleV = CSScale / TextureSize,
                Texture = texture,
                Vertexes = new VHE.Vector[] {
                            new VHE.Vector(xmax * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Vector(xmin * CSScale, -ymin * CSScale, zmax * CSScale),
                            new VHE.Vector(xmin * CSScale, -ymin * CSScale, zmin * CSScale),
                        }
            });

            //front
            solid.Faces.Add(new VHE.Face()
            {
                AxisU = new VHE.Vector(1, 0, 0),
                AxisV = new VHE.Vector(0, 0, -1),
                ScaleU = CSScale / TextureSize,
                ScaleV = CSScale / TextureSize,
                Texture = texture,
                Vertexes = new VHE.Vector[] {
                            new VHE.Vector(xmax * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Vector(xmin * CSScale, -ymax * CSScale, zmin * CSScale),
                            new VHE.Vector(xmin * CSScale, -ymax * CSScale, zmax * CSScale),
                        }
            });

            return solid;
        }

        private static string GetTextureName(BlockTexture bt, params string[] keys)
        {
            BlockTexture.TextureKey tk = null;

            foreach (var key in keys)
            {
                tk = bt.Textures.Find(x => x.Key == key);
                if (tk != null)
                {
                    break;
                }
            }

            if (tk != null)
            {
                return tk.Texture;
            }

            return null;
        }

        private static bool PaneMerge(List<Solid> mcsolids, Solid pane, int z)
        {
            if (pane.Type == Solid.SolidType.Fence)
            {
                return false;
            }

            if (pane.XClosed || pane.YClosed)
            {
                //looking for the same pane in the previous Z layer
                var paneZ = mcsolids.Find(pz => pz.Type == Solid.SolidType.Pane &&
                    pz.Xmin == pane.Xmin && pz.Xmax == pane.Xmax &&
                    pz.Ymin == pane.Ymin && pz.Ymax == pane.Ymax && pz.Zmax == z &&
                    pz.XBegTouch == pane.XBegTouch && pz.XEndTouch == pane.XEndTouch &&
                    pz.YBegTouch == pane.YBegTouch && pz.YEndTouch == pane.YEndTouch &&
                    pz.BlockID == pane.BlockID && pz.BlockData == pane.BlockData);

                if (paneZ != null)
                {
                    paneZ.Zmax++;
                    mcsolids.Remove(pane);
                    return true;
                }
            }

            return false;
        }

        private static BlockTexture GetBT(int id, int data)
        {
            foreach (var bt in Blocks)
            {
                if (bt.ID == id)
                {
                    int dat;
                    if (bt.DataMask != 0)
                    {
                        dat = data & bt.DataMask;
                    }
                    else
                    {
                        dat = data;
                    }

                    if (bt.Data == dat)
                    {
                        return bt;
                    }
                }
            }

            return null;
        }

        private static void SignObjects(World world, Block block, int x, int y, int z, VHE.Map map)
        {
            var chunk = world.GetChunkAtBlock(0, block.X, block.Z);
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

                            var objt = ObjectTemplates.Find(o => o.Macros == macros);
                            if (objt == null)
                            {
                                Console.WriteLine("Undefined macros {0} at {1} {2} {3}", macros,
                                    block.X, block.Y, block.Z);
                            }
                            else
                            {
                                var obj = new VHE.Object() { ClassName = objt.ClassName };

                                foreach (var part in objt.Parameters)
                                {
                                    var par = new VHE.Object.Parameter()
                                    {
                                        Name = part.Name,
                                    };

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
                                        switch (args[0])
                                        {
                                            case "angle":
                                                res = ((int)(block.Data * 22.5f)).ToString();
                                                break;

                                            case "x":
                                                double val = x;
                                                if (args.Count > 1)
                                                {
                                                    val += Convert.ToDouble(args[1]);
                                                }
                                                res = (val * CSScale).ToString();
                                                break;

                                            case "y":
                                                val = -y;
                                                if (args.Count > 1)
                                                {
                                                    val += Convert.ToDouble(args[1]);
                                                }
                                                res = (val * CSScale).ToString();
                                                break;

                                            case "z":
                                                val = z + 1;
                                                if (args.Count > 1)
                                                {
                                                    val += Convert.ToDouble(args[1]);
                                                }
                                                res = (val * CSScale).ToString();
                                                break;

                                            default:
                                                Console.WriteLine("Undefined submacros \"{0}\" at {1} {2} {3}", mac,
                                                    block.X, block.Y, block.Z);
                                                break;
                                        }

                                        value = value.Insert(beg, res);
                                        
                                    }

                                    par.SetValue(value);
                                    obj.Parameters.Add(par);
                                }

                                map.Objects.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        /*Debug*/
        private static void Debug(int x, int y, int z, int xmin, int xmax, int ymin, int ymax, int zmin, int zmax,
           List<Solid> mcsolids)
        {
            Settings.DebugEnable = false;
            Console.CursorVisible = false;

            //Build empty field
            for (int cz = 0; cz <= ymax - ymin; cz++)
            {
                for (int cy = 0; cy <= zmax - zmin; cy++)
                {
                    for (int cx = 0; cx <= xmax - xmin; cx++)
                    {
                        if (cx == x && cy == y && cz == z)
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }

                        Console.SetCursorPosition(cx + (xmax - xmin + 4) * cz + 1, cy + 1);
                        Console.Write(".");

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition((xmax - xmin + 4) * cz + 1, zmax - zmin + 2);
                Console.Write("z=" + cz);

                Console.SetCursorPosition((xmax - xmin + 4) * cz + 1, 0);
                for (int i = 0; i <= xmax - xmin; i++)
                {
                    Console.Write(ToHex(i));
                }
                for (int i = 0; i <= zmax - zmin; i++)
                {
                    Console.SetCursorPosition((xmax - xmin + 4) * cz, i + 1);
                    Console.Write(ToHex(i));
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            //Render solids
            for (int i = 0; i < mcsolids.Count; i++)
            {
                var sld = mcsolids[i];
                if (sld.Type != Solid.SolidType.Normal)
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

                            Console.SetCursorPosition(cx + (xmax - xmin + 4) * cz + 1, cy + 1);
                            Console.Write(ToHex(sld.TestID));
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.White;

                        }
                    }
                }
            }


            //Render panes
            char[,,] matrix = new char[xmax - xmin + 1, zmax - zmin + 1, ymax - ymin + 1];

            for (int i = 0; i < mcsolids.Count; i++)
            {
                var pane = mcsolids[i];
                if (pane.Type != Solid.SolidType.Pane)
                {
                    continue;
                }

                for (int cz = pane.Zmin; cz < pane.Zmax; cz++)
                {
                    for (int cy = pane.Ymin; cy < pane.Ymax; cy++)
                    {
                        for (int cx = pane.Xmin; cx < pane.Xmax; cx++)
                        {
                            if (pane.Orientation == Solid.Orient.X)
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
                            else if (pane.Orientation == Solid.Orient.Y)
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

            for (int cz = 0; cz <= ymax - ymin; cz++)
            {
                for (int cy = 0; cy <= zmax - zmin; cy++)
                {
                    for (int cx = 0; cx <= xmax - xmin; cx++)
                    {
                        if (cx == x && cy == y && cz == z)
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                        }

                        if (matrix[cx, cy, cz] != '\0')
                        {
                            Console.SetCursorPosition(cx + (xmax - xmin + 4) * cz + 1, cy + 1);
                            Console.Write(matrix[cx, cy, cz]);
                        }

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }


            Console.SetCursorPosition(0, zmax - zmin + 4);
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
                Wad = new VHE.WAD(Resource[Resources.Wad]);
            }
            if (res == Resources.Textures || res == Resources.All)
            {
                Blocks = JsonConvert.DeserializeObject<List<BlockTexture>>(File.ReadAllText(Resource[Resources.Textures]));
            }
            if (res == Resources.Objects || res == Resources.All)
            {
                ObjectTemplates = JsonConvert.DeserializeObject<List<ObjectTemplate>>(File.ReadAllText(Resource[Resources.Objects]));
            }
        }
    }
}
