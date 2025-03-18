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

        public static int Xmin, Ymin, Zmin, Xmax, Ymax, Zmax; //mc coordinates

        public static bool Aborted { get; private set; }
        public static int BlockCount { get; private set; }

        private static List<BlockTexture> Blocks;
        private static List<VHE.WAD> Wads;
        private static List<EntityTemplate> SignEntities;
        private static List<EntityTemplate> SolidEntities;

        private static World MCWorld;
        private static VHE.Map Map;
        private static List<Solid> Solids;

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

            Map = new VHE.Map();
            Map.AddString("worldspawn", "wad", @"\cstrike\minecraft.wad");
            Solids = new List<Solid>();

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

                    /*if ((block.ID == 9 && block.Data != 0) || block.ID == 8)
                    {
                        Console.WriteLine("WATER: {0}:{1}", block.ID, block.Data);
                    }*/

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

                        if (block.ID != 0)
                        {
                            BlockCount++;
                        }

                        //Pane
                        var bt = Blocks.Find(a => a.ID == block.ID);

                        if (bt != null)
                        {
                            switch (bt.Model.ToUpper())
                            {
                                case "PANE":
                                case "FENCE":
                                    SolidPaneFence(block, bt, x, y, z);
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
                                        var mcsolid = new Solid(block.ID, data, x, y, z);
                                        mcsolid.Type = Solid.SolidType.Door;
                                        Solids.Add(mcsolid);
                                    }
                                    block.ID = 0;
                                    break;
                            }
                        }

                        //Normal block
                        SolidNormal(block, x, y, z, btm);

                        if (Debuging)
                        {
                            Debug(x, y, z);
                        }
                    }
                    Solids.ForEach(x => x.XClosed = true);

                }
                Solids.ForEach(x => x.YClosed = true);

            }
            Solids.ForEach(x => x.ZClosed = true);

            if (Debuging)
            {
                Debug(Xmax - Xmin, Zmax - Zmin, Ymax - Ymin);
            }

            //Generate cs solids
            Aborted = false;
            foreach (var mcsolid in Solids)
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
                    case Solid.SolidType.Normal:
                    case Solid.SolidType.Liquid:
                        GenerateModelNormal(mcsolid, bt);
                        break;

                    case Solid.SolidType.Pane:
                        GenerateModelPane(mcsolid, bt);
                        break;

                    case Solid.SolidType.Fence:
                        GenerateModelFence(mcsolid, bt);
                        break;

                    case Solid.SolidType.Door:
                        GenerateModelDoor(mcsolid, bt);
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

                Map.AddSolid(CreateSolid(-1, -1, 0, sbx + 1, 0, sbz + 1, "SKY", false));
                Map.AddSolid(CreateSolid(-1, sby, 0, sbx + 1, sby + 1, sbz + 1, "SKY", false));
                Map.AddSolid(CreateSolid(-1, 0, 0, 0, sby, sbz + 1, "SKY", false));
                Map.AddSolid(CreateSolid(sbx, 0, 0, sbx + 1, sby, sbz + 1, "SKY", false));
                Map.AddSolid(CreateSolid(0, 0, sbz, sbx, sby, sbz + 1, "SKY", false));
                Map.AddSolid(CreateSolid(-1, -1, -1, sbx + 1, sby + 1, 0, "SKY", false));
            }

            return Map;
        }

        private static void SolidPaneFence(Block block, BlockTexture bt, int x, int y, int z)
        {
            bool px = false, py = false;
            var model = Solid.SType[bt.Model.ToUpper()];

            //X
            var paneX = Solids.Find(p => p.Type == model && !p.XClosed &&
                p.Xmax == x && p.Ymin == y && p.Orientation != Solid.Orient.Y && p.BlockID == block.ID);

            if (paneX == null) //create new pane
            {
                paneX = new Solid(block.ID, block.Data, x, y, z);
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
                    paneX.Orientation = Solid.Orient.X;
                    paneX.XBegTouch = nbmx;
                    paneX.XEndTouch = nbpx && bpx.ID != block.ID;
                    paneX.XClosed = bpx.ID != block.ID;

                    if (!PaneMerge(paneX, z))
                    {
                        //create new pane
                        Solids.Add(paneX);
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
            var paneY = Solids.Find(p => p.Type == model && !p.YClosed &&
                p.Ymax == y && p.Xmin == x && p.Orientation != Solid.Orient.X && p.BlockID == block.ID);

            if (paneY == null) //create new pane
            {
                paneY = new Solid(block.ID, block.Data, x, y, z);
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
                    paneY.Orientation = Solid.Orient.Y;
                    paneY.YBegTouch = nbmy;
                    paneY.YEndTouch = nbpy && bpy.ID != block.ID;
                    paneY.YClosed = bpy.ID != block.ID;

                    if (!PaneMerge(paneY, z))
                    {
                        //create new pane
                        Solids.Add(paneY);
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
                    Solids.Add(paneY);
                }
            }

            //Z
            if (model == Solid.SolidType.Fence)
            {
                var pillar = Solids.Find(p => p.Type == model && !p.ZClosed && p.BlockID == block.ID &&
                    p.Orientation == Solid.Orient.Z && p.Xmin == x && p.Ymin == y && p.Zmax == z);

                if (pillar == null)
                {
                    pillar = new Solid(block.ID, block.Data, x, y, z);
                    pillar.Type = model;
                    pillar.Orientation = Solid.Orient.Z;

                    var bpz = MCWorld.GetBlock(0, x + Xmin, z + Ymin + 1, y + Zmin);
                    var btmy = Blocks.Find(e => e.ID == bpz.ID);
                    if (btmy == null || btmy.ID != block.ID)
                    {
                        pillar.ZClosed = true;
                    }

                    Solids.Add(pillar);
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

        private static void SolidNormal(Block block, int x, int y, int z, BlockTexture bt)
        {
            bool found = false;
            Solid[] cuts = new Solid[2];
            foreach (var solid in Solids)
            {
                if (solid.Type != Solid.SolidType.Normal && solid.Type != Solid.SolidType.Liquid)
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
                    Solids.Add(cut);
                    added = true;
                }
            }

            if (!found && block.ID != 0)
            {
                Solids.Add(new Solid(block.ID, block.Data, x, y, z) { 
                    Type = bt.GetSolidType()
                });
                var last = Solids.Last();
                Solids.Remove(last);
                Solids.Insert(0, last);
                added = true;
            }

            if (added)
            {
                int idmax = -1;
                foreach (var sld in Solids)
                {
                    if (sld.TestID > idmax || (sld.TestID != -1 && idmax == -1))
                    {
                        idmax = sld.TestID;
                    }
                }
                foreach (var sld in Solids)
                {
                    if (sld.TestID == -1)
                    {
                        sld.TestID = ++idmax;
                    }
                }
            }
        }

        private static void GenerateModelNormal(Solid mcsolid, BlockTexture bt)
        {
            float xmin = mcsolid.Xmin, ymin = mcsolid.Ymin, zmin = mcsolid.Zmin,
                xmax = mcsolid.Xmax, ymax = mcsolid.Ymax, zmax = mcsolid.Zmax;

            if (mcsolid.Type == Solid.SolidType.Liquid)
            {
                zmax -= 0.125f;
            }

            var solid = CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, bt, false);
            MapAddObject(solid, bt);
        }

        private static void GenerateModelPane(Solid mcsolid, BlockTexture bt)
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

            MapAddObject(solid, bt);
        }

        private static void GenerateModelFence(Solid mcsolid, BlockTexture bt)
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
                    Map.AddSolid(CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, bt, false));

                    zmin += 0.375f;
                    zmax += 0.375f;
                    Map.AddSolid(CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, bt, false));
                }
            }
            else //vertical pillars
            {
                Map.AddSolid(CreateSolid(mcsolid.Xmin + 0.375f, mcsolid.Ymin + 0.375f, mcsolid.Zmin,
                    mcsolid.Xmin + 0.625f, mcsolid.Ymin + 0.625f, mcsolid.Zmax, bt, false));
            }
        }

        private static void GenerateModelDoor(Solid mcsolid, BlockTexture bt)
        {
            float xmin = mcsolid.Xmin, xmax = mcsolid.Xmax, 
                ymin = mcsolid.Ymin, ymax = mcsolid.Ymax, 
                zmin = mcsolid.Zmin, zmax = mcsolid.Zmax + 1;

            float oxmin = 0, oymin = 0;

            bool mirror = false, rotate = false, offset = false;

            switch (mcsolid.BlockData)
            {
                case 0:
                case 13:
                    xmax -= 0.8125f;
                    oxmin = xmin + 0.03125f;
                    oymin = ymin - 0.0625f;
                    mirror = true;
                    break;

                case 1:
                case 14:
                    ymax -= 0.8125f;
                    oxmin = xmax - 0.0625f;
                    oymin = ymin + 0.03125f;
                    mirror = true;
                    rotate = true;
                    break;

                case 2:
                case 15:
                    xmin += 0.8125f;
                    oxmin = xmin + 0.03125f;
                    oymin = ymax - 0.0625f;
                    offset = true;
                    break;

                case 3:
                case 12:
                    ymin += 0.8125f;
                    oxmin = xmin - 0.0625f;
                    oymin = ymin + 0.03125f;
                    rotate = true;
                    offset = true;
                    break;

                case 4:
                case 9:
                    ymax -= 0.8125f;
                    oxmin = xmin - 0.0625f;
                    oymin = ymin + 0.03125f;
                    rotate = true;
                    break;

                case 5:
                case 10:
                    xmin += 0.8125f;
                    oxmin = xmin + 0.03125f;
                    oymin = ymin - 0.0625f;
                    mirror = true;
                    offset = true;
                    break;

                case 6:
                case 11:
                    ymin += 0.8125f;
                    oxmin = xmax - 0.0625f;
                    oymin = ymin + 0.03125f;
                    mirror = true;
                    rotate = true;
                    offset = true;
                    break;

                case 7:
                case 8:
                    xmax -= 0.8125f;
                    oxmin = xmin + 0.03125f;
                    oymin = ymax - 0.0625f;
                    break;
            }

            float oxmax = oxmin + 0.125f;
            float oymax = oymin + 0.125f;
            float ozmin = zmin + 0.9375f;
            float ozmax = zmax - 0.9375f;

            var door = CreateSolid(xmin, ymin, zmin, xmax, ymax, zmax, bt, rotate);
            var origin = CreateSolid(oxmin, oymin, ozmin, oxmax, oymax, ozmax, "origin", false);

            if (mirror)
            {
                door.Faces[2].MirrorTexture();
                door.Faces[3].MirrorTexture();
                door.Faces[4].MirrorTexture();
                door.Faces[5].MirrorTexture();
            }

            if (offset)
            {
                door.Faces[0].OffsetU = 0.1875f * TextureSize;
                door.Faces[1].OffsetU = 0.1875f * TextureSize;
            }

            var solids = new List<VHE.Map.Solid>() { door, origin };

            MapAddObject(solids, bt);
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

        /*private static void MapAddObject(VHE.Map.Solid solid, BlockTexture bt)
        {
            var se = GetSolidEntity(bt);
            if (se != null)
            {
                var entity = new VHE.Entity(se);
                entity.AddSolid(solid);
                Map.CreateEntity(entity);
            }
            else
            {
                Map.AddSolid(solid);
            }
        }*/

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

        private static EntityTemplate GetSolidEntity(BlockTexture bt)
        {
            if (bt.Entity != null)
            {
                return SolidEntities.Find(x => x.Macros.ToUpper() == bt.Entity.ToUpper());
            }

            return null;
        }

        private static bool PaneMerge(Solid pane, int z)
        {
            if (pane.Type == Solid.SolidType.Fence)
            {
                return false;
            }

            if (pane.XClosed || pane.YClosed)
            {
                //looking for the same pane in the previous Z layer
                var paneZ = Solids.Find(pz => pz.Type == Solid.SolidType.Pane &&
                    pz.Xmin == pane.Xmin && pz.Xmax == pane.Xmax &&
                    pz.Ymin == pane.Ymin && pz.Ymax == pane.Ymax && pz.Zmax == z &&
                    pz.XBegTouch == pane.XBegTouch && pz.XEndTouch == pane.XEndTouch &&
                    pz.YBegTouch == pane.YBegTouch && pz.YEndTouch == pane.YEndTouch &&
                    pz.BlockID == pane.BlockID && pz.BlockData == pane.BlockData);

                if (paneZ != null)
                {
                    paneZ.Zmax++;
                    Solids.Remove(pane);
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
                    if (bt.Data == -1) //Ignore the data value
                    {
                        return bt;
                    }

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
                        dat = data;
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
            for (int i = 0; i < Solids.Count; i++)
            {
                var sld = Solids[i];
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

            for (int i = 0; i < Solids.Count; i++)
            {
                var pane = Solids[i];
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
