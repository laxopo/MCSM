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
        public List<string> WadFiles;
    }
}
