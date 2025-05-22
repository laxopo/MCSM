using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM.VHE
{
    public class Entity
    {
        public const string SolidArrayName = "Solids";
        public string ClassName { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        public class Parameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public Type ValueType { get; set; }

            public Parameter (string name)
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
        }

        public enum Type
        {
            Undefined,
            Float,
            Int,
            String,
            StringArray,
            Point,
            Point2D,
            SolidArray
        }

        public static Dictionary<string, Type> Types = new Dictionary<string, Type>() {
            {"FLOAT", Type.Float },
            {"INT", Type.Int },
            {"STRING", Type.String },
            {"STRINGARRAY", Type.StringArray },
            {"POINT", Type.Point },
            {"POINT2D", Type.Point2D },
            {"SOLIDARRAY", Type.SolidArray }
        };

        public Entity(string className)
        {
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

        public void AddSolid(Map.Solid solid)
        {
            var par = Parameters.Find(x => x.Name == SolidArrayName);
            if (par == null)
            {
                par = new Parameter(SolidArrayName, new List<Map.Solid>(), Type.SolidArray);
                Parameters.Add(par);
            }

            (par.Value as List<Map.Solid>).Add(solid);
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
            string buf = "";

            switch (valueType)
            {
                case Type.Float:
                    return Map.Str(value);

                case Type.Int:
                case Type.String:
                    return value.ToString();

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
                    foreach (var sld in value as List<Map.Solid>)
                    {
                        if (buf != "")
                        {
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
                    }
                    return buf;

                default:
                    throw new Exception("Undefined value type.");
            }
        }
    }
}
