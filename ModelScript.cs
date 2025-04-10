using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class ModelScript
    {
        public string Name { get; set; }
        public List<Solid> Solids { get; set; } = new List<Solid>();

        public class Solid
        {
            //string
            public string Name { get; set; }

            //point
            public string AbsOffset { get; set; }
            public string Offset { get; set; }
            public string Size { get; set; }
            public string OriginAlign { get; set; }
            public string OriginRotOffset { get; set; }
            public string Rotation { get; set; }

            //float
            public string TextureScale { get; set; }

            //bool
            public bool TextureLockOffsets { get; set; }
            public bool TextureOriented { get; set; }
            
            //Face sides
            public string[] TexturedFaces { get; set; }

            //Texture keys
            public List<BlockDecsriptor.TextureKey> Textures { get; set; }
                = new List<BlockDecsriptor.TextureKey>();

            //Faces
            public Face[] Faces { get; set; }

            public class Face
            {
                //string
                public string Name { get; set; }
                public string Texture { get; set; }

                //float
                public string OffsetU { get; set; }
                public string OffsetV { get; set; }
                public string ScaleU { get; set; }
                public string ScaleV { get; set; }
                public string Rotation { get; set; }

                //bool
                public bool UnscaledOffset { get; set; }
                public bool StretchU { get; set; }
                public bool StretchV { get; set; }
                public bool MirrorU { get; set; }
                public bool MirrorV { get; set; }
                public bool Frame { get; set; }
            }
        }

        public enum Type
        {
            Point,
            Float,
            FaceList,
        }

        public Dictionary<Type, VHE.Entity.Type> Types = new Dictionary<Type, VHE.Entity.Type>()
        {
            {Type.Point, VHE.Entity.Type.Point },
            {Type.Float, VHE.Entity.Type.Float },
        };

        private static class Defaults
        {
            public static VHE.Point Mul = new VHE.Point(1, 1, 1);
            public static VHE.Point Zero = new VHE.Point(); 
        }

        public Model GetModel()
        {
            var model = new Model();
            model.Name = Name;
            model.Solids = new List<Model.Solid>();

            foreach (var sld in Solids)
            {
                var faces = new List<Model.Face>();

                if (sld.Faces != null)
                {
                    foreach (var fc in sld.Faces)
                    {
                        var face = new Model.Face()
                        {
                            Name = Model.GetFaceEnum(fc.Name),
                            Texture = fc.Texture,

                            OffsetU = Parse(fc.OffsetU, Type.Float, 0),
                            OffsetV = Parse(fc.OffsetV, Type.Float, 0),
                            ScaleU = Parse(fc.ScaleU, Type.Float, 1),
                            ScaleV = Parse(fc.ScaleV, Type.Float, 1),
                            Rotation = Parse(fc.Rotation, Type.Float, 0),

                            Frame = fc.Frame,
                            MirrorU = fc.MirrorU,
                            MirrorV = fc.MirrorV,
                            StretchU = fc.StretchU,
                            StretchV = fc.StretchV,
                            UnscaledOffset = fc.UnscaledOffset
                        };

                        faces.Add(face);
                    }
                }

                var solid = new Model.Solid()
                {
                    Name = sld.Name,

                    AbsOffset = Parse(sld.AbsOffset, Type.Point, Defaults.Zero),
                    Offset = Parse(sld.Offset, Type.Point, Defaults.Zero),
                    OriginAlign = Parse(sld.OriginAlign, Type.Point, Defaults.Mul),
                    OriginRotOffset = Parse(sld.OriginRotOffset, Type.Point, Defaults.Zero),
                    Rotation = Parse(sld.Rotation, Type.Point, Defaults.Zero),
                    Size = Parse(sld.Size, Type.Point, Defaults.Zero),

                    TextureScale = Parse(sld.TextureScale, Type.Float, 1),

                    TextureLockOffsets = sld.TextureLockOffsets,
                    TextureOriented = sld.TextureOriented,

                    TexturedFaces = Parse(sld.TexturedFaces, Type.FaceList, null),
                    Textures = sld.Textures,
                    Faces = faces
                };

                model.Solids.Add(solid);
            }

            return model;
        }

        private dynamic Parse(object value, Type type, object defvalue)
        {
            if (value == null)
            {
                return defvalue;
            }

            switch (type)
            {
                case Type.Float:
                case Type.Point:
                    return VHE.Entity.DeserializeValue(value as string, Types[type]);

                case Type.FaceList:
                    var faces = new List<Model.Faces>();
                    foreach (var str in value as string[])
                    {
                        faces.Add(Model.GetFaceEnum(str));
                    }
                    return faces.ToArray();

                default:
                    throw new Exception("Unknown type");
            }
        }
    }
}
