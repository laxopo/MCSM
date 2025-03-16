using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class Model
    {
        public string Name { get; set; }
        public double ScaleX { get; set; }//in minecraft blocks, at cs coordinates
        public double ScaleY { get; set; }//
        public double ScaleZ { get; set; }//
        public List<VHE.Map.Solid> Solids { get; set; } = new List<VHE.Map.Solid>();

        public Model() { }

        /*public Model(string name, double length, double width, double height, VHE.Map model)
        {
            Name = name;
            ScaleX = length;
            ScaleY = width;
            ScaleZ = height;
            Solids = model.Solids;
        }*/
    }
}
