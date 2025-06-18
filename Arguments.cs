using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace MCSM
{
    public class Arguments
    {
        public string WorldPath { get; set; }
        public string MapOutputPath { get; set; }
        public Programs Program { get; set; } = Programs.Converter;
        public int[] Range { get; set; } //dim, xyz_min, xyz_max
        public string NBTList { get; set; }
        public string NBTTag { get; set; }
        public string BlockIDName { get; set; }
        public int BlockID { get; set; }
        public bool SkyBoxEnable { get; set; }
        public int Replace { get; set; } = -1;

        public enum Programs
        {
            Converter,
            BlockInspect,
            Help
        }

        public Arguments() { }

        public Arguments(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var arg = ArgCommands.TryGetValue(args[i], out var value) ? value : Args.Unknown;

                switch (arg)
                {
                    default:
                        throw GetException(ref Exceptions.InvalidArgument, args[i]);

                    case Args.WorldPath:
                        WorldPath = GetString(args, i, "\"".ToArray());
                        if (!Directory.Exists(WorldPath))
                        {
                            throw GetException(ref Exceptions.PathNotFound, WorldPath, args[i]);
                        }
                        i++;
                        break;

                    case Args.MapOutputPath:
                        MapOutputPath = GetString(args, i, "\"".ToArray());
                        i++;
                        break;

                    case Args.BlockInspect:
                        Program = Programs.BlockInspect;
                        try
                        {
                            BlockID = GetInt(args, i + 1);
                            i++;
                        }
                        catch { }
                        break;

                    case Args.Range:
                        Range = GetRange(args, ref i);
                        break;

                    case Args.NBT:
                        var valuesNBT = GetNBTArgs(args, ref i);
                        NBTList = valuesNBT[0];
                        NBTTag = valuesNBT[1];
                        BlockIDName = valuesNBT[2];
                        break;

                    case Args.Help:
                        Help();
                        return;

                    case Args.SkyBox:
                        SkyBoxEnable = true;
                        break;

                    case Args.Replace:
                        Replace = GetInt(args, i + 1);
                        i++;
                        break;
                }
            }

            if (WorldPath == null)
            {
                throw GetException(ref Exceptions.NotEnoughArgs, GetArgCommand(Args.WorldPath));
            }

            switch (Program)
            {
                case Programs.Converter:
                    CheckValues(MapOutputPath, Range);
                    break;

                case Programs.BlockInspect:
                    CheckValues(Range);
                    if (BlockID == 0 && NBTTag == null)
                    {
                        throw GetException(ref Exceptions.NotEnoughArgs, "block ID or NBT arguments");
                    }
                    break;
            }
        }

        public enum Args
        {
            Unknown,
            WorldPath,
            MapOutputPath,
            BlockInspect,
            Range,
            NBT,
            Help,
            SkyBox,
            Replace
        }

        public static Dictionary<string, Args> ArgCommands = new Dictionary<string, Args>()
        {
            { "-w", Args.WorldPath },
            { "-m", Args.MapOutputPath },
            { "-bi", Args.BlockInspect },
            { "-r", Args.Range },
            { "-nbt", Args.NBT },
            { "-help", Args.Help },
            { "-sky",  Args.SkyBox},
            { "-rep",  Args.Replace}
        };

        public static string GetArgCommand(Args arg)
        {
            return ArgCommands.FirstOrDefault(x => x.Value == arg).Key;
        }

        public string CommandLine()
        {
            string com = "";

            switch (Program)
            {
                case Programs.Converter:
                    com += GetArgCommand(Args.WorldPath) + " \"" + WorldPath + "\" ";
                    com += GetArgCommand(Args.MapOutputPath) + " \"" + MapOutputPath + "\" ";
                    com += GetArgCommand(Args.Range) + " " + RangeSerialize();
                    if (SkyBoxEnable)
                    {
                        com += " " + GetArgCommand(Args.SkyBox);
                    }
                    if (Replace > -1)
                    {
                        com += " " + GetArgCommand(Args.Replace) + " " + Replace;
                    }

                    break;

                case Programs.BlockInspect:

                    com += GetArgCommand(Args.WorldPath) + " \"" + WorldPath + "\" ";
                    com += GetArgCommand(Args.Range) + " " + RangeSerialize() + " ";
                    com += GetArgCommand(Args.BlockInspect) + " " + BlockID + " ";

                    if (NBTTag == null)
                    {
                        break;
                    }

                    com += GetArgCommand(Args.NBT) + " " + BlockID + " ";

                    if (NBTList != null)
                    {
                        com += NBTList + " ";
                    }

                    com += NBTTag + " " + BlockIDName;
                    break;
            }

            return com.Trim();
        }

        /**/

        private void Help()
        {
            Program = Programs.Help;
            HelpData.ToList().ForEach(x => Console.WriteLine(x));
            Console.ReadLine();
        }

        private static string[] HelpData = new string[]
        {
            "Common arguments:",
            GetArgCommand(Args.WorldPath) + "\t: minecraft world path (root directory)",
            GetArgCommand(Args.MapOutputPath) + "\t: output VHE map path",
            GetArgCommand(Args.Range) + " [ dim [x y z] [x y z] ] : minecraft world range:",
            "\tdim : dimension = -1, 0, 1; ",
            "\txyz : range of world (2 points)",
            GetArgCommand(Args.NBT) + " [ list tag IDName ] : NBT arguments for reading specified tag:",
            "\tlist   : listTag in the NBT structure (can be skipped)",
            "\ttag    : tag name in the list",
            "\tIDName : block ID name (minecraft:blockName)",
            GetArgCommand(Args.Replace) + "id\t: replace non-registered blocks with spec. id (only for the converter)",
            "",
            "Optional flags:",
            GetArgCommand(Args.SkyBox) + "\t: enable skybox generation (only for the converter)",
            "",
            "Programs: (devault = converter)",
            "[ " + GetArgCommand(Args.WorldPath) + " " + GetArgCommand(Args.MapOutputPath) + " " 
                + GetArgCommand(Args.Range) + "] : Converter (default)",
            "[ " + GetArgCommand(Args.WorldPath) + " " + GetArgCommand(Args.Range) + " ] " + 
                GetArgCommand(Args.BlockInspect) + " [ [id] or " + GetArgCommand(Args.NBT) + "] : Block Inspect",
            "\tid : block id number. Set -1 for id scan. Can be skipped is " + GetArgCommand(Args.NBT) + " is specified"
        };

        private string GetString(string[] args, int index, char[] trim = null)
        {
            CheckIndex(args, index);

            if (trim == null)
            {
                return args[index + 1].Trim();
            }

            return args[index + 1].Trim(trim);
        }

        private int GetInt(string[] args, int index, string arg = null)
        {
            try
            {
                return Convert.ToInt32(args[index]);
            }
            catch
            {
                throw GetException(ref Exceptions.InvalidData, args[index]);
            }
        }

        private int[] GetRange(string[] args, ref int index)
        {
            CheckIndex(args, index, args[index]);
            var arg = args[index];

            if (args.Length - (index + 8) < 0)
            {
                throw GetException(ref Exceptions.NotEnoughArgVal, args[index]);
            }

            var range = new int[7];
            index++;
            for (int i = 0; i < 7; i++)
            {
                range[i] = GetInt(args, index, arg);
                index++;
            }
            index--;

            if (range[0] > 1 || range[0] < -1)
            {
                throw GetException(ref Exceptions.InvalidData, range[0].ToString() + "(must be -1 ... 1)");
            }

            for (int i = 1; i < 4; i++)
            {
                if (range[i] > range[i + 3])
                {
                    IntSwap(ref range[i], ref range[i + 3]);
                }
            }

            return range;
        }

        private string[] GetNBTArgs(string[] args, ref int index)
        {
            CheckIndex(args, index);

            if (args.Length - (index + 2) < 0)
            {
                throw GetException(ref Exceptions.NotEnoughArgVal, args[index]);
            }

            List<string> values = new List<string>();
            index++;
            for (int i = 0; i < 3; i++)
            {
                if (index >= args.Length || ArgCommands.ContainsKey(args[index]))
                {
                    break;
                }

                values.Add(args[index]);
                index++;
            }
            index--;

            if (values.Count < 3)
            {
                values.Insert(0, "");
            }

            return values.ToArray();
        }

        private string RangeSerialize()
        {
            string serial = "";
            foreach (var val in Range)
            {
                serial += val.ToString() + " ";
            }

            return serial.Trim();
        }

        private void IntSwap(ref int i1, ref int i2)
        {
            var buf_i1 = i1;
            i1 = i2;
            i2 = buf_i1;
        }

        private void CheckIndex(string[] args, int index, string arg = null)
        {
            if (index + 1 >= args.Length)
            {
                if (arg == null)
                {
                    throw GetException(ref Exceptions.ValueNotSpecified, args[index]);
                }
                else
                {
                    throw GetException(ref Exceptions.ValueNotSpecified, arg);
                }
            }
        }

        private void CheckValues(params object[] values)
        {
            foreach (var val in values)
            {
                if (val == null)
                {
                    var arg = GetArgCommand((Args)Enum.Parse(typeof(Args), val.GetType().Name));
                    throw GetException(ref Exceptions.NotEnoughArgs, arg);
                }
            }
        }

        private string ParseTextArguments(string text, string[] args)
        {
            int beg = 0, end = 0;

            while ((beg = text.IndexOf('{', end)) != -1)
            {
                end = text.IndexOf('}');
                if (end == -1)
                {
                    break;
                }

                var textIdx = text.Substring(beg + 1, end - beg - 1);
                var idx = Convert.ToInt32(textIdx);
                text.Replace(textIdx, args[idx]);
                beg += args[idx].Length;
            }

            return text;
        }

        /*Exceptions*/

        public static class Exceptions
        {
            public static Exception InvalidArgument = new Exception("Invalid argument: ");
            public static Exception ValueNotSpecified = new Exception("Argument value not specified. Argument: ");
            public static Exception PathNotFound = new Exception("Path not found \"{0}\". Argument: {1}");
            public static Exception InvalidData = new Exception("Invalid value data: ");
            public static Exception InvalidFormat = new Exception("Invalid data format. Argument: ");
            public static Exception NotEnoughArgVal = new Exception("Not enough values count for the argument: ");
            public static Exception NotEnoughArgs = new Exception("Not enough arguments. Expected: ");
        }

        private Exception GetException(ref Exception exception, params string[] textArgs)
        {
            var mainText = exception.Message;

            switch (textArgs.Length)
            {
                case 0:
                    exception = new Exception(mainText);
                    break;

                case 1:
                    exception = new Exception(mainText + textArgs[0]);
                    break;

                default:
                    mainText = ParseTextArguments(mainText, textArgs);
                    exception = new Exception(mainText);
                    break;
            }

            return exception;
        }
    }
}
