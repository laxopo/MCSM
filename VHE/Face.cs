using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv.VHE
{
    public class Face : ICloneable
    {
        public string Texture { get; set; }
        public Vector AxisU { get; set; }
        public Vector AxisV { get; set; }
        public float OffsetU { get; set; }
        public float OffsetV{ get; set; }
        public float ScaleU { get; set; }
        public float ScaleV { get; set; }
        public float Rotation { get; set; }
        public Vector[] Vertexes { get; set; } = new Vector[3];

        public object Clone()
        {
            return MemberwiseClone();
        }

        public Face Copy()
        {
            var face = new Face();
            face.Texture = Texture;
            face.AxisU = AxisU.Copy();
            face.AxisV = AxisV.Copy();
            face.OffsetU = OffsetU;
            face.OffsetV = OffsetV;
            face.ScaleU = ScaleU;
            face.ScaleV = ScaleV;
            face.Rotation = Rotation;

            for (int i = 0; i < 3; i++)
            {
                face.Vertexes[i] = Vertexes[i].Copy();
            }

            return face;
        }

        public void MirrorTexture()
        {
            AxisU.X *= -1;
            AxisU.Y *= -1;
        }
    }
}
