using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM.VHE
{
    public class Face : ICloneable
    {
        public string Texture { get; set; }
        public Point AxisU { get; set; }
        public Point AxisV { get; set; }
        public float OffsetU { get; set; }
        public float OffsetV{ get; set; }
        public float ScaleU { get; set; } = 1;
        public float ScaleV { get; set; } = 1;
        public float Rotation { get; set; }
        public Point[] Vertexes { get; set; } = new Point[3];

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
