using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv.VHE
{
    public class Map
    {
        public string ClassName { get; set; } = "worldspawn";
        public int MaxRange { get; set; } = 4096;
        public List<string> Textures { get; set; } = new List<string>();
        public List<Solid> Solids { get; set; } = new List<Solid>();
        public List<Object> Objects { get; set; } = new List<Object>();

        public class Solid
        {
            public string Name { get; set; }
            public List<Face> Faces { get; set; } = new List<Face>();
        }

        public Map() { }

        public Map(string[] data)
        {
            int block = -1;
            string header = "";
            bool sldBlock = false;
            Solid solid = null;

            foreach (var row in data)
            {
                if (row[0] == '{')
                {
                    if (block >= 0)
                    {
                        sldBlock = true;
                        solid = new Solid();
                    }

                    block++;
                    continue;
                }

                if (row[0] == '}')
                {
                    if (sldBlock)
                    {
                        sldBlock = false;
                        Solids.Add(solid);
                    }
                }

                if (block == 0)
                {
                    header += row;
                }

                if (block == 1 && header != "")
                {
                    ClassName = GetParamether(header, "classname");
                    MaxRange = Convert.ToInt32(GetParamether(header, "MaxRange"));
                    if (Convert.ToInt32(GetParamether(header, "mapversion")) != 220)
                    {
                        throw new Exception("Unsupported map version");
                    }

                    Textures = GetParamether(header, "wad").Split(';').ToList();
                    header = "";
                }

                if (sldBlock)
                {
                    var face = new Face();
                    int index = 0;

                    var vertexes = GetValuesRow(row, ref index, 3, "()");
                    for (int v = 0; v < 3; v++)
                    {
                        face.Vertexes[v] = new Vector(vertexes[v]);
                    }

                    //texture name
                    char ch;
                    bool txtblk = false;
                    string texture = "";
                    index = row.IndexOf(' ', index);
                    do
                    {
                        ch = row[index++];
                        if (ch != ' ' && ch != ')' && ch != '[')
                        {
                            txtblk = true;
                            texture += ch;
                        }
                        else if (txtblk)
                        {
                            index--;
                            break;
                        }    
                    }
                    while (true);

                    if (texture != "null")
                    {
                        face.Texture = texture;
                    }

                    var uvVectors = GetValuesRow(row, ref index, 2, "[]");
                    face.AxisU = new Vector(uvVectors[0]);
                    face.AxisV = new Vector(uvVectors[1]);
                    face.OffsetU = uvVectors[0][3];
                    face.OffsetV = uvVectors[1][3];

                    var tFormat = GetValuesRow(row, ref index);
                    face.Rotation = tFormat[0][0];
                    face.ScaleU = tFormat[0][1];
                    face.ScaleV = tFormat[0][2];

                    solid.Faces.Add(face);
                }
            }
        }

        private float[][] GetValuesRow(string row, ref int index, int count = 1, string range = null)
        {
            var blocks = new List<float[]>();

            for (int v = 0; v < count; v++)
            {
                if (range == null)
                {
                    blocks.Add(GetValuesBlock(row.Substring(index, row.Length - index - 1)));
                }
                else
                {
                    index = row.IndexOf(range[0], index) + 1;
                    int end = row.IndexOf(range[1], index);
                    blocks.Add(GetValuesBlock(row.Substring(index, end - index)));
                    index = end;
                }
            }

            return blocks.ToArray();
        }

        private float[] GetValuesBlock(string block)
        {
            var list = new List<float>();

            bool valInit = false;
            string val = "";

            for (int i = 0; i < block.Length; i++)
            {
                var ch = block[i];
                bool valid = char.IsDigit(ch) || ch == '.' || ch == '-';

                if (valid)
                {
                    if (!valInit)
                    {
                        valInit = true;
                        val = ch.ToString();
                    }
                    else
                    {
                        val += ch;
                    }
                }

                if (!valid || i == block.Length - 1)
                {
                    if (valInit)
                    {
                        valInit = false;
                        list.Add(Convert.ToSingle(val));
                    }
                }
            }

            return list.ToArray();
        }

        private string GetParamether(string data, string paramether)
        {
            int idx = data.IndexOf("\"" + paramether + "\"");
            if (idx == -1)
            {
                return null;
            }

            idx = data.IndexOf("\"", idx + paramether.Length + 2) + 1;
            int end = data.IndexOf("\"", idx);

            return data.Substring(idx, end - idx);
        }

        public static string Str(object value)
        {
            var buf = value.ToString();
            buf = buf.Replace(',', '.');
            return buf;
        }

        public string Serialize()
        {
            List<string> data = new List<string>();

            data.Add("{");
            data.Add("\"classname\" \"" + ClassName + "\"");
            data.Add("\"MaxRange\" \"" + MaxRange + "\"");
            data.Add("\"mapversion\" \"220\"");
            
            var wadStr = "\"wad\" \"";
            bool init = false;
            foreach (var wad in Textures)
            {
                if (init)
                {
                    wadStr += "; ";
                }

                wadStr += wad;
                init = true;
            }
            data.Add(wadStr+ "\"");

            foreach(var solid in Solids)
            {
                data.Add("{");
                foreach (var face in solid.Faces)
                {
                    string texture;
                    if (face.Texture == null)
                    {
                        texture = "null";
                    }
                    else
                    {
                        texture = face.Texture;
                    }

                    string row =
                        "( " + Str(face.Vertexes[0].X) + " " + Str(face.Vertexes[0].Y) + " " + Str(face.Vertexes[0].Z) + " ) " +
                        "( " + Str(face.Vertexes[1].X) + " " + Str(face.Vertexes[1].Y) + " " + Str(face.Vertexes[1].Z) + " ) " +
                        "( " + Str(face.Vertexes[2].X) + " " + Str(face.Vertexes[2].Y) + " " + Str(face.Vertexes[2].Z) + " ) " +
                        texture + " " +
                        "[ " + Str(face.AxisU.X) + " " + Str(face.AxisU.Y) + " " + Str(face.AxisU.Z) + " " + Str(face.OffsetU) + " ] " +
                        "[ " + Str(face.AxisV.X) + " " + Str(face.AxisV.Y) + " " + Str(face.AxisV.Z) + " " + Str(face.OffsetV) + " ] " +
                        Str(face.Rotation) + " " + Str(face.ScaleU) + " " + Str(face.ScaleV) + " ";

                    data.Add(row);
                }
                data.Add("}");
            }

            data.Add("}");

            //objects
            foreach (var obj in Objects)
            {
                data.Add("{");
                data.Add("\"classname\" \"" + obj.ClassName + "\"");

                foreach (var par in obj.Parameters)
                {
                    data.Add("\"" + par.Name + "\" \"" + par.SerializeValue() + "\"");
                }

                data.Add("}");
            }

            data.Add("");

            return string.Join(Environment.NewLine, data);
        }
    }
}
