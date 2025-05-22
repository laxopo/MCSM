using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM
{
    public class BlockDescriptor : BlockIDs
    {
        public string ModelClass { get; set; }
        public string ModelName { get; set; }
        public string Entity { get; set; }
        public int DataMask { get; set; }
        public int DataMax { get; set; }
        public int[] DataExceptions { get; set; }
        public bool IgnoreExcluded { get; set; }
        public bool TextureOriented { get; set; }
        public bool WorldOffset { get; set; }
        public ThreeState Grouping { get; set; }
        public RotationType Rotation { get; set; }
        public List<TextureKey> Textures { get; set; } = new List<TextureKey>();

        public enum ThreeState
        {
            Auto,
            Enable,
            Disable
        }

        public enum RotationType
        {
            None,
            R4,
            R6,
            R8,
            R16
        }

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

        public BlockDescriptor Copy()
        {
            var bt = new BlockDescriptor();
            bt.ModelClass = ModelClass;
            bt.ModelName = ModelName;
            bt.Entity = Entity;
            bt.DataMask = DataMask;
            bt.Data = Data;
            bt.DataMax = DataMax;
            bt.Name = Name;
            bt.ID = ID;
            bt.IgnoreExcluded = IgnoreExcluded;
            bt.TextureOriented = TextureOriented;
            bt.Rotation = Rotation;
            bt.Grouping = Grouping;
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
            var tk = Textures.Find(x => CompareString(x.SolidName, solidName) &&
                CompareString(x.Key, key));

            if (tk == null)
            {
                if(solidName == null)
                {
                    tk = Textures.Find(x => CompareString(x.SolidName, solidName) && x.Key == null);
                }
                else if (key == null)
                {
                    tk = Textures.Find(x => x.SolidName == null && CompareString(x.Key, key));
                }
            }

            if (tk == null)
            {
                tk = Textures.Find(x => x.SolidName == null && x.Key == null);
            }

            tk.Texture = textureName;
        }

        public string GetTextureName(BlockGroup bg, string solidName = null, params string[] keys)
        {
            return GetTextureName(Textures, bg, solidName, keys);
        }

        /// <summary>
        /// data = -1 : ignore the data value
        /// </summary>
        /// <param name="bt"></param>
        /// <param name="blockdata"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// 
        public static string GetTextureName(List<TextureKey> textures, BlockGroup bg, 
            string solidName, params string[] keys)
        {
            var name = TextureName(textures, bg.Data, solidName, keys);
            return Macros.TextureName(name, bg);
        }

        public TextureKey GetSolidTK(string solidName)
        {
            return Textures.Find(t => CompareString(t.SolidName, solidName));
        }

        public BlockGroup.ModelType GetSolidType()
        {
            return BlockGroup.GetSolidType(ModelClass);
        }

        /**/

        private static string TextureName(List<TextureKey> textures, int blockdata,
            string solidName, params string[] keys)
        {
            if (solidName == "")
            {
                solidName = null;
            }

            var macKey = "";
            if (blockdata > -1)
            {
                macKey = "$d" + blockdata;
            }

            if (keys.Length == 0)
            {
                if (blockdata > 0)
                {
                    return GetDataTexture(textures, macKey, solidName);
                }
                else
                {
                    return textures[0].Texture;
                }
            }

            foreach (var key in keys)
            {
                var txt = GetDataTexture(textures, macKey, solidName, key);

                if (txt != null)
                {
                    return txt;
                }
            }

            return null;
        }

        private static string GetDataTexture(List<TextureKey> textures, string macKey, string solidName = null, string key = null)
        {
            foreach (var txt in textures)
            {
                if (txt.SolidName != null && solidName != null && !CompareString(solidName, txt.SolidName))
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
                if (CompareString(args[0], macKey))
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
                else if (CompareString(args[0], key))
                {
                    return txt.Texture;
                }
            }

            return null;
        }

        private static bool CompareString(string str1, string str2)
        {
            if (str1 == null && str2 == null)
            {
                return true;
            }

            return ToUpper(str1) == ToUpper(str2);
        }

        private static string ToUpper(string str)
        {
            if (str == null || str == "")
            {
                return null;
            }

            return str.ToUpper();
        }
    }
}
