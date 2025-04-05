using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public static class Modelling
    {
        public static float Scale { get; set; }
        public static float TextureRes { get; set; }
        public static float MinSize { get; set; }
        public static List<VHE.WAD> Wads { get; set; }
        public static List<Model> Models { get; set; }
        public static List<EntityTemplate> SolidEntities { get; set; }

        public static readonly double Sqrt2 = Math.Sqrt(2);

        private static readonly VHE.Point[] VectorsU = new VHE.Point[] {
                new VHE.Point(1, 0, 0),
                new VHE.Point(1, 0, 0),
                new VHE.Point(0, -1, 0),
                new VHE.Point(0, 1, 0),
                new VHE.Point(-1, 0, 0),
                new VHE.Point(1, 0, 0),
            };

        private static readonly VHE.Point[] VectorsV = new VHE.Point[] {
                new VHE.Point(0, -1, 0),
                new VHE.Point(0, 1, 0),
                new VHE.Point(0, 0, -1),
                new VHE.Point(0, 0, -1),
                new VHE.Point(0, 0, -1),
                new VHE.Point(0, 0, -1),
            };

        public static void Initialize(float scale, float textureSize, 
            List<VHE.WAD> wads, List<Model> models, List<EntityTemplate> solidEntities)
        {
            Scale = scale;
            TextureRes = textureSize;
            Wads = wads;
            Models = models;
            SolidEntities = solidEntities;
            MinSize = 0.11f / Scale;
        }

        public static Model CreateModel(BlockGroup bg)
        {
            return new Model()
            {
                Solids = { new Model.Solid() {
                    Size = new VHE.Point(
                        bg.Xmax - bg.Xmin,
                        bg.Ymax - bg.Ymin,
                        bg.Zmax - bg.Zmin)} }
            };
        }

        public static VHE.Map.Solid GenerateSolid(Model model, string texture)
        {

            var textures = new string[6];
            for (int i = 0; i < 6; i++)
            {
                textures[i] = texture;
            }

            return GenerateSolids(new BlockTexture(), null, model, textures)[0];
        }

        public static VHE.Map.Solid GenerateSolid(BlockTexture bt, BlockGroup bg, Model model = null, string[] textures = null)
        {
            if (model == null)
            {
                model = CreateModel(bg);
            }
 
            return GenerateSolids(bt, bg, model, textures)[0];
        }

        public static List<VHE.Map.Solid> GenerateSolids(Model model, string texture)
        {
            var textures = new string[6];
            for (int i = 0; i < 6; i++)
            {
                textures[i] = texture;
            }

            return GenerateSolids(null, null, model, textures);
        }

        public static List<VHE.Map.Solid> GenerateSolids(BlockTexture bt, BlockGroup bg, 
            Model model, string[] textures = null)
        {
            if (model == null)
            {
                if (bt == null)
                {
                    throw new Exception("Model not found");
                }

                model = Models.Find(x => x.Name == bt.ModelName);

                if (model == null)
                {
                    throw new Exception("Model not found");
                }
            }

            VHE.Point pos;
            if (model.Position != null)
            {
                pos = model.Position;
            }
            else
            {
                if (bg == null)
                {
                    throw new Exception("Position of the solid is not specified");
                }

                pos = new VHE.Point(bg.Xmin, bg.Ymin, bg.Zmin);
            }

            int blockData = -1;
            if (bg != null)
            {
                blockData = bg.BlockData;
            }

            List<VHE.Map.Solid> solids = new List<VHE.Map.Solid>();
            bool textureInput = textures != null;

            foreach (var mdlSolid in model.Solids)
            {
                //get textures
                if (!textureInput)
                {
                    if (mdlSolid.TextureScale > 0)
                    {
                        foreach (var face in mdlSolid.Faces)
                        {
                            face.ScaleU = mdlSolid.TextureScale;
                            face.ScaleV = mdlSolid.TextureScale;
                        }
                    }

                    if (mdlSolid.Textures.Count == 0 && bt != null && bt.TextureOriented)
                    {
                        mdlSolid.TextureOriented = true;
                    }

                    if (mdlSolid.TextureOriented)
                    {
                        mdlSolid.Face(Model.Faces.Rear).MirrorV ^= true;
                        mdlSolid.Face(Model.Faces.Right).MirrorV ^= true;
                    }

                    textures = new string[6];

                    for (int i = 0; i < 6; i++)
                    {
                        if (mdlSolid.TexturedFaces != null)
                        {
                            if (!mdlSolid.TexturedFaces.ToList().Contains((Model.Faces)i))
                            {
                                continue;
                            }
                        }

                        if (mdlSolid.Textures.Count > 0)
                        {
                            textures[i] = TextureBySide(i, mdlSolid.Name, mdlSolid.Textures, blockData);
                        }
                        else
                        {
                            if (bt == null)
                            {
                                throw new Exception("Block mapper is not specified");
                            }

                            textures[i] = TextureBySide(i, mdlSolid.Name, bt.Textures, blockData);
                        }
                    }
                }

                solids.Add(CreateSolid(pos, mdlSolid, textures));
            }

            return solids;
        }

        public static VHE.Map.Solid CreateSolid(VHE.Point pos, Model.Solid mdlSolid, string[] textures)
        {
            if (mdlSolid.Size.X < 0 || mdlSolid.Size.Y < 0 || mdlSolid.Size.Z < 0)
            {
                throw new Exception("Invalid solid size");
            }

            if (mdlSolid.Size.X == 0)
            {
                mdlSolid.Size.X = MinSize;
            }

            if (mdlSolid.Size.Y == 0)
            {
                mdlSolid.Size.Y = MinSize;
            }

            if (mdlSolid.Size.Z == 0)
            {
                mdlSolid.Size.Z = MinSize;
            }

            //set origin
            float xmin = mdlSolid.Size.X * (float)(-0.5 + 0.5 * mdlSolid.OriginAlign.X) - mdlSolid.OriginRotOffset.X, 
                xmax = mdlSolid.Size.X * (float)(0.5 + 0.5 * mdlSolid.OriginAlign.X) - mdlSolid.OriginRotOffset.X,
                ymin = mdlSolid.Size.Y * (float)(-0.5 + 0.5 * mdlSolid.OriginAlign.Y) - mdlSolid.OriginRotOffset.Y, 
                ymax = mdlSolid.Size.Y * (float)(0.5 + 0.5 * mdlSolid.OriginAlign.Y) - mdlSolid.OriginRotOffset.Y,
                zmin = mdlSolid.Size.Z * (float)(-0.5 + 0.5 * mdlSolid.OriginAlign.Z) - mdlSolid.OriginRotOffset.Z, 
                zmax = mdlSolid.Size.Z * (float)(0.5 + 0.5 * mdlSolid.OriginAlign.Z) - mdlSolid.OriginRotOffset.Z;
           
            //create faces points
            var pts = new VHE.Point[] {
                new VHE.Point(xmin, -ymax, zmax), //0  -  -  +
                new VHE.Point(xmax, -ymax, zmax), //1  +  -  +
                new VHE.Point(xmax, -ymin, zmax), //2  +  +  +
                new VHE.Point(xmin, -ymin, zmin), //3  -  +  -
                new VHE.Point(xmax, -ymin, zmin), //4  +  +  -
                new VHE.Point(xmax, -ymax, zmin), //5  +  -  -
                new VHE.Point(xmin, -ymin, zmax), //6  -  +  +
                new VHE.Point(xmin, -ymax, zmin)  //7  -  -  -
            };

            //create texture axes vectors
            var aus = new VHE.Point[6];
            var avs = new VHE.Point[6];
            for (int i = 0; i < 6; i++)
            {
                aus[i] = VectorsU[i].Copy();
                avs[i] = VectorsV[i].Copy();

                if (mdlSolid.Face(i).MirrorV)
                {
                    aus[i] = Mirror(aus[i]);
                }

                if (mdlSolid.Face(i).MirrorU)
                {
                    avs[i] = Mirror(avs[i]);
                }
            }

            //Rotate points and vectors
            for (int i = 0; i < 8; i++)
            {
                RotateXYZ(pts[i], -mdlSolid.Rotation.X, mdlSolid.Rotation.Y, -mdlSolid.Rotation.Z);
            }

            for (int i = 0; i < 6; i++)
            {
                RotateXYZ(aus[i], -mdlSolid.Rotation.X, mdlSolid.Rotation.Y, -mdlSolid.Rotation.Z);
                RotateXYZ(avs[i], -mdlSolid.Rotation.X, mdlSolid.Rotation.Y, -mdlSolid.Rotation.Z);
            }

            //Move
            foreach (var pt in pts)
            {
                pt.X = (pt.X + pos.X + mdlSolid.Offset.X + mdlSolid.OriginRotOffset.X) * Scale;
                pt.Y = (pt.Y - pos.Y - mdlSolid.Offset.Y - mdlSolid.OriginRotOffset.Y) * Scale;
                pt.Z = (pt.Z + pos.Z + mdlSolid.Offset.Z + mdlSolid.OriginRotOffset.Z) * Scale;
            }

            //create a solid
            var solid = new VHE.Map.Solid()
            {
                Faces = {
                    new VHE.Face() { //top
                        Vertexes = new VHE.Point[] { pts[6], pts[2], pts[1] }
                    },
                    new VHE.Face() { //bottom
                        Vertexes = new VHE.Point[] { pts[7], pts[5], pts[4] }
                    },
                    new VHE.Face() { //left
                        Vertexes = new VHE.Point[] { pts[6], pts[0], pts[7] }
                    },
                    new VHE.Face() { //right
                        Vertexes = new VHE.Point[] { pts[1], pts[2], pts[4] }
                    },
                    new VHE.Face() { //rear
                        Vertexes = new VHE.Point[] { pts[2], pts[6], pts[3] }
                    },
                    new VHE.Face() { //front
                        Vertexes = new VHE.Point[] { pts[0], pts[1], pts[5] }
                    }
                }
            };

            //set textures axes, offsets
            for (int i = 0; i < 6; i++)
            {
                var face = solid.Faces[i];
                face.Rotation = RotationLimit(mdlSolid.Face(i).Rotation);

                RotateUV(aus[i], avs[i], face.Rotation);
                face.AxisU = aus[i];
                face.AxisV = avs[i];

                face.Texture = GetTextureName(textures, i);

                var wadTexture = GetTexture(Wads, face.Texture);
                if (wadTexture == null)
                {
                    continue;
                }

                float tw = wadTexture.Width;
                float th = wadTexture.Height;

                Model.Face tparams = mdlSolid.Face(i);
                if (tparams == null)
                {
                    tparams = new Model.Face();
                }

                float fou = 0, fov = 0;
                if (tparams.Frame)
                {
                    fou = -TextureRes / 16;
                    fov = -TextureRes / 16;
                }

                var faceSize = GetFaceSize(mdlSolid, i);

                if (tparams.StretchU)
                {
                    face.ScaleU = Scale / (tw + fou * 2) * faceSize[0];
                }
                else
                {
                    face.ScaleU = Scale / TextureRes;
                }

                if (tparams.StretchV)
                {
                    face.ScaleV = Scale / (th + fov * 2) * faceSize[1];
                }
                else
                {
                    face.ScaleV = Scale / TextureRes;
                }

                face.ScaleU *= mdlSolid.Face(i).ScaleU;
                face.ScaleV *= mdlSolid.Face(i).ScaleV;

                var offU = tparams.OffsetU * TextureRes / face.ScaleU;
                var offV = tparams.OffsetV * TextureRes / face.ScaleU;

                var pt = face.Vertexes[0];
                var u = (pt.X * aus[i].X + pt.Y * aus[i].Y + pt.Z * aus[i].Z) / face.ScaleU;
                var v = (pt.X * avs[i].X + pt.Y * avs[i].Y + pt.Z * avs[i].Z) / face.ScaleV;
                var uf = (float)(tw / Sqrt2 * Cos(-face.Rotation + 45));
                var vf = (float)(th / Sqrt2 * Sin(-face.Rotation + 45));

                if (mdlSolid.TextureLockOffsets)
                {
                    u = u % tw - u % TextureRes;
                    v = v % th - v % TextureRes;
                }

                face.OffsetU = tw / 2 - (u - uf + offU + fou * -Sign(mdlSolid.Face(i).MirrorV)) % tw;
                face.OffsetV = th / 2 - (v - vf + offV + fov * -Sign(mdlSolid.Face(i).MirrorU)) % th;
            }

            return solid;
        }

        public static VHE.WAD.Texture GetTexture(List<VHE.WAD> wads, string textureName)
        {
            if (textureName == null)
            {
                return null;
            }

            foreach (var wad in wads)
            {
                var txt = wad.Textures.Find(t => t.Name.ToUpper() == textureName.ToUpper());
                if (txt != null)
                {
                    return txt;
                }
            }

            return null;
        }

        /**/

        private static string TextureBySide(int side, string solidName, List<BlockTexture.TextureKey> tks, int blockData)
        {
            string[] keys;

            switch (side)
            {
                case 0:
                    keys = new string[] { "top", "vert", null };
                    break;

                case 1:
                    keys = new string[] { "bottom", "vert", null };
                    break;

                case 2:
                    keys = new string[] { "left", "side", "edge", null };
                    break;

                case 3:
                    keys = new string[] { "right", "side", "edge", null };
                    break;

                case 4:
                    keys = new string[] { "rear", "side", "face", null };
                    break;

                case 5:
                    keys = new string[] { "front", "side", "face", null };
                    break;

                default:
                    throw new Exception("Side index out of range");
            }

            return BlockTexture.GetTextureName(tks, blockData, solidName, keys);
        }

        private static string GetTextureName(string[] array, int index)
        {
            if (array == null)
            {
                return null;
            }    

            if (index >= array.Length)
            {
                return null;
            }

            var txt = array[index];
            if (txt == "")
            {
                return null;
            }

            return txt;
        }

        private static int Sign(bool value)
        {
            if (value)
            {
                return 1;
            }

            return -1;
        }

        /*Geometry*/

        public static void RotatingOffset(ref float x, ref float y, float angle)
        {
            x = (float)Cos(angle);
            y = (float)Sin(angle);
        }

        private static void RotateXYZ(VHE.Point pt, double rotX, double rotY, double rotZ)
        {
            if (rotX == 0 && rotY == 0 && rotZ == 0)
            {
                return;
            }

            double rx = GetRad(rotX);
            double ry = GetRad(rotY);
            double rz = GetRad(rotZ);

            var m1 = new double[,]
            {
                {
                    Math.Cos(ry) * Math.Cos(rz),
                    Math.Sin(rx) * Math.Sin(ry) * Math.Cos(rz) - Math.Cos(rx) * Math.Sin(rz),
                    Math.Cos(rx) * Math.Sin(ry) * Math.Cos(rz) + Math.Sin(rx) * Math.Sin(rz)
                },
                {
                    Math.Cos(ry) * Math.Sin(rz),
                    Math.Sin(rx) * Math.Sin(ry) * Math.Sin(rz) + Math.Cos(rx) * Math.Cos(rz),
                    Math.Cos(rx) * Math.Sin(ry) * Math.Sin(rz) - Math.Sin(rx) * Math.Cos(rz)
                },
                {
                    -Math.Sin(ry),
                    Math.Sin(rx) * Math.Cos(ry),
                    Math.Cos(rx) * Math.Cos(ry)
                }
            };

            var m2 = new double[] { pt.X, pt.Y, pt.Z };
            var m3 = new double[3];

            for (int row = 0; row < 3; row++)
            {
                double sum = 0;
                for (int col = 0; col < 3; col++)
                {
                    sum += m1[row, col] * m2[col];
                }

                m3[row] = sum;
            }

            var offsets = new VHE.Point
            {
                X = pt.X - (float)m3[0],
                Y = pt.Y - (float)m3[1],
                Z = pt.Z - (float)m3[2]
            };

            pt.X = (float)m3[0];
            pt.Y = (float)m3[1];
            pt.Z = (float)m3[2];
        }

        private static void RotateUV(VHE.Point u, VHE.Point v, float rot)
        {
            if (rot == 0)
            {
                return;
            }

            var rad = GetRad(rot);
            var u0 = new VHE.Point(u);
            var v0 = new VHE.Point(v);

            u.X = (float)(u0.X * Math.Cos(rad) + v0.X * Math.Sin(rad));
            u.Y = (float)(u0.Y * Math.Cos(rad) + v0.Y * Math.Sin(rad));
            u.Z = (float)(u0.Z * Math.Cos(rad) + v0.Z * Math.Sin(rad));

            v.X = (float)(-u0.X * Math.Sin(rad) + v0.X * Math.Cos(rad));
            v.Y = (float)(-u0.Y * Math.Sin(rad) + v0.Y * Math.Cos(rad));
            v.Z = (float)(-u0.Z * Math.Sin(rad) + v0.Z * Math.Cos(rad));
        }

        private static double GetRad(float deg)
        {
            return deg * Math.PI / 180;
        }

        private static double GetRad(double deg)
        {
            return deg * Math.PI / 180;
        }

        private static double Sin(double deg)
        {
            return Math.Sin(GetRad(deg));
        }

        private static double Cos(double deg)
        {
            return Math.Cos(GetRad(deg));
        }

        private static float[] GetFaceSize(Model.Solid mdlSolid, int side)
        {
            float[] size;

            switch (side)
            {
                case 0:
                case 1:
                    size = new float[] { mdlSolid.Size.X, mdlSolid.Size.Y };
                    break;

                case 2:
                case 3:
                    size = new float[] { mdlSolid.Size.Y, mdlSolid.Size.Z };
                    break;

                case 4:
                case 5:
                    size = new float[] { mdlSolid.Size.X, mdlSolid.Size.Z };
                    break;

                default:
                    throw new Exception("Side out of range");
            }

            return size;
        }

        private static int VectDir(VHE.Point vector)
        {
            if (vector.X < 0 || vector.Y < 0 || vector.Z < 0)
            {
                return -1;
            }

            return 1;
        }

        private static float RotationLimit(float angle)
        {
            if (angle < 0)
            {
                return 360 - angle;
            }
            else if (angle >= 360)
            {
                return angle %= 360;
            }

            return angle;
        }

        private static VHE.Point Mirror(VHE.Point point)
        {
            var mr = point.Copy();
            mr.X *= -1;
            mr.Y *= -1;
            mr.Z *= -1;

            return mr;
        }
    }
}
