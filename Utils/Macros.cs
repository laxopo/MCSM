using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public static class Macros
    {
        private static float Scale { get; set; }

        /**/

        public static void Initialize(float csscale)
        {
            Scale = csscale;
        }

        public static string TextureName(string name, int blockData)
        {
            if (name == null || blockData == -1)
            {
                return name;
            }

            int end = name.IndexOf('}');
            if (end == -1)
            {
                return name;
            }

            int beg = LastIndexOf(name, "{", end);
            if (beg == -1)
            {
                return name;
            }

            var mac = name.Substring(beg + 1, end - beg - 1);
            string res;

            switch (mac)
            {
                case "d":
                    res = blockData.ToString();
                    break;

                default:
                    throw new Exception("Unknown texture name macros: " + mac);
            }

            return name.Replace("{" + mac + "}", res);
        }

        public static EntityScript GetSignEntity(List<EntityScript> list, string[] signText)
        {
            if (signText[0].IndexOf("$") != 0)
            {
                return null;
            }

            List<string> args = new List<string>();
            foreach (var row in signText)
            {
                args.AddRange(row.Split(' '));
            }

            var macros = args[0].Trim('$');
            var es = list.Find(o => o.Macros == macros);

            if (es == null)
            {
                throw new Exception(macros, Exceptions.ESNotFound);
            }

            return es;
        }

        public static string EntityValue(string rawValue, int blockData, float x, float y, float z)
        {
            string value = rawValue;

            while (value.IndexOf("{") != -1)
            {
                int beg = value.IndexOf("{");
                int end = value.IndexOf("}", beg + 1);

                if (beg == -1 || end == -1)
                {
                    break;
                }

                List<string> args = new List<string>();
                var mac = value.Substring(beg + 1, end - beg - 1);

                int sep = mac.IndexOf(" ");
                if (sep == -1)
                {
                    args.Add(mac);
                }
                else
                {
                    int ab = 0;
                    while (sep != -1)
                    {
                        args.Add(mac.Substring(ab, sep - ab));
                        ab = sep + 1;
                        sep = mac.IndexOf(" ", ab);

                        if (sep == -1)
                        {
                            args.Add(mac.Substring(ab, mac.Length - ab));
                        }
                    }
                }

                value = value.Remove(beg, end - beg + 1);

                string res = "";
                switch (args[0].ToUpper())
                {
                    case "ANGLE":
                        int vali = -BlockDataParse.Rotation16(blockData) + 90;
                        res = vali.ToString();
                        break;

                    case "X":
                        double val = x + 0.5;
                        if (args.Count > 1)
                        {
                            val += Convert.ToDouble(args[1]);
                        }
                        res = (val * Scale).ToString();
                        break;

                    case "Y":
                        val = -y - 0.5;
                        if (args.Count > 1)
                        {
                            val += Convert.ToDouble(args[1]);
                        }
                        res = (val * Scale).ToString();
                        break;

                    case "Z":
                        val = z + 0.5;
                        if (args.Count > 1)
                        {
                            val += Convert.ToDouble(args[1]);
                        }
                        res = (val * Scale).ToString();
                        break;

                    default:
                        throw new Exception(mac, Exceptions.SubMacrosUndef);

                        /*if (block.ID == 0)
                        {

                            Message.Write("Undefined submacros \"{0}\" at {1} {2} {3}", mac, x, y, z);
                        }
                        else
                        {
                            Message.Write("Undefined submacros \"{0}\" at block {1} {2} {3}", mac,
                                block.X, block.Y, block.Z);
                        }
                        break;*/
                }

                value = value.Insert(beg, res);
            }

            return value;
        }

        /*Exceptions*/

        public static class Exceptions
        {
            public static Exception ESNotFound;
            public static Exception SubMacrosUndef = new Exception("Undefined submacros ");
        }

        /**/

        private static int LastIndexOf(string text, string sign, int endIndex)
        {
            int idx = 0, last = -1;

        cycle:
            idx = text.IndexOf(sign, idx);
            if (idx == -1 || idx >= endIndex)
            {
                return last;
            }
            else
            {
                last = idx++;
                goto cycle;
            }
        }
    }
}
