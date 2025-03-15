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
                switch (ValueType)
                {
                    case Type.Float:
                        return Map.Str(Value);

                    case Type.Int:
                    case Type.String:
                        return Value.ToString();

                    case Type.Vector:
                        var vect = Value as Vector;
                        return Map.Str(vect.X) + " " + Map.Str(vect.Y) + " " + Map.Str(vect.Z);

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
            Vector,
            Solid,
            SolidArray
        }

        public static Dictionary<string, Type> Types = new Dictionary<string, Type>() {
            {"Float", Type.Float },
            {"Int", Type.Int },
            {"String", Type.String },
            {"Vector", Type.Vector }
        };
    }
}
