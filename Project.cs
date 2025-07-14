using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using NamedBinaryTag;

namespace MCSM
{
    public class Project
    {
        [JsonIgnore]
        public string WorldPath { get; set; }

        [JsonIgnore]
        public string WorldName { get; set; }

        public List<RangeMark> RangeMarks { get; set; } = new List<RangeMark>();
        public string ConverterRangeMark { get; set; }
        public string BIRangeMark { get; set; }
        public List<MapProps> MapProperties { get; set; } = new List<MapProps>();
        public string PropPreset { get; set; }

        public class RangeMark
        {
            public string Name { get; set; }
            public int[] Range { get; set; }
        }

        public class MapProps
        {
            [JsonIgnore]
            public List<BlockReplace> BlockReplaceList 
            { 
                get
                {
                    if (blockReplaceList == null)
                    {
                        BlockReplaceListUpdate();
                    }

                    return blockReplaceList;
                }
            }

            public string Name { get; set; }
            public string Title { get; set; }
            public string Skyname { get; set; }
            public string LightLevel { get; set; }
            public string WaveHeight { get; set; }
            public string MaxViewableDistance { get; set; } = "4096";
            public List<string> BlockReplacement { get; set; } = new List<string>(); //1:2 = 3:4
            public List<VHE.Entity> Entities { get; set; } = new List<VHE.Entity>();

            private List<BlockReplace> blockReplaceList { get; set; }

            public BlockReplace GetBlockReplace(int id, int data)
            {
                BlockReplace buf = null;

                foreach (var br in BlockReplaceList)
                {
                    if (br.IDFrom == id)
                    {
                        if (br.DataFrom == -1)
                        {
                            return br;
                        }

                        if (br.DataFrom == data)
                        {
                            buf = br;
                        }
                    }
                }

                return buf;
            }

            public BlockReplace GetBlockReplace(int index)
            {
                if (index >= BlockReplacement.Count || index < 0)
                {
                    return null;
                }

                return GetBlockReplace(BlockReplacement[index]);
            }

            public BlockReplace GetBlockReplace(string data)
            {
                var args = data.Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                var from = args[0].Split(':');
                var to = args[1].Split(':');
                var br = new BlockReplace() {
                    IDFrom = Convert.ToInt32(from[0]),
                    IDTo = Convert.ToInt32(to[0])
                };

                if (from.Length > 1)
                {
                    br.DataFrom = Convert.ToInt32(from[1]);
                }

                if (to.Length > 1)
                {
                    br.DataTo = Convert.ToInt32(to[1]);
                }

                return br;
            }

            public void AddBlockReplace(string from, string to)
            {
                BlockReplacement.Add(GetBRData(from, to));
            }

            public void SetBlockReplace(string from, string to, int index)
            {
                BlockReplacement[index] = GetBRData(from, to);
            }

            public void BlockReplaceListUpdate()
            {
                blockReplaceList = new List<BlockReplace>();
                BlockReplacement.ForEach(br => blockReplaceList.Add(GetBlockReplace(br)));
            }

            /**/

            private string GetBRData(string from, string to)
            {
                from = from.Trim();
                to = to.Trim();

                //check data
                var f = from.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                var t = to.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                if (f.Length == 0)
                {
                    throw new Exception("Not enough arguments");
                }

                try
                {
                    Convert.ToInt32(f[0]);
                    if (f.Length > 1)
                    {
                        Convert.ToInt32(f[1]);
                    }

                    if (t.Length > 0)
                    {
                        Convert.ToInt32(t[0]);
                    }

                    if (t.Length > 1)
                    {
                        Convert.ToInt32(t[1]);
                    }

                }
                catch
                {
                    throw new Exception("Bad data input");
                }

                //set data
                string data = f[0].Trim();
                if (f.Length > 1)
                {
                    data += ":" + f[1].Trim();
                }

                data += " = ";
                if (t.Length > 0)
                {
                    data += t[0].Trim();
                    if (t.Length > 1)
                    {
                        data += ":" + t[1].Trim();
                    }
                }
                else
                {
                    data += "0";
                }

                return data;
            }
        }

        public class BlockReplace
        {
            public int IDFrom { get; set; }
            public int DataFrom { get; set; } = -1;
            public int IDTo { get; set; }
            public int DataTo { get; set; } = -1;

            public string[] GetFTStrings()
            {
                var from = IDFrom.ToString();
                if (DataFrom >= 0)
                {
                    from += ":" + DataFrom;
                }

                var to = IDTo.ToString();
                if (DataTo >= 0)
                {
                    to += ":" + DataTo;
                }

                return new string[]
                {
                    from,
                    to
                };
            }
        }

        public static Project OpenFile(string worldPath, bool ro = false)
        {
            if (!Directory.Exists(worldPath))
            {
                if (ro)
                {
                    throw new Exception("World path does not extist");
                }

                return null;
            }

            var projPath = Path.Combine(worldPath, "mcsm.json");
            var levelPath = Path.Combine(worldPath, "level.dat");

            if (File.Exists(levelPath))
            {
                Project pr;
                if (File.Exists(projPath))
                {
                    pr = JsonConvert.DeserializeObject<Project>(File.ReadAllText(projPath));
                }
                else
                {
                    if (ro)
                    {
                        throw new Exception("Project file mcsm.json not found");
                    }

                    pr = new Project();
                }

                pr.WorldPath = worldPath;
                var level = new NBT(levelPath);
                pr.WorldName = level.GetTag("Data/LevelName");
                return pr;
            }
            else
            {
                throw new Exception("File level.dat not found");
            }
        }

        public void SaveToFile()
        {
            File.WriteAllText(Path.Combine(WorldPath, "mcsm.json"), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public MapProps GetMapProperties()
        {
            return GetMapProperties(PropPreset);
        }

        public MapProps GetMapProperties(string name)
        {
            return MapProperties.Find(x => x.Name == name);
        }
    }
}
