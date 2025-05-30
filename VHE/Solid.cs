using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM.VHE
{
    public class Solid
    {
        public string Name { get; set; }
        public string Entity { get; set; }
        public List<Face> Faces { get; set; } = new List<Face>();
        public bool HasOrigin { get; set; }
    }
}
