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

        public class RangeMark
        {
            public string Name { get; set; }
            public int[] Range { get; set; }
        }

        public static Project OpenFile(string worldPath)
        {
            if (!Directory.Exists(worldPath))
            {
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
    }
}
