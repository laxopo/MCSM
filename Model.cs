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
        public List<Solid> Solids { get; set; } = new List<Solid>();
        public VHE.Point Position { get; set; }

        public class Solid
        {
            public string Name { get; set; }
            public VHE.Point Offset { get; set; } = new VHE.Point();
            public VHE.Point Size { get; set; } = new VHE.Point();
            public VHE.Point OriginAlign { get; set; } = new VHE.Point(1, 1, 1);
            public VHE.Point OriginRotOffset { get; set; } = new VHE.Point();
            public VHE.Point Rotation { get; set; } = new VHE.Point();
            public Faces[] TexturedFaces { get; set; }
            public bool TextureLockOffsets { get; set; }
            public bool TextureOriented { get; set; }
            public float TextureScale { get; set; }
            public List<Face> Faces { get; set; } = new List<Face>();
            public List<BlockTexture.TextureKey> Textures { get; set; } 
                = new List<BlockTexture.TextureKey>();

            public Solid()
            {
                for (int i = 0; i < 6; i++)
                {
                    Faces.Add(new Face() { Name = (Faces)i });
                }
            }

            public Face Face(int faceIndex)
            {
                return Face((Faces)faceIndex);
            }

            public Face Face(Faces face)
            {
                var fc = Faces.ToList().Find(x => x.Name == face);

                if (fc == null)
                {
                    var nfc = new Face(face);
                    Faces.Add(nfc);
                    return nfc;
                }

                return fc;
            }
        }

        public class Face
        {
            public Faces Name { get; set; } = Faces.Undefined;
            public float OffsetU { get; set; }
            public float OffsetV { get; set; }
            public float ScaleU { get; set; } = 1;
            public float ScaleV { get; set; } = 1;
            public float Rotation { get; set; }
            public bool StretchU { get; set; }
            public bool StretchV { get; set; }
            public bool MirrorU { get; set; }
            public bool MirrorV { get; set; }
            public bool Frame { get; set; }

            public Face() { }

            public Face(Faces name)
            {
                Name = name;
            }
        }

        public enum Faces
        {
            Top,
            Bottom,
            Left,
            Right,
            Rear,
            Front,
            Undefined
        }

        public Model() { }
    }
}
