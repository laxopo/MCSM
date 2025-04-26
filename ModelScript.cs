using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MCSMapConv
{
    public class ModelScript
    {
        public string Name { get; set; }
        public string Origin { get; set; }
        public string Rotation { get; set; }
        public List<Solid> Solids { get; set; } = new List<Solid>();
        public BlockDescriptor.TextureKey[] TextureKeys { get; set; }

        public class Solid
        {
            //string
            public string Name { get; set; }

            //point
            public string AbsOffset { get; set; }
            public string Offset { get; set; }
            public string Size { get; set; } = "1 1 1";
            public string OriginAlign { get; set; } = "1 1 1";
            public string OriginRotOffset { get; set; }
            public string Rotation { get; set; }

            //float
            public string TextureScale { get; set; }

            //bool
            public bool TextureLockOffsets { get; set; }
            public bool TextureOriented { get; set; }
            
            //Face sides
            public string[] TexturedFaces { get; set; }

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
                
                //Point
                public string Origin { get; set; }

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
            Point2D,
            Float,
            FaceList,
        }

        [JsonIgnore]
        public Dictionary<Type, VHE.Entity.Type> Types = new Dictionary<Type, VHE.Entity.Type>()
        {
            {Type.Point, VHE.Entity.Type.Point },
            {Type.Point2D, VHE.Entity.Type.Point2D },
            {Type.Float, VHE.Entity.Type.Float },
        };

        private static class Defaults
        {
            public static VHE.Point POne = new VHE.Point(1, 1, 1);
            public static VHE.Point PZero = new VHE.Point();
            public static VHE.Point2D P2DZero = new VHE.Point2D();
        }

        /**/

        public Model ToModel()
        {
            var model = new Model();
            model.Name = Name;
            model.Origin = Parse(Origin, Type.Point, Defaults.PZero);
            model.Rotation = Parse(Rotation, Type.Point, Defaults.PZero);
            model.Solids = new List<Model.Solid>();

            if (TextureKeys != null)
            {
                model.TextureKeys = TextureKeys.ToList();
            }

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

                            Origin = Parse(fc.Origin, Type.Point2D, Defaults.P2DZero),

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

                    AbsOffset = Parse(sld.AbsOffset, Type.Point, Defaults.PZero),
                    Offset = Parse(sld.Offset, Type.Point, Defaults.PZero),
                    OriginAlign = Parse(sld.OriginAlign, Type.Point, Defaults.POne),
                    OriginRotOffset = Parse(sld.OriginRotOffset, Type.Point, Defaults.PZero),
                    Rotation = Parse(sld.Rotation, Type.Point, Defaults.PZero),
                    Size = Parse(sld.Size, Type.Point, Defaults.PZero),

                    TextureScale = Parse(sld.TextureScale, Type.Float, 1),

                    TextureLockOffsets = sld.TextureLockOffsets,
                    TextureOriented = sld.TextureOriented,
                    
                    Faces = faces
                };
                

                solid.TexturedFaces = Parse(sld.TexturedFaces, Type.FaceList, null);

                model.Solids.Add(solid);
            }

            return model;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { 
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }

        public static ModelScript FromModel(Model model)
        {
            var mds = new ModelScript()
            {
                Name = ToScript(model.Name),
                Origin = ToScript(model.Origin, Defaults.PZero),
                Rotation = ToScript(model.Rotation, Defaults.PZero)
            };

            if (model.TextureKeys != null && model.TextureKeys.Count > 0)
            {
                mds.TextureKeys = model.TextureKeys.ToArray();
            }

            if (model.Solids != null)
            {
                foreach (var solid in model.Solids)
                {
                    var sls = new Solid()
                    {
                        AbsOffset = ToScript(solid.AbsOffset, Defaults.PZero),
                        Name = solid.Name,
                        Offset = ToScript(solid.Offset, Defaults.PZero),
                        OriginAlign = ToScript(solid.OriginAlign, Defaults.POne),
                        OriginRotOffset = ToScript(solid.OriginRotOffset, Defaults.PZero),
                        Rotation = ToScript(solid.Rotation, Defaults.PZero),
                        Size = ToScript(solid.Size, Defaults.PZero),
                        TextureScale = ToScript(solid.TextureScale, 0, 1),
                        TextureLockOffsets = solid.TextureLockOffsets,
                        TextureOriented = solid.TextureOriented,
                    };

                    if (solid.TexturedFaces != null && solid.TexturedFaces.Length > 0)
                    {
                        var list = new List<string>();
                        foreach (var face in solid.TexturedFaces)
                        {
                            if (face == Model.Faces.Undefined)
                            {
                                continue;
                            }

                            list.Add(face.ToString());
                        }

                        sls.TexturedFaces = list.ToArray();
                    }

                    var faces = solid.GetFaces();
                    if (faces != null)
                    {
                        var list = new List<Solid.Face>();
                        foreach (var face in faces)
                        {
                            var defFcs = new Solid.Face();

                            var fcs = new Solid.Face()
                            {
                                Name = face.Name.ToString(),
                                OffsetU = ToScript(face.OffsetU, 0),
                                OffsetV = ToScript(face.OffsetV, 0),
                                ScaleU = ToScript(face.ScaleU, 0, 1),
                                ScaleV = ToScript(face.ScaleV, 0, 1),
                                Rotation = ToScript(face.Rotation, 0),
                                Origin = ToScript(face.Origin, Defaults.P2DZero),
                                Texture = face.Texture,
                                Frame = face.Frame,
                                MirrorU = face.MirrorU,
                                MirrorV = face.MirrorV,
                                StretchU = face.StretchU,
                                StretchV = face.StretchV,
                                UnscaledOffset = face.UnscaledOffset
                            };

                            foreach (PropertyInfo fcsProp in fcs.GetType().GetProperties())
                            {
                                if (fcsProp.Name == "Name")
                                {
                                    continue;
                                }

                                var defProp = defFcs.GetType().GetProperty(fcsProp.Name);
                                if (defProp == null)
                                {
                                    continue;
                                }

                                var val = fcsProp.GetValue(fcs, null);
                                var def = defProp.GetValue(defFcs, null);

                                if (val != def && !Compare(val, def))
                                {
                                    list.Add(fcs);
                                    break;
                                }
                            }
                        }

                        if (list.Count > 0)
                        {
                            sls.Faces = list.ToArray();
                        }
                    }

                    mds.Solids.Add(sls);
                }
            }

            return mds;
        }

        public static ModelScript FromJson(string data)
        {
            return JsonConvert.DeserializeObject<ModelScript>(data);
        }

        public ModelScript Copy()
        {
            return FromJson(Serialize());
        }

        /**/

        private static bool Compare(object obj1, object obj2)
        {
            if (obj1 is bool)
            {
                return (bool)obj1 == (bool)obj2;
            }
            if (obj1 is float)
            {
                return (float)obj1 == (float)obj2;
            }
            if (obj1 is string)
            {
                return (string)obj1 == (string)obj2;
            }

            throw new Exception("Unsupported type");
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
                case Type.Point2D:
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

        private static string ToScript(object value, params object[] defvalues)
        {
            if (value == null)
            {
                return null;
            }    

            if (value is VHE.Point || value is VHE.Point2D)
            {
                foreach (var defvalue in defvalues)
                {
                    if (value is VHE.Point2D)
                    {
                        var val1 = value as VHE.Point2D;
                        var val2 = defvalue as VHE.Point2D;
                        if (val1.X == val2.X && val1.Y == val2.Y)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        var val1 = value as VHE.Point;
                        var val2 = defvalue as VHE.Point;
                        if (val1.X == val2.X && val1.Y == val2.Y && val1.Z == val2.Z)
                        {
                            return null;
                        }
                    }
                }

                if (value is VHE.Point2D)
                {
                    return VHE.Entity.SerializeValue(value, VHE.Entity.Type.Point2D);
                }
                else
                {
                    return VHE.Entity.SerializeValue(value, VHE.Entity.Type.Point);
                }
            }
            else if (value is float || value is double)
            {
                foreach (var defvalue in defvalues)
                {
                    if (Convert.ToSingle(value) == Convert.ToSingle(defvalue))
                    {
                        return null;
                    }
                }

                return VHE.Entity.SerializeValue(value, VHE.Entity.Type.Float);
            }
            else if (value is string)
            {
                var val = value as string;
                foreach (var defvalue in defvalues)
                {
                    if (val == defvalue as string || val == "")
                    {
                        return null;
                    }
                }

                return val;
            }
            else
            {
                throw new Exception("Unsupported value type");
            }
        }
    }
}
