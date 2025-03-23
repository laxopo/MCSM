using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class BlockTexture : BlockIDs
    {
        public List<TextureKey> Textures { get; set; } = new List<TextureKey>();
        public string Model { get; set; }
        public string Entity { get; set; }
        public int DataMask { get; set; }
        public int DataMax { get; set; }
        public bool IgnoreExcluded { get; set; }

        public class TextureKey
        {
            public string Key { get; set; }
            public string Texture { get; set; }
        }

        public string GetTextureName(string key)
        {
            if (key == null)
            {
                return Textures[0].Texture;
            }

            var tk = Textures.Find(x => x.Key.ToUpper() == key.ToUpper());
            if (tk != null)
            {
                return tk.Texture;
            }

            return null;
        }

        public Solid.SolidType GetSolidType()
        {
            return Solid.GetSolidType(Model);
        }
    }
}
