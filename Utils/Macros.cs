using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NamedBinaryTag;

namespace MCSM
{
    public static class Macros
    {
        private static float Scale { get; set; }

        /**/

        public static void Initialize(float csscale)
        {
            Scale = csscale;
        }

        public static string TextureName(string name, BlockGroup bg)
        {
            return Parse(name, bg, false);
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

        public static string EntityValue(string rawValue, BlockGroup bg)
        {
            return Parse(rawValue, bg, true);
        }

        public static string Parse(string value, BlockGroup bg, bool entity)
        {
            if (value == null || bg == null)
            {
                return value;
            }

            value = value.Replace('.', ',');
            string data, newValue = value;
            int index = 0;

            //get {data} blocks
            while ((data = GetBlock(value, ref index)) != null)
            {
                var args = data.Split(' ');
                string res = "";

                switch (args[0].ToUpper())
                {
                    case "D": //block data
                        res = bg.Data.ToString();
                        break;

                    case "NBT": //block nbt

                        if (bg.Block == null)
                        {
                            throw new Exception("The block is not specified");
                        }

                        if (args.Length < 3)
                        {
                            throw new Exception("Not enought arguments in macros");
                        }

                        if (bg.Block.Chunk == null)
                        {
                            break;
                        }

                        List<NBT> te = bg.Block.Chunk.NBTData.GetTag("Level/" + args[1]);
                        foreach (var e in te)
                        {
                            string id = e.GetTag("id");
                            int x = e.GetTag("x");
                            int y = e.GetTag("y");
                            int z = e.GetTag("z");

                            if (id.ToUpper() == bg.Block.Name.ToUpper() &&
                                x == bg.Block.X && y == bg.Block.Y && z == bg.Block.Z)
                            {
                                object par = e.GetTag(args[2]);
                                res = par.ToString();
                                break;
                            }
                        }
                        break;

                    case "ANG16":
                        if (entity)
                        {
                            res = (-BlockDataParse.Rotation16(bg.Data) + 90).ToString();
                        }
                        else
                        {
                            res = BlockDataParse.Rotation16(bg.Data).ToString();
                        }
                        break;

                    case "ANG4":
                        if (entity)
                        {
                            res = (-BlockDataParse.Rotation4L(bg.Data) + 90).ToString();
                        }
                        else
                        {
                            res = BlockDataParse.Rotation4L(bg.Data).ToString();
                        }
                        break;

                    case "X":
                        res = ParseCoordinate(bg.Xmin + 0.5, args);
                        break;

                    case "Y":
                        res = ParseCoordinate(-bg.Ymin - 0.5, args);
                        break;

                    case "Z":
                        res = ParseCoordinate(bg.Zmin + 0.5, args);
                        break;

                    default:
                        throw new Exception("Unknown macros: " + args[0]);
                }

                newValue = newValue.Replace("{" + data + "}", res);
            }

            return newValue;
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

        private static string GetBlock(string data, ref int startIndex)
        {
            int beg = -1, end = -1;

            for (; startIndex < data.Length; startIndex++)
            {
                var ch = data[startIndex];

                if (ch == '{')
                {
                    beg = startIndex;
                }
                else if (ch == '}' && beg != -1)
                {
                    end = startIndex++;
                }

                if (beg != -1 && end != -1)
                {
                    break;
                }
            }

            if (beg == -1 || end == -1)
            {
                return null;
            }

            return data.Substring(beg + 1, end - beg - 1);
        }

        private static string ParseCoordinate (double x, string[] args)
        {
            try
            {
                x += Convert.ToDouble(args[1]);
            }
            catch { }
            return (x * Scale).ToString();
        }
    }
}
