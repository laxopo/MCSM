using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class BlockDecsriptor : BlockIDs
    {
        public List<TextureKey> Textures { get; set; } = new List<TextureKey>();
        public string ModelClass { get; set; }
        public string ModelName { get; set; }
        public string Entity { get; set; }
        public int DataMask { get; set; }
        public int DataMax { get; set; }
        public int[] DataExceptions { get; set; }
        public bool IgnoreExcluded { get; set; }
        public bool TextureOriented { get; set; }
        public bool WorldOffset { get; set; }
        public bool GroupByData { get; set; }

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

        public BlockDecsriptor Copy()
        {
            var bt = new BlockDecsriptor();
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

        public string GetTextureName(int blockdata, string solidName = null, params string[] keys)
        {
            return GetTextureName(Textures, blockdata, solidName, keys);
        }

        /// <summary>
        /// data = -1 : ignore the data value
        /// </summary>
        /// <param name="bt"></param>
        /// <param name="blockdata"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// 
        public static string GetTextureName(List<TextureKey> textures, int blockdata, 
            string solidName, params string[] keys)
        {
            var name = TextureName(textures, blockdata, solidName, keys);
            return Macros.TextureName(name, blockdata);
        }

        public TextureKey GetSolidTK(string solidName)
        {
            return Textures.Find(t => ToUpper(t.SolidName) == ToUpper(solidName));
        }

        public BlockGroup.ModelType GetSolidType()
        {
            return BlockGroup.GetSolidType(ModelClass);
        }

        public List<string> GetTexureNamesList()
        {
            var list = new List<string>();
            var values = new List<string>();

            //get all tk values
            foreach (var tk in Textures)
            {
                values.Add(tk.Texture);
            }

            int max = DataMax;
            if (DataMax == 0)
            {
                max = 15;
            }

            //convert macroses
            foreach (var val in values)
            {
                //get all value variants 
                for (int d = 0; d <= max; d++)
                {
                    int dat = d;
                    if (DataMask > 0)
                    {
                        dat &= DataMask;
                    }

                    var macVal = Macros.TextureName(val, dat);
                    if (val == macVal) //no macros
                    {
                        list.Add(val);
                        break;
                    }

                    if (!list.Contains(macVal))
                    {
                        list.Add(macVal);
                    }
                }
            }

            return list;
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

        private static string TextureName(List<TextureKey> textures, int blockdata,
            string solidName, params string[] keys)
        {
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
                if (txt.SolidName != null && solidName != null && solidName != txt.SolidName)
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
                if (args[0] == macKey)
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
    }
}
