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

        public enum ParseTypes
        {
            Undefined,
            String,
            Int,
            Float
        }

        /**/

        public static void Initialize(float csscale)
        {
            Scale = csscale;
        }

        public static string TextureName(string name, BlockGroup bg)
        {
            return Parse(name, false, bg, false);
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

        public static string EntityValue(string rawValue, BlockGroup bg, BlockDescriptor bt = null)
        {
            return Parse(rawValue, true, bg, true, bt);
        }

        public static string Parse(string value, bool isFloat, BlockGroup bg, bool entity, BlockDescriptor bt = null)
        {
            if (value == null || bg == null)
            {
                return value;
            }

            if (isFloat)
            {
                value = value.Replace('.', ',');
            }
            
            string data, newValue = value;
            int index = 0;

            //get {data} blocks
            while ((data = GetBlock(value, ref index)) != null)
            {
                var args = data.Split(' ');
                string res = Decode(args, bg, entity, bt);
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

        private static string Decode(string[] args, BlockGroup bg, bool entity, BlockDescriptor bt = null)
        {
            string res = "";
            switch (args[0].ToUpper())
            {
                case "D": //block masked data
                    res = bg.Data.ToString();
                    break;

                case "DB": //block data
                    res = bg.Block.Data.ToString();
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

                case "SX":
                    res = (bg.Xmax - bg.Xmin).ToString();
                    break;

                case "SY":
                    res = (bg.Ymax - bg.Ymin).ToString();
                    break;

                case "SZ":
                    res = (bg.Zmax - bg.Zmin).ToString();
                    break;

                case "TEX":
                    res = ParseTex(args, bg, bt);
                    break;

                case "IF":
                    res = ParseIf(args, bg, entity, bt);
                    break;

                default:
                    throw new Exception("Unknown macros: " + args[0]);
            }

            return res;
        }

        private static ParseTypes GetParseType(string macros)
        {
            switch (macros.ToUpper())
            {
                case "D":
                case "DB":
                case "SX":
                case "SY":
                case "SZ":
                    return ParseTypes.Int;

                case "NBT":
                case "TEX":
                    return ParseTypes.String;

                case "ANG16":
                case "ANG4":
                case "X":
                case "Y":
                case "Z":
                    return ParseTypes.Float;

                case "IF":
                    return ParseTypes.Undefined;

                default:
                    throw new Exception("Unknown macros: " + macros);
            }
        }

        private static dynamic ConvertParseType(string macros, string value)
        {
            var ptype = GetParseType(macros);

            switch (ptype)
            {
                case ParseTypes.Float:
                    return Convert.ToSingle(value);

                case ParseTypes.Int:
                    return Convert.ToInt32(value);

                default:
                    return value;
            }
        }

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

        private static string ParseIf(string[] args, BlockGroup bg, bool entity, BlockDescriptor bt = null)
        {
            //$sx==2||$sy==2:T:F

            //$sx==2 || $sy==2 T       F
            //conds            valTrue valFalse

            //$sx  ==   2     || $sy  ==   2     T       F
            //cond cpop cpval op cond cpop cpval valTrue valFalse

            string res = "";

            for (int i = 1; i < args.Length; i++)
            {
                var arg = args[i];
                var cv = arg.Split(':');
                if (cv.Length < 2)
                {
                    //error
                    continue;
                }

                var condMain = cv[0];
                var valTrue = cv[1];
                string valFalse = "";
                if (cv.Length > 2)
                {
                    valFalse = cv[2];
                }

                var conds = ConditionSplit(condMain); //$sx==2 || $sy==2

                //calc condidions
                for (int c = 0; c < conds.Length; c += 2) //calc $sx==2 and $sy==2
                {
                    var cstr = conds[c];
                    var cc = cstr.Split(new string[] { "==", "!=", "<=", ">=", "<", ">" }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    if (cv.Length < 2)
                    {
                        //error
                        continue;
                    }

                    var cond = cc[0]; //$sx
                    var cpval = cc[1]; //2
                    var cpop = cstr.Replace(cond, "").Replace(cpval, ""); //==

                    if (cond[0] == '$')
                    {
                        var condArgs = cond.Trim('$').Split(' ');
                        var condVal = Decode(condArgs, bg, entity, bt);

                        bool condRes = false;
                        dynamic cdv = ConvertParseType(condArgs[0], condVal);
                        dynamic cpv = ConvertParseType(condArgs[0], cpval);

                        switch (cpop)
                        {
                            case "==":
                                condRes = cdv == cpv;
                                break;

                            case "!=":
                                condRes = cdv != cpv;
                                break;

                            case "<=":
                                condRes = cdv <= cpv;
                                break;

                            case ">=":
                                condRes = cdv >= cpv;
                                break;

                            case "<":
                                condRes = cdv < cpv;
                                break;

                            case ">":
                                condRes = cdv > cpv;
                                break;
                        }

                        if (condRes)
                        {
                            conds[c] = "t";
                        }
                        else
                        {
                            conds[c] = "f";
                        }
                    }
                    else
                    {
                        //error
                        continue;
                    }
                }

                //condition logic ops
                bool init = false;
                bool opf = false;
                bool cd1 = false, cd2;
                string logop = "";

                foreach (var cond in conds) //t || f
                {
                    if (opf)
                    {
                        logop = cond;
                    }
                    else
                    {
                        if (!init)
                        {
                            init = true;
                            cd1 = cond == "t";
                        }
                        else
                        {
                            cd2 = cond == "t";
                            
                            switch (logop)
                            {
                                case "||":
                                    cd1 |= cd2;
                                    break;

                                case "&&":
                                    cd1 &= cd2;
                                    break;
                            }
                        }
                    }

                    opf ^= true;
                }

                //result
                if (cd1)
                {
                    res = valTrue;
                }
                else
                {
                    res = valFalse;
                }
            }

            return res;
        }

        private static string[] ConditionSplit(string condMain)
        {
            var conds = new List<string>() { condMain };

            List<string> Splt(List<string> conditions, string sep)
            {
                var buf = new List<string>();

                for (int c = 0; c < conditions.Count; c++)
                {
                    var cond = conditions[c];

                    var cds = cond.Split(new string[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                    
                    for (int i = 0; i < cds.Length; i++)
                    {
                        buf.Add(cds[i]);
                        if (i != cds.Length - 1)
                        {
                            buf.Add(sep);
                        }
                    }
                }

                return buf;
            }

            conds = Splt(conds, "&&");
            conds = Splt(conds, "||");
            return conds.ToArray();
        }

        private static string ParseTex(string[] args, BlockGroup bg, BlockDescriptor bt)
        {
            var mcs = new string[]
            {
                "f", //face (array)
                "s", //solid
                "d" //read data
            };

            string GetEquValue(string arg)
            {
                var a = arg.Split('=');
                if (a.Length < 2)
                {
                    return null;
                }

                return a[1];
            }

            string solid = null;
            List<string> faces = new List<string>();
            int data = -1;

            foreach (var arg in args)
            {
                for (int i = 0; i < mcs.Length; i++)
                {
                    var mac = "$" + mcs[i].ToUpper();
                    if (arg.ToUpper().IndexOf(mac) != 0)
                    {
                        continue;
                    }

                    switch (i)
                    {
                        case 0:
                            faces.Add(GetEquValue(arg));
                            break;

                        case 1:
                            solid = GetEquValue(arg);
                            break;

                        case 2:
                            data = bg.Data;
                            break;
                    }
                }
            }

            return bt.GetTextureName(data, solid, faces.ToArray());
        }
    }
}
