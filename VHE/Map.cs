using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MCSM.VHE
{
    public class Map
    {
        public List<Entity> Data { get; set; } = new List<Entity>();

        public Map() 
        {
            var header = new Entity("worldspawn");
            header.AddParameter("mapversion", 220, Entity.Type.Int);
            header.AddParameter("MaxRange", 4096, Entity.Type.Int);
            header.AddParameter("sounds", 1, Entity.Type.Int);
            header.AddParameter("wad", new List<string>(), Entity.Type.StringArray);
            header.AddParameter("Solids", new List<Solid>(), Entity.Type.SolidArray);
            Data.Add(header);
        }
        public static string Str(object value)
        {
            string buf;

            if (value is double || value is float)
            {
                var val = (float)value;

                if(float.IsNaN(val) || float.IsInfinity(val))
                {
                    throw new Exception("Invalid float value");
                }

                buf = val.ToString("0.########");
            }
            else
            {
                buf = value.ToString();
            }

            buf = buf.Replace(',', '.');
            return buf;
        }

        public string Serialize()
        {
            int i = 0, s = 0;
            return Serialize(ref i, ref s);
        }

        public string Serialize(ref int e, ref int s)
        {
            List<string> data = new List<string>();

            for (e = 0; e < Data.Count; e++)
            {
                var obj = Data[e];

                data.Add("{");
                data.Add("\"classname\" \"" + obj.ClassName + "\"");

                foreach (var par in obj.Parameters)
                {
                    if (par.ValueType == Entity.Type.SolidArray)
                    {
                        data.Add(par.SerializeValue(ref s));
                    }
                    else
                    {
                        data.Add("\"" + par.Name + "\" \"" + par.SerializeValue(ref s) + "\"");
                    }
                }

                data.Add("}");
            }

            data.Add("");

            return string.Join(Environment.NewLine, data);
        }

        public void SetParameter(string className, string paramName, object value)
        {
            GetParameter(className, paramName).Value = value;
        }

        public Entity.Parameter GetParameter(string className, string paramName)
        {
            var obj = Data.Find(x => x.ClassName == className);
            if (obj == null)
            {
                throw new Exception("Object not found.");
            }

            var param = obj.Parameters.Find(x => x.Name == paramName);
            if (param == null)
            {
                throw new Exception("Parameter not found.");
            }

            return param;
        }

        public void AddSolids(string className, List<Solid> solids)
        {
            solids.ForEach(x => AddSolid(className, x));
        }

        public void AddSolids(List<Solid> solids)
        {
            solids.ForEach(x => AddSolid(x));
        }

        public void AddSolid(Solid solid)
        {
            AddSolid("worldspawn", solid);
        }

        public void AddSolid(string className, Solid solid)
        {
            var param = GetParameter(className, "Solids");
            (param.Value as List<Solid>).Add(solid);
        }

        public void AddStrings(string className, string paramName, List<string> values)
        {
            values.ForEach(val => AddString(className, paramName, val));
        }

        public void AddString(string className, string paramName, string value)
        {
            var param = GetParameter(className, paramName);

            (param.Value as List<string>).Add(value);
        }

        public Stat GetSolidsCount()
        {
            var stat = new Stat();

            foreach (var obj in Data)
            {
                var solids = obj.Parameters.Find(x => x.Name == "Solids");

                if (solids == null)
                {
                    continue;
                }

                stat.Solids += (solids.Value as List<Solid>).Count;
                (solids.Value as List<Solid>).ForEach(x => stat.Faces += x.Faces.Count);
            }

            return stat;
        }

        public void CreateEntity(EntityScript entityTemplate)
        {
            Data.Add(new Entity(entityTemplate));
        }

        public void CreateEntity(Entity entity)
        {
            Data.Add(entity);
        }

        /**/

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
    }
}
