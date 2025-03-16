using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv.VHE
{
    public class Object
    {
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
            }

            public Parameter(string name, object value, Type type)
            {
                Name = name;
                Value = value;
                ValueType = type;
            }

            public void SetValue(string value)
            {
                switch (ValueType)
                {
                    case Type.Float:
                        Value = Convert.ToSingle(value);
                        break;

                    case Type.Int:
                        Value = Convert.ToInt32(value);
                        break;

                    case Type.String:
                        Value = value;
                        break;

                    case Type.Vector:
                        float x = 0, y = 0, z = 0;
                        int digit = 0;
                        bool active = false;
                        string buf = "";
                        for (int i = 0; i <= value.Length; i++)
                        {
                            char ch;
                            if (i < value.Length)
                            {
                                ch = value[i];
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

                            if (digit > 2)
                            {
                                break;
                            }
                        }

                        Value = new Vector(x, y, z);
                        break;
                }
            }

            public void SetType(string type)
            {
                ValueType = Types[type];
            }

            public string SerializeValue()
            {
                string buf = "";

                switch (ValueType)
                {
                    case Type.Float:
                        return Map.Str(Value);

                    case Type.Int:
                    case Type.String:
                        return Value.ToString();

                    case Type.StringArray:
                        foreach (var str in Value as List<string>)
                        {
                            if (buf != "")
                            {
                                buf += ";";
                            }
                            buf += str;
                        }
                        return buf;

                    case Type.Vector:
                        var vect = Value as Vector;
                        return Map.Str(vect.X) + " " + Map.Str(vect.Y) + " " + Map.Str(vect.Z);

                    case Type.SolidArray:
                        foreach (var sld in Value as List<Map.Solid>)
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

        public enum Type
        {
            Undefined,
            Float,
            Int,
            String,
            StringArray,
            Vector,
            SolidArray
        }

        public static Dictionary<string, Type> Types = new Dictionary<string, Type>() {
            {"Float", Type.Float },
            {"Int", Type.Int },
            {"String", Type.String },
            {"Vector", Type.Vector }
        };

        public Object(string className)
        {
            ClassName = className;
        }

        public void AddParameter(string name, object value, Type type)
        {
            Parameters.Add(new Parameter(name, value, type));
        }
    }
}
