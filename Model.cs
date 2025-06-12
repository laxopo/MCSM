using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM
{
    public class Model
    {
        public string Name { get; set; }
        public VHE.Point Origin { get; set; } = new VHE.Point();
        public VHE.Point Offset { get; set; } = new VHE.Point();
        public VHE.Point Rotation { get; set; } = new VHE.Point();
        public List<Solid> Solids { get; set; } = new List<Solid>();
        public List<BlockDescriptor.TextureKey> TextureKeys { get; set; }
                = new List<BlockDescriptor.TextureKey>();
        public VHE.Point Position { get; set; }
        public VHE.Point PositionBuf { get; set; } = new VHE.Point();

        public class Solid
        {
            public string Name { get; set; }
            public VHE.Point AbsOffset { get; set; } = new VHE.Point();
            public VHE.Point Offset { get; set; } = new VHE.Point();
            public VHE.Point Size { get; set; } = new VHE.Point(1, 1, 1);
            public VHE.Point OriginAlign { get; set; } = new VHE.Point(1, 1, 1);
            public VHE.Point OriginRotOffset { get; set; } = new VHE.Point();
            public VHE.Point Rotation { get; set; } = new VHE.Point();
            public List<string> IncludedSolids { get; set; } = new List<string>();
            public string Entity { get; set; }
            public Faces[] TexturedFaces { get; set; }
            public bool TextureLockOffsets { get; set; }
            public bool TextureLockRotanion { get; set; }
            public bool TextureOriented { get; set; }
            public bool SolidOrigin { get; set; }
            public float TextureScale { get; set; }
            public List<Face> Faces { private get; set; } = new List<Face>();


            public Solid Copy()
            {
                var solid = new Solid();

                solid.Name = Name;
                solid.AbsOffset = AbsOffset.Copy();
                solid.Offset = Offset.Copy();
                solid.Size = Size.Copy();
                solid.OriginAlign = OriginAlign.Copy();
                solid.OriginRotOffset = OriginRotOffset.Copy();
                solid.Rotation = Rotation.Copy();
                solid.TextureLockOffsets = TextureLockOffsets;
                solid.TextureLockRotanion = TextureLockRotanion;
                solid.TextureOriented = TextureOriented;
                solid.TextureScale = TextureScale;
                solid.Entity = Entity;
                solid.SolidOrigin = SolidOrigin;
                solid.IncludedSolids = new List<string>(IncludedSolids);

                if (TexturedFaces != null)
                {
                    solid.TexturedFaces = (Faces[])TexturedFaces.Clone();
                }
                
                Faces.ForEach(x => solid.Faces.Add(x.Copy()));

                return solid;
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

            public List<Face> GetFaces()
            {
                var list = new List<Face>();

                for (int i = 0; i < 6; i++)
                {
                    list.Add(Face(i));
                }

                return list;
            }
        }

        public class Face
        {
            public Faces Name { get; set; } = Faces.Undefined;
            public string Texture { get; set; }
            public float OffsetU { get; set; }
            public float OffsetV { get; set; }
            public bool UnscaledOffset { get; set; }
            public float ScaleU { get; set; } = 1;
            public float ScaleV { get; set; } = 1;
            public float Rotation { get; set; }
            public VHE.Point2D Origin { get; set; } = new VHE.Point2D();
            public bool StretchU { get; set; }
            public bool StretchV { get; set; }
            public bool ReverseU { get; set; }
            public bool ReverseV { get; set; }
            public bool Frame { get; set; }
            public bool LockOrigin { get; set; }

            public Face() { }

            public Face(Faces name)
            {
                Name = name;
            }

            public Face Copy()
            {
                var face = new Face();

                face.Name = Name;
                face.Texture = Texture;
                face.OffsetU = OffsetU;
                face.OffsetV = OffsetV;
                face.UnscaledOffset = UnscaledOffset;
                face.ScaleU = ScaleU;
                face.ScaleV = ScaleV;
                face.Origin = Origin.Copy();
                face.Rotation = Rotation;
                face.StretchU = StretchU;
                face.StretchV = StretchV;
                face.ReverseV = ReverseV;
                face.ReverseU = ReverseU;
                face.Frame = Frame;
                face.LockOrigin = LockOrigin;

                return face;
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

        public Model(Model source)
        {
            Name = source.Name;
            Origin = source.Origin.Copy();
            Offset = source.Offset.Copy();
            Rotation = source.Rotation.Copy();
            if (source.Position != null)
            {
                Position = source.Position.Copy();
            }
            source.Solids.ForEach(s => Solids.Add(s.Copy()));
            source.TextureKeys.ForEach(x => TextureKeys.Add(x.Copy()));
        }

        public static Faces GetFaceEnum(string name)
        {
            if (name == null)
            {
                return Faces.Undefined;
            }

            switch (name.ToUpper())
            {
                case "TOP":
                    return Faces.Top;

                case "BOTTOM":
                    return Faces.Bottom;

                case "LEFT":
                    return Faces.Left;

                case "RIGHT":
                    return Faces.Right;

                case "REAR":
                    return Faces.Rear;

                case "FRONT":
                    return Faces.Front;

                default:
                    throw new Exception("Invalid face name: " + name);
            }
        }

        public Solid GetSolid(string name)
        {
            return Solids.Find(x => x.Name == name);
        }
    }
}
