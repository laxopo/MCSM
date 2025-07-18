﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NamedBinaryTag;

namespace MCSM
{
    public class ModelScript
    {
        public string Name { get; set; }
        public string Origin { get; set; }
        public string Rotation { get; set; }
        public string Offset { get; set; }
        public List<Solid> Solids { get; set; } = new List<Solid>();
        public List<BlockDescriptor.TextureKey> TextureKeys { get; set; }

        public class Solid
        {
            //string
            public string Name { get; set; }
            public string Entity { get; set; }
            public List<string> IncludedSolids { get; set; } = new List<string>();

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
            public bool TextureLockRotanion { get; set; }
            public bool TextureOriented { get; set; }
            public bool SolidOrigin { get; set; }

            //Face sides
            public string[] TexturedFaces { get; set; }

            //Faces
            public List<Face> Faces { get; set; }

            public Face Face(string faceName)
            {
                if (Faces == null)
                {
                    Faces = new List<Face>();
                }

                var fc = Faces.ToList().Find(x => x.Name == faceName);

                if (fc == null)
                {
                    var nfc = new Face() { Name = faceName };
                    Faces.Add(nfc);
                    return nfc;
                }

                return fc;
            }
        }

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
            public bool ReverseU { get; set; }
            public bool ReverseV { get; set; }
            public bool Frame { get; set; }
            public bool LockOrigin { get; set; }
        }

        public enum Type
        {
            Point,
            Point2D,
            Float,
            FaceList,
        }

        [JsonIgnore]
        public static Dictionary<Type, VHE.Entity.Type> Types = new Dictionary<Type, VHE.Entity.Type>()
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

        public Model ToModelDefault(BlockDescriptor bt)
        {
            return ToModel(bt, null, null, null, true);
        }

        public Model ToModel(BlockDescriptor bt = null, BlockGroup bg = null, 
            World world = null, Block block = null, bool forceDefault = false)
        {
            var model = new Model();
            model.Name = Name;
            model.Origin = Parse(Origin, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault);
            model.Rotation = Parse(Rotation, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault);
            model.Offset = Parse(Offset, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault);
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

                            OffsetU = Parse(fc.OffsetU, Type.Float, 0, bt, bg, world, block, forceDefault),
                            OffsetV = Parse(fc.OffsetV, Type.Float, 0, bt, bg, world, block, forceDefault),
                            ScaleU = Parse(fc.ScaleU, Type.Float, 1, bt, bg, world, block, forceDefault),
                            ScaleV = Parse(fc.ScaleV, Type.Float, 1, bt, bg, world, block, forceDefault),
                            Rotation = Parse(fc.Rotation, Type.Float, 0, bt, bg, world, block, forceDefault),

                            Origin = Parse(fc.Origin, Type.Point2D, Defaults.P2DZero, bt, bg, world, block, forceDefault),

                            Frame = fc.Frame,
                            ReverseV = fc.ReverseV,
                            ReverseU = fc.ReverseU,
                            StretchU = fc.StretchU,
                            StretchV = fc.StretchV,
                            UnscaledOffset = fc.UnscaledOffset,
                            LockOrigin = fc.LockOrigin
                        };

                        faces.Add(face);
                    }
                }

                var solid = new Model.Solid()
                {
                    Name = sld.Name,

                    AbsOffset = Parse(sld.AbsOffset, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault),
                    Offset = Parse(sld.Offset, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault),
                    OriginAlign = Parse(sld.OriginAlign, Type.Point, Defaults.POne, bt, bg, world, block, forceDefault),
                    OriginRotOffset = Parse(sld.OriginRotOffset, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault),
                    Rotation = Parse(sld.Rotation, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault),
                    Size = Parse(sld.Size, Type.Point, Defaults.PZero, bt, bg, world, block, forceDefault),
                    IncludedSolids = new List<string>(sld.IncludedSolids),

                    TextureScale = Parse(sld.TextureScale, Type.Float, 1, bt, bg, world, block, forceDefault),

                    TextureLockOffsets = sld.TextureLockOffsets,
                    TextureLockRotanion = sld.TextureLockRotanion,
                    TextureOriented = sld.TextureOriented,
                    SolidOrigin = sld.SolidOrigin,

                    Entity = sld.Entity,
                    
                    Faces = faces
                };
                

                solid.TexturedFaces = Parse(sld.TexturedFaces, Type.FaceList, null, bt, bg, world, block, forceDefault);

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
                Offset = ToScript(model.Offset, Defaults.PZero),
                Rotation = ToScript(model.Rotation, Defaults.PZero)
            };

            if (model.TextureKeys != null && model.TextureKeys.Count > 0)
            {
                mds.TextureKeys = model.TextureKeys;
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
                        TextureLockRotanion = solid.TextureLockRotanion,
                        TextureOriented = solid.TextureOriented,
                        Entity = solid.Entity,
                        SolidOrigin = solid.SolidOrigin,
                        IncludedSolids = new List<string>(solid.IncludedSolids)
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
                        var list = new List<Face>();
                        foreach (var face in faces)
                        {
                            var defFcs = new Face();

                            var fcs = new Face()
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
                                ReverseV = face.ReverseV,
                                ReverseU = face.ReverseU,
                                StretchU = face.StretchU,
                                StretchV = face.StretchV,
                                UnscaledOffset = face.UnscaledOffset,
                                LockOrigin = face.LockOrigin
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
                            sls.Faces = list;
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

        public static dynamic Parse(object value, Type type, object defvalue, 
            BlockDescriptor bt, BlockGroup bg, World world, Block block, bool forceDefault)
        {
            if (value == null)
            {
                switch (type)
                {
                    case Type.Float:
                        return defvalue;

                    case Type.Point:
                        return (defvalue as VHE.Point).Copy();

                    case Type.Point2D:
                        return (defvalue as VHE.Point2D).Copy();

                    case Type.FaceList:
                        return null;

                    default:
                        throw new Exception("Unknown type");
                }
            }

            switch (type)
            {
                case Type.Float:
                case Type.Point:
                case Type.Point2D:
                    var data = Macros.Parse(value.ToString(), bg, false, bt, world, block, forceDefault);
                    return VHE.Entity.DeserializeValue(data, Types[type]);

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
