﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace MCSM.VHE
{
    public class Entity
    {
        public const string SolidArrayName = "Solids";
        public const string OriginName = "origin";
        public static int SolidCounter { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public List<RangeSolid> RangeSolids { get; set; } = new List<RangeSolid>();

        public class Parameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public Type ValueType { get; set; }

            public Parameter() { }

            public Parameter(string name)
            {
                Name = name;
                ValueType = Type.Undefined;
            }

            public Parameter(string name, object value, Type type)
            {
                Name = name;
                Value = value;
                ValueType = type;
            }

            public void SetValue(string value)
            {
                Value = DeserializeValue(value, ValueType);
            }

            public void SetType(string type)
            {
                ValueType = Types[type.ToUpper()];
            }

            public string SerializeValue()
            {
                return Entity.SerializeValue(Value, ValueType);
            }

            public void SerializeSolidArray(MemoryStream ms)
            {
                Entity.SerializeSolidArray(ms, Value as List<Solid>);
            }

            public Parameter Copy()
            {
                var par = new Parameter(Name);
                par.Value = Value;
                par.ValueType = ValueType;
                return par;
            }
        }

        public class RangeSolid
        {
            public float[] Range { get; set; }
            public string Texture { get; set; }

            public RangeSolid(float[] range, string texture)
            {
                Range = range;
                Texture = texture;
            }

            public RangeSolid Copy()
            {
                var rng = new RangeSolid(new float[Range.Length], Texture);
                Range.CopyTo(rng.Range, 0);
                return rng;
            }

            public float[] GetMapRange(int offx, int offy, int offz)
            {
                var rng = new float[Range.Length];
                Range.CopyTo(rng, 0);

                //normalize
                for (int i = 0; i < 3; i++)
                {
                    Check(ref rng[i], ref rng[i + 3]);
                }

                return rng;
            }

            private void Check(ref float v0, ref float v1)
            {
                if (v0 <= v1)
                {
                    return;
                }

                var buf = v0;
                v0 = v1;
                v1 = buf;
            }
        }

        public enum Type
        {
            Undefined,
            Float,
            Int,
            IntArray,
            String,
            StringArray,
            Point,
            Point2D,
            SolidArray
        }

        public static Dictionary<string, Type> Types = new Dictionary<string, Type>() {
            {"FLOAT", Type.Float },
            {"INT", Type.Int },
            {"INTARRAY", Type.IntArray },
            {"STRING", Type.String },
            {"STRINGARRAY", Type.StringArray },
            {"POINT", Type.Point },
            {"POINT2D", Type.Point2D },
            {"SOLIDARRAY", Type.SolidArray }
        };

        public Entity() { }

        public Entity(string className)
        {
            ClassName = className;
        }

        public Entity(string name, string className)
        {
            Name = name;
            ClassName = className;
        }

        public Entity(EntityScript entityTemplate)
        {
            ClassName = entityTemplate.ClassName;
            
            foreach (var parTemp in entityTemplate.Parameters)
            {
                var par = new Parameter(parTemp.Name);
                par.SetType(parTemp.ValueType);
                par.SetValue(parTemp.Value);

                Parameters.Add(par);
            }
        }

        public void AddParameter(string name, object value, Type type)
        {
            Parameters.Add(new Parameter(name, value, type));
        }

        public void RemoveParameter(string name)
        {
            RemoveItemPar(Parameters, name);
        }

        public void AddSolidRange(List<Solid> solids)
        {
            solids.ForEach(s => AddSolid(s));
        }

        public void AddSolid(Solid solid)
        {
            var par = Parameters.Find(x => x.Name == SolidArrayName);
            if (par == null)
            {
                par = new Parameter(SolidArrayName, new List<Solid>(), Type.SolidArray);
                Parameters.Add(par);
            }

            (par.Value as List<Solid>).Add(solid);
        }

        public List<Solid> GetSolids()
        {
            var par = Parameters.Find(x => x.Name == SolidArrayName);
            if (par == null)
            {
                return null;
            }

            return par.Value as List<Solid>;
        }

        public Solid GetSolid(int index)
        {
            var slds = GetSolids();
            if (slds == null || index >= slds.Count)
            {
                return null;
            }

            return slds[index];
        }

        public static dynamic DeserializeValue(string data, Type type)
        {
            switch (type)
            {
                case Type.Float:
                    data = data.Replace('.', ',');
                    try
                    {
                        return Convert.ToSingle(data);
                    }
                    catch { }
                    return 0.0f;

                case Type.Int:
                    try
                    {
                        return Convert.ToInt32(data);
                    }
                    catch { }
                    return 0;

                case Type.IntArray:
                    var strInts = data.Split(' ');
                    var ints = new List<int>();
                    try
                    {
                        strInts.ToList().ForEach(xi => ints.Add(Convert.ToInt32(xi)));
                    }
                    catch { }
                    return ints.ToArray();

                case Type.String:
                    return data;

                case Type.Point:
                case Type.Point2D:
                    int digitCount = 2;
                    if (type == Type.Point)
                    {
                        digitCount++;
                    }

                    data = data.Replace('.', ',');
                    float x = 0, y = 0, z = 0;
                    int digit = 0;
                    bool active = false;
                    string buf = "";
                    for (int i = 0; i <= data.Length; i++)
                    {
                        char ch;
                        if (i < data.Length)
                        {
                            ch = data[i];
                        }
                        else
                        {
                            ch = ' ';
                        }

                        if (char.IsDigit(ch) || ch == ',' || ch == '-')
                        {
                            active = true;
                            buf += ch;
                        }
                        else
                        {
                            if (active)
                            {
                                active = false;
                                var val = Convert.ToSingle(buf);
                                buf = "";

                                switch (digit)
                                {
                                    case 0:
                                        x = val;
                                        break;

                                    case 1:
                                        y = val;
                                        break;

                                    case 2:
                                        z = val;
                                        break;
                                }

                                digit++;
                            }
                        }

                        if (digit == digitCount)
                        {
                            break;
                        }
                    }

                    if (type == Type.Point2D)
                    {
                        return new Point2D(x, y);
                    }
                    else
                    {
                        return new Point(x, y, z);
                    }
                    

                default:
                    throw new Exception("Undefined value type.");
            }
        }

        public static string SerializeValue(object value, Type valueType)
        {
            if (value == null)
            {
                return null;
            }

            string buf = "";

            switch (valueType)
            {
                case Type.Float:
                    return Map.Str(value);

                case Type.Int:
                case Type.String:
                    return value.ToString();

                case Type.IntArray:
                    foreach (var xi in (int[])value)
                    {
                        buf += xi.ToString() + " ";
                    }
                    buf.Trim();
                    return buf;

                case Type.StringArray:
                    foreach (var str in value as List<string>)
                    {
                        if (buf != "")
                        {
                            buf += ";";
                        }
                        buf += str;
                    }
                    return buf;

                case Type.Point:
                    var vect = value as Point;
                    return Map.Str(vect.X) + " " + Map.Str(vect.Y) + " " + Map.Str(vect.Z);

                case Type.Point2D:
                    var vect2D = value as Point2D;
                    return Map.Str(vect2D.X) + " " + Map.Str(vect2D.Y);

                case Type.SolidArray:
                    

                default:
                    throw new Exception("Undefined value type.");
            }
        }

        public static void SerializeSolidArray(MemoryStream ms, List<Solid> array)
        {
            bool begin = true;
            foreach (var sld in array)
            {
                string buf = "";
                if (begin)
                {
                    begin = false;
                    buf += Environment.NewLine;
                }

                buf += "{" + Environment.NewLine;
                foreach (var face in sld.Faces)
                {
                    string texture;
                    if (face.Texture == null)
                    {
                        texture = "NULL";
                    }
                    else
                    {
                        texture = face.Texture.ToUpper();
                    }

                    buf +=
                        "( " + Map.Str(face.Vertexes[0].X) + " " + Map.Str(face.Vertexes[0].Y) + " " + Map.Str(face.Vertexes[0].Z) + " ) " +
                        "( " + Map.Str(face.Vertexes[1].X) + " " + Map.Str(face.Vertexes[1].Y) + " " + Map.Str(face.Vertexes[1].Z) + " ) " +
                        "( " + Map.Str(face.Vertexes[2].X) + " " + Map.Str(face.Vertexes[2].Y) + " " + Map.Str(face.Vertexes[2].Z) + " ) " +
                        texture + " " +
                        "[ " + Map.Str(face.AxisU.X) + " " + Map.Str(face.AxisU.Y) + " " + Map.Str(face.AxisU.Z) + " " + Map.Str(face.OffsetU) + " ] " +
                        "[ " + Map.Str(face.AxisV.X) + " " + Map.Str(face.AxisV.Y) + " " + Map.Str(face.AxisV.Z) + " " + Map.Str(face.OffsetV) + " ] " +
                        Map.Str(face.Rotation) + " " + Map.Str(face.ScaleU) + " " + Map.Str(face.ScaleV) + " " + Environment.NewLine;
                }
                buf += "}";

                SolidCounter++;
                Map.MemoryWrite(ms, buf);
            }
        }

        public List<Parameter> CopyParameters()
        {
            var epars = new List<Parameter>();

            foreach (var par in Parameters)
            {
                object eval;
                switch (par.ValueType)
                {
                    case Type.IntArray:
                        var val = par.Value as int[];
                        eval = new int[val.Length];
                        val.CopyTo((int[])eval, 0);
                        break;

                    case Type.Point:
                        eval = new Point((Point)par.Value);
                        break;

                    case Type.Point2D:
                        eval = new Point2D((Point2D)par.Value);
                        break;

                    case Type.SolidArray:
                        eval = new List<Solid>((List<Solid>)par.Value);
                        break;

                    case Type.StringArray:
                        eval = new string[(par.Value as string[]).Length];
                        (par.Value as string[]).CopyTo((string[])eval, 0);
                        break;

                    default:
                        eval = par.Value;
                        break;
                }

                epars.Add(new Parameter(par.Name, eval, par.ValueType));
            }

            return epars;
        }

        public bool Compare(Entity entity, params string[] skipParams)
        {
            if (ClassName != entity.ClassName)
            {
                return false;
            }

            var pars = CopyParameters();
            var epars = entity.CopyParameters();

            foreach (var par in Parameters)
            {
                if (skipParams.Contains(par.Name))
                {
                    continue;
                }

                var epar = entity.Parameters.Find(x => x.Name == par.Name);
                if (epar == null)
                {
                    return false;
                }


                if (epar.ValueType != par.ValueType || epar.SerializeValue() != par.SerializeValue())
                {
                    return false;
                }

                RemoveItemPar(pars, par.Name);
                RemoveItemPar(epars, par.Name);
            }

            foreach (var skip in skipParams)
            {
                RemoveItemPar(pars, skip);
                RemoveItemPar(epars, skip);
            }

            return pars.Count == epars.Count;
        }

        public Entity Copy()
        {
            var entity = new Entity(Name, ClassName);
            foreach (var par in Parameters)
            {
                entity.Parameters.Add(par.Copy());
            }

            entity.RangeSolids = new List<RangeSolid>();
            RangeSolids.ForEach(r => entity.RangeSolids.Add(r.Copy()));

            return entity;
        }

        public static List<string> GetTypesList()
        {
            var parList = Enum.GetNames(typeof(Type)).ToList();
            parList.Remove(Enum.GetName(typeof(Type), Type.Undefined));
            parList.Remove(Enum.GetName(typeof(Type), Type.SolidArray));
            return parList;
        }

        /**/

        private void RemoveItemPar(List<Parameter> list, string parName)
        {
            list.RemoveAll(x => x.Name == parName);
        }
    }
}
