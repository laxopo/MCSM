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
        public static List<EntityScript> SolidEntities { get; set; }

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
            List<VHE.WAD> wads, List<EntityScript> solidEntities)
        {
            Scale = scale;
            TextureRes = textureSize;
            Wads = wads;

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
            return GenerateSolids(new BlockDescriptor(), null, model, texture)[0];
        }

        public static VHE.Map.Solid GenerateSolid(BlockDescriptor bt, BlockGroup bg, Model model = null, string texture = null)
        {
            if (model == null)
            {
                model = CreateModel(bg);
            }
 
            return GenerateSolids(bt, bg, model, texture)[0];
        }

        public static List<VHE.Map.Solid> GenerateSolids(Model model, string texture = null)
        {
            return GenerateSolids(null, null, model, texture);
        }

        public static List<VHE.Map.Solid> GenerateSolids(BlockDescriptor bt, BlockGroup bg, 
            Model model, string texture = null)
        {
            var modelBuf = new Model(model);

            if (modelBuf == null)
            {
                throw new Exception("Model not found");
            }

            VHE.Point pos;
            if (modelBuf.Position != null)
            {
                pos = modelBuf.Position;
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
            bool textureInput = texture != null;

            foreach (var mdlSolid in modelBuf.Solids)
            {
                //get textures
                if (textureInput)
                {
                    foreach (var face in mdlSolid.GetFaces())
                    {
                        if (face.Texture == null)
                        {
                            face.Texture = texture;
                        }
                    }
                }
                else
                {
                    if (mdlSolid.TextureScale > 0)
                    {
                        foreach (var face in mdlSolid.GetFaces())
                        {
                            face.ScaleU *= mdlSolid.TextureScale;
                            face.ScaleV *= mdlSolid.TextureScale;
                        }
                    }

                    //if (modelBuf.TextureKeys.Count == 0 && bt != null && bt.TextureOriented)
                    if (bt != null && bt.TextureOriented)
                    {
                        mdlSolid.TextureOriented = true;
                    }

                    if (mdlSolid.TextureOriented)
                    {
                        mdlSolid.Face(Model.Faces.Rear).MirrorV ^= true;
                        mdlSolid.Face(Model.Faces.Right).MirrorV ^= true;
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        if (mdlSolid.TexturedFaces != null)
                        {
                            if (!mdlSolid.TexturedFaces.ToList().Contains((Model.Faces)i))
                            {
                                continue;
                            }
                        }

                        if (bt != null)
                        {
                            bt.Textures.ForEach(x => modelBuf.TextureKeys.Add(x));
                        }

                        if (mdlSolid.Face(i).Texture == null)
                        {
                            mdlSolid.Face(i).Texture = TextureBySide(i, mdlSolid.Name, modelBuf.TextureKeys, blockData);
                        }

                        /*if (modelBuf.TextureKeys.Count > 0)
                        {
                            if (mdlSolid.Face(i).Texture == null)
                            {
                                mdlSolid.Face(i).Texture = TextureBySide(i, mdlSolid.Name, modelBuf.TextureKeys, blockData);
                            }
                        }
                        else
                        {
                            if (bt != null)
                            {
                                var face = mdlSolid.Face(i);
                                if (face.Texture == null)
                                {
                                    face.Texture = TextureBySide(i, mdlSolid.Name, bt.Textures, blockData);
                                }
                            }
                        }*/
                    }
                }

                solids.Add(CreateSolid(mdlSolid, pos, modelBuf.Origin, modelBuf.Rotation));
            }

            return solids;
        }

        public static VHE.Map.Solid CreateSolid(Model.Solid mdlSolid, VHE.Point pos, VHE.Point origin, VHE.Point rotation)
        {
            //validate the size values
            if (mdlSolid.Size.X < 0 || mdlSolid.Size.Y < 0 || mdlSolid.Size.Z < 0)
            {
                throw new Exception("Invalid solid size");
            }

            if (mdlSolid.Size.X < MinSize)
            {
                mdlSolid.Size.X = MinSize;
            }

            if (mdlSolid.Size.Y < MinSize)
            {
                mdlSolid.Size.Y = MinSize;
            }

            if (mdlSolid.Size.Z < MinSize)
            {
                mdlSolid.Size.Z = MinSize;
            }

            //set the solid origin (rot point is still zero)
            float xmin = GetEdgeMin(mdlSolid.Size.X, mdlSolid.OriginAlign.X),
                xmax = GetEdgeMax(mdlSolid.Size.X, mdlSolid.OriginAlign.X),
                ymin = GetEdgeMin(mdlSolid.Size.Y, mdlSolid.OriginAlign.Y),
                ymax = GetEdgeMax(mdlSolid.Size.Y, mdlSolid.OriginAlign.Y),
                zmin = GetEdgeMin(mdlSolid.Size.Z, mdlSolid.OriginAlign.Z),
                zmax = GetEdgeMax(mdlSolid.Size.Z, mdlSolid.OriginAlign.Z);

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

            //create texture axes
            var aus = new VHE.Point[6];
            var avs = new VHE.Point[6];
            for (int i = 0; i < 6; i++)
            {
                aus[i] = VectorsU[i].Copy();
                avs[i] = VectorsV[i].Copy();

                if (mdlSolid.Face(i).MirrorV)
                {
                    aus[i] = ReverseVector(aus[i]);
                }

                if (mdlSolid.Face(i).MirrorU)
                {
                    avs[i] = ReverseVector(avs[i]);
                }
            }

            //Solid rotation
            //set solid rotation point
            foreach (var pt in pts)
            {
                pt.X = pt.X - mdlSolid.OriginRotOffset.X;
                pt.Y = pt.Y + mdlSolid.OriginRotOffset.Y;
                pt.Z = pt.Z - mdlSolid.OriginRotOffset.Z;
            }

            //rotate solid
            for (int i = 0; i < 8; i++)
            {
                Rotate3D(pts[i], -mdlSolid.Rotation.X, mdlSolid.Rotation.Y, -mdlSolid.Rotation.Z);
            }

            //rotate UV axes
            for (int i = 0; i < 6; i++)
            {
                Rotate3D(aus[i], -mdlSolid.Rotation.X, mdlSolid.Rotation.Y, -mdlSolid.Rotation.Z);
                Rotate3D(avs[i], -mdlSolid.Rotation.X, mdlSolid.Rotation.Y, -mdlSolid.Rotation.Z);
            }

            //shift back and set the solid offset, and set the model rotation point
            foreach (var pt in pts)
            {
                pt.X = pt.X + mdlSolid.OriginRotOffset.X + mdlSolid.Offset.X - origin.X;
                pt.Y = pt.Y - mdlSolid.OriginRotOffset.Y - mdlSolid.Offset.Y + origin.Y;
                pt.Z = pt.Z + mdlSolid.OriginRotOffset.Z + mdlSolid.Offset.Z - origin.Z;
            }

            //Model rotation
            //rotate solid
            for (int i = 0; i < 8; i++)
            {
                Rotate3D(pts[i], -rotation.X, rotation.Y, -rotation.Z);
            }

            //rotate UV axes
            for (int i = 0; i < 6; i++)
            {
                Rotate3D(aus[i], -rotation.X, rotation.Y, -rotation.Z);
                Rotate3D(avs[i], -rotation.X, rotation.Y, -rotation.Z);
            }

            //shift back and set the solid final position
            foreach (var pt in pts)
            {
                pt.X = (pt.X + pos.X + origin.X + mdlSolid.AbsOffset.X) * Scale;
                pt.Y = (pt.Y - pos.Y - origin.Y - mdlSolid.AbsOffset.Y) * Scale;
                pt.Z = (pt.Z + pos.Z + origin.Z + mdlSolid.AbsOffset.Z) * Scale;
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

            //face texturing
            for (int i = 0; i < 6; i++)
            {
                var face = solid.Faces[i];

                face.Rotation = RotationLimit(mdlSolid.Face(i).Rotation);
                RotateUV(aus[i], avs[i], face.Rotation);
                face.AxisU = aus[i];
                face.AxisV = avs[i];

                face.Texture = GetTextureName(mdlSolid, i);
                

                var wadTexture = GetTexture(Wads, face.Texture);
                if (wadTexture == null)
                {
                    continue;
                }

                float tw = wadTexture.Width;
                float th = wadTexture.Height;

                Model.Face mdlFace = mdlSolid.Face(i);
                if (mdlFace == null)
                {
                    mdlFace = new Model.Face();
                }

                float fou = 0, fov = 0;
                if (mdlFace.Frame)
                {
                    fou = TextureRes / 16;
                    fov = TextureRes / 16;
                }

                var faceSize = GetFaceSize(mdlSolid, i);

                if (mdlFace.StretchU)
                {
                    face.ScaleU = Scale / (tw - fou * 2) * faceSize[0];
                }
                else
                {
                    face.ScaleU = Scale / TextureRes;
                }

                if (mdlFace.StretchV)
                {
                    face.ScaleV = Scale / (th - fov * 2) * faceSize[1];
                }
                else
                {
                    face.ScaleV = Scale / TextureRes;
                }

                face.ScaleU *= mdlFace.ScaleU;
                face.ScaleV *= mdlFace.ScaleV;

                float k = 1;
                if (!mdlFace.UnscaledOffset)
                {
                    k = TextureRes / 16;
                }

                var offU = mdlFace.OffsetU * k;
                var offV = mdlFace.OffsetV * k;

                var pt = face.Vertexes[0];
                var u = (pt.X * aus[i].X + pt.Y * aus[i].Y + pt.Z * aus[i].Z) / face.ScaleU;
                var v = (pt.X * avs[i].X + pt.Y * avs[i].Y + pt.Z * avs[i].Z) / face.ScaleV;

                if (mdlSolid.TextureLockOffsets)
                {
                    u = u % tw - u % TextureRes;
                    v = v % th - v % TextureRes;
                }

                var us = Sign(!mdlFace.MirrorV);
                var vs = Sign(!mdlFace.MirrorU);

                if (face.Rotation != 0)
                {
                    var kf = TextureRes / 16;
                    mdlFace.Origin.X *= us * kf / mdlFace.ScaleU;
                    mdlFace.Origin.Y *= vs * kf / mdlFace.ScaleV;

                    u -= mdlFace.Origin.X;
                    v -= mdlFace.Origin.Y;
                    Rotate2D(ref u, ref v, mdlFace.Rotation);
                    u += mdlFace.Origin.X;
                    v += mdlFace.Origin.Y;
                    Trans2D(ref u, ref v, mdlFace.Rotation);
                }

                face.OffsetU = (tw - u + (offU + fou) * us) % tw;
                face.OffsetV = (th - v + (offV + fov) * vs) % th;
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
                var tex = wad.GetTexture(textureName);
                if (tex != null)
                {
                    return tex;
                }
            }

            return null;
        }

        public static float GetEdgeMin(float size, float originAlign)
        {
            return size * (float)(-0.5 + 0.5 * originAlign);
        }

        public static float GetEdgeMax(float size, float originAlign)
        {
            return size * (float)(0.5 + 0.5 * originAlign);
        }

        /**/

        public static int Sign(bool value)
        {
            if (value)
            {
                return 1;
            }

            return -1;
        }

        private static string TextureBySide(int side, string solidName, List<BlockDescriptor.TextureKey> tks, int blockData)
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

            return BlockDescriptor.GetTextureName(tks, blockData, solidName, keys);
        }

        private static string GetTextureName(Model.Solid sld, int index)
        {
            var face = sld.Face(index);

            if (face == null)
            {
                return null;
            }

            if (face.Texture == "")
            {
                return null;
            }

            return face.Texture;
        }

        /*Geometry*/

        public static void Rotate2D(ref float x, ref float y, float angle)
        {
            if (angle == 0)
            {
                return;
            }

            var bx = (float)(x * Cos(angle) - y * Sin(angle));
            var by = (float)(x * Sin(angle) + y * Cos(angle));
            x = bx;
            y = by;
        }

        public static void Rotate2D(ref int x, ref int y, float angle)
        {
            if (angle == 0)
            {
                return;
            }

            var bx = (int)(x * Cos(angle) - y * Sin(angle));
            var by = (int)(x * Sin(angle) + y * Cos(angle));
            x = bx;
            y = by;
        }

        public static void Trans2D(ref float x, ref float y, float angle)
        {
            if (angle == 0)
            {
                return;
            }

            var bx = (float)(x * Cos(angle) + y * Sin(angle));
            var by = (float)(-x * Sin(angle) + y * Cos(angle));
            x = bx;
            y = by;
        }

        public static VHE.Point ReverseVector(VHE.Point point)
        {
            var mr = point.Copy();
            mr.X *= -1;
            mr.Y *= -1;
            mr.Z *= -1;

            return mr;
        }

        public static void Rotate3D(VHE.Point pt, double rotX, double rotY, double rotZ)
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

        public static void RotateUV(VHE.Point u, VHE.Point v, float rot)
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
    }
}
