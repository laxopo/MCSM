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
        public string ModelName { get; set; }
        public string Entity { get; set; }
        public int DataMask { get; set; }
        public int DataMax { get; set; }
        public bool IgnoreExcluded { get; set; }
        public int[] DataExceptions { get; set; }
        public bool TextureOriented { get; set; }
        public bool WorldOffset { get; set; }

        public class TextureKey
        {
            public string SolidName { get; set; }
            public string Key { get; set; }
            public string Texture { get; set; }

            public TextureKey Copy()
            {
                var tk = new TextureKey();
                tk.SolidName = SolidName;
                tk.Key = Key;
                tk.Texture = Texture;

                return tk;
            }
        }

        public BlockTexture Copy()
        {
            var bt = new BlockTexture();
            bt.Model = Model;
            bt.ModelName = ModelName;
            bt.Entity = Entity;
            bt.DataMask = DataMask;
            bt.Data = Data;
            bt.DataMax = DataMax;
            bt.Name = Name;
            bt.ID = ID;
            bt.IgnoreExcluded = IgnoreExcluded;
            bt.TextureOriented = TextureOriented;
            bt.Textures = new List<TextureKey>();

            if (DataExceptions != null)
            {
                bt.DataExceptions = new int[DataExceptions.Length];
                for (int i = 0; i < DataExceptions.Length; i++)
                {
                    bt.DataExceptions[i] = DataExceptions[i];
                }
            }
            
            Textures.ForEach(x => bt.Textures.Add(x.Copy()));

            return bt;
        }

        public void SetTextureName(string key, string textureName)
        {
            SetTextureName(null, key, textureName);
        }

        public void SetTextureName(string solidName, string key, string textureName)
        {
            var tk = Textures.Find(x => ToUpper(x.SolidName) == ToUpper(solidName) && 
                ToUpper(x.Key) == ToUpper(key));

            if (tk == null)
            {
                if(solidName == null)
                {
                    tk = Textures.Find(x => ToUpper(x.SolidName) == ToUpper(solidName) &&
                        x.Key == null);
                }
                else if (key == null)
                {
                    tk = Textures.Find(x => x.SolidName == null && ToUpper(x.Key) == ToUpper(key));
                }
            }

            if (tk == null)
            {
                tk = Textures.Find(x => x.SolidName == null && x.Key == null);
            }

            tk.Texture = textureName;
        }

        public string GetTextureName(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (key == null)
                {
                    return Textures[0].Texture;
                }

                var tk = Textures.Find(x => ToUpper(x.Key) == ToUpper(key));
                if (tk != null)
                {
                    return tk.Texture;
                }
            }

            return null;
        }

        /// <summary>
        /// data = -1 : ignore the data value
        /// </summary>
        /// <param name="bt"></param>
        /// <param name="data"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// 
        public static string GetTextureName(List<TextureKey> textures, int data, 
            string solidName = null, params string[] keys)
        {
            var mac = "";
            if (data > -1)
            {
                mac = "$d" + data;
            }

            string GetDataTexture(string key = null)
            {
                foreach (var txt in textures)
                {
                    if (solidName != null && solidName != txt.SolidName)
                    {
                        continue;
                    }

                    if (txt.Key == null)
                    {
                        if (key == null)
                        {
                            return txt.Texture;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var args = txt.Key.Split(' ');
                    if (args[0] == mac)
                    {
                        if (key != null && args.Length > 1)
                        {
                            if (args[1] == key)
                            {
                                return txt.Texture;
                            }
                        }
                        else
                        {
                            return txt.Texture;
                        }
                    }
                    else if (args[0] == key)
                    {
                        return txt.Texture;
                    }
                }

                return null;
            }

            if (keys.Length == 0)
            {
                if (data > 0)
                {
                    return GetDataTexture();
                }
                else
                {
                    return textures[0].Texture;
                }
            }

            foreach (var key in keys)
            {
                var txt = GetDataTexture(key);

                if (txt != null)
                {
                    return txt;
                }
            }

            return null;
        }

        public string GetTextureName(int data, string solidName = null, params string[] keys)
        {
            return GetTextureName(Textures, data, solidName, keys);
        }

        public BlockGroup.SolidType GetSolidType()
        {
            return BlockGroup.GetSolidType(Model);
        }

        /**/

        private string ToUpper(string text)
        {
            if (text == null)
            {
                return null;
            }

            return text.ToUpper();
        }
    }
}
