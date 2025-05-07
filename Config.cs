using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace MCSMapConv
{
    public class Config
    {
        public float BlockScale { get; set; }
        public int TextureResolution { get; set; }
        public string CstrikePath { get; set; }
        public string MapOutputPath { get; set; }
        public string WorldPath { get; set; }
        public List<VHE.Point> WorldRangeConv { get; set; } = new List<VHE.Point>();
        public List<VHE.Point> WorldRangeBI { get; set; } = new List<VHE.Point>();
        public int WorldDimensionConv { get; set; }
        public int WorldDimensionBI { get; set; }
        public string NBTList { get; set; }
        public string NBTTag { get; set; }
        public string BlockIDName { get; set; }
        public int BlockID { get; set; }
        public List<string> WadFiles { get; set; } = new List<string>();
    }
}
