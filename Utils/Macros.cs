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

        public static string EntityValue(string rawValue, BlockGroup bg, BlockDescriptor bt = null)
        {
            return Parse(rawValue, bg, true, bt);
        }

        public static string Parse(string value, BlockGroup bg, bool entity, BlockDescriptor bt = null)
        {
            if (value == null || bg == null)
            {
                return value;
            }

            value = value.Replace('.', ',');

            string data, newValue = value;
            int index = 0;

            //get {data} blocks
            //{or 33 {if $d>=3&&$d<=5:64:128}}
            while ((data = GetBlock(value, ref index)).Length > 0)
            {
                var args = ArgSplit(data, ' ');
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

                case "OR":
                case "AND":
                case "NOT":
                    res = ParseLogic(args, bg, entity, bt);
                    break;

                case "ADDI":
                case "SUBI":
                case "MULI":
                case "DIVI":
                case "ADD":
                case "SUB":
                case "MUL":
                case "DIV":
                    res = ParseArithmetic(args, bg, entity, bt);
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
                case "OR":
                case "AND":
                case "NOT":
                case "ADDI":
                case "SUBI":
                case "MULI":
                case "DIVI":
                    return ParseTypes.Int;

                case "NBT":
                case "TEX":
                    return ParseTypes.String;

                case "ANG16":
                case "ANG4":
                case "X":
                case "Y":
                case "Z":
                case "ADD":
                case "SUB":
                case "MUL":
                case "DIV":
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

        /*System routine*/

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
            //{or 33 {if $d>=3&&$d<=5:64:128}}
            int lvl = -1;
            string block = "";
            bool spec = false;

            for (; startIndex < data.Length; startIndex++)
            {
                var ch = data[startIndex];

                if (ch == '&')
                {
                    spec = true;
                }

                if (lvl == -1)
                {
                    if (ch == '{' && !spec)
                    {
                        lvl++;
                    }
                }
                else
                {
                    if (ch == '{' && !spec)
                    {
                        lvl++;
                    }

                    if (ch == '}')
                    {
                        if (lvl > 0)
                        {
                            lvl--;
                        }
                        else
                        {
                            startIndex++;
                            break;
                        }
                    }

                    block += ch;
                }

                if (ch != '&')
                {
                    spec = false;
                }

                if (startIndex == data.Length - 1 && lvl > -1)
                {
                    block = "";
                }
            }

            return block;
        }

        private static string[] ArgSplit(string data, char separator)
        {
            //or 33 {if $d>=3&&$d<=5:64:128}
            var args = new List<string>();
            string arg = "";
            int lvl = 0;
            bool spec = false;

            for (int i = 0; i < data.Length; i++)
            {
                var ch = data[i];

                if (ch == '$')
                {
                    spec = true;
                }

                if (ch == '{' && !spec)
                {
                    lvl++;
                }

                if (ch == '}')
                {
                    lvl--;
                    if (lvl < 0)
                    {
                        //error
                    }
                }

                if (ch == separator && lvl == 0)
                {
                    args.Add(arg.Trim());
                    arg = "";
                }
                else
                {
                    arg += ch;
                }

                if (i == data.Length - 1)
                {
                    if (lvl > 0)
                    {
                        //error
                    }

                    args.Add(arg.Trim());
                }

                if (ch != '$')
                {
                    spec = false;
                }
            }

            return args.ToArray();
        }

        /*Macros parse*/

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

            res = Parse(res, bg, entity, bt);

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

        private static string[] ParseValues(string[] args, BlockGroup bg, bool entity, BlockDescriptor bt)
        {
            var values = new List<string>();

            for (int i = 1; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg[0])
                {
                    case '$':
                        values.Add(Decode(new string[] { arg.Remove(0, 1) }, bg, entity, bt));
                        break;

                    case '{':
                        values.Add(Parse(arg, bg, entity, bt));
                        break;

                    default:
                        values.Add(arg);
                        break;
                }
            }

            return values.ToArray();
        }

        private static string ParseLogic(string[] args, BlockGroup bg, bool entity, BlockDescriptor bt)
        {
            var values = ParseValues(args, bg, entity, bt).ToList();
            int res = 0;

            try
            {
                res = Convert.ToInt32(values[0]);
                values.RemoveAt(0);

                switch (args[0].ToUpper())
                {
                    case "OR":
                        values.ForEach(x => res |= Convert.ToInt32(x));
                        break;

                    case "AND":
                        values.ForEach(x => res &= Convert.ToInt32(x));
                        break;

                    case "NOT":
                        values.ForEach(x => res ^= Convert.ToInt32(x));
                        break;
                }
            }
            catch 
            {
                //error
            }

            return res.ToString();
        }

        private static string ParseArithmetic(string[] args, BlockGroup bg, bool entity, BlockDescriptor bt)
        {
            var values = ParseValues(args, bg, entity, bt).ToList();
            double res = 0;
            bool toInt = false;
            var mac = args[0].ToUpper();
            if (mac.Length == 4 && mac[3] == 'I')
            {
                toInt = true;
                mac.Remove(3, 1);
            }

            try
            {
                res = Convert.ToDouble(values[0]);
                values.RemoveAt(0);

                switch (args[0].ToUpper())
                {
                    case "ADD":
                        values.ForEach(x => res += Convert.ToDouble(x));
                        break;

                    case "SUB":
                        values.ForEach(x => res -= Convert.ToDouble(x));
                        break;

                    case "MUL":
                        values.ForEach(x => res *= Convert.ToDouble(x));
                        break;

                    case "DIV":
                        values.ForEach(x => res /= Convert.ToDouble(x));
                        break;
                }
            }
            catch 
            { 
                //error
            }

            if (toInt)
            {
                return Convert.ToInt32(res).ToString();
            }
            else
            {
                return res.ToString();
            }
        }
    }
}
