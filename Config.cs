using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace MCSM
{
    public class Config
    {
        public float BlockScale { get; set; }
        public int TextureResolution { get; set; }
        public string CstrikePath { get; set; }
        public string MapOutputPath { get; set; }
        public string SelectedWorldPath { get; set; }
        public List<string> WadFiles { get; set; } = new List<string>();
        public List<string> WorldPaths { get; set; } = new List<string>();
    }
}
