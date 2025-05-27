using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSM
{
    public class BlockDescriptor : BlockIDs
    {
        public int ReferenceID { get; set; }
        public int ReferenceData { get; set; }
        public string ModelClass { get; set; }
        public string ModelName { get; set; }
        public string Entity { get; set; }
        public List<string> SysEntities { get; set; } = new List<string>();
        public int DataOffset { get; set; }
        public int DataMask { get; set; }
        public int DataMax { get; set; }
        public int[] DataExceptions { get; set; }
        public bool IgnoreExcluded { get; set; }
        public bool TextureOriented { get; set; }
        public bool WorldOffset { get; set; }
        public GroupType Grouping { get; set; }
        public RotationType Rotation { get; set; }
        public string Align { get; set; }
        public List<TextureKey> Textures { get; set; } = new List<TextureKey>();

        /*public enum ThreeState
        {
            Auto,
            Enable,
            Disable
        }*/

        public enum GroupType
        {
            None = 2,
            DataXYZ = 0,
            DataXY = 1,
            DataZ = 3,
            XYZ,
            XY,
            Z
        }

        public enum RotationType
        {
            None,
            R4,
            R4L,
            R4Z,
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
            bt.ReferenceID = ReferenceID;
            bt.ReferenceData = ReferenceData;
            bt.ModelClass = ModelClass;
            bt.ModelName = ModelName;
            bt.Entity = Entity;
            bt.SysEntities = new List<string>(SysEntities);
            bt.DataOffset = DataOffset;
            bt.DataMask = DataMask;
            bt.Data = Data;
            bt.DataMax = DataMax;
            bt.Name = Name;
            bt.ID = ID;
            bt.IgnoreExcluded = IgnoreExcluded;
            bt.TextureOriented = TextureOriented;
            bt.Rotation = Rotation;
            bt.Grouping = Grouping;
            bt.WorldOffset = WorldOffset;
            bt.Align = Align;
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

        public static string[] GetGroupTypeList()
        {
            var list = Enum.GetNames(typeof(GroupType)).ToList().OrderBy(o => o).ToList();
            var none = list.Find(x => x == Enum.GetName(typeof(GroupType), GroupType.None));
            list.Remove(none);
            list.Insert(0, none);
            return list.ToArray();
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
            name = Macros.TextureName(name, bg);
            return name;
        }

        public TextureKey GetSolidTK(string solidName)
        {
            return Textures.Find(t => CompareString(t.SolidName, solidName));
        }

        public BlockGroup.ModelType GetSolidType()
        {
            return BlockGroup.GetSolidType(ModelClass);
        }

        public string GetTextureName(int blockdata, string solidName, params string[] keys)
        {
            return TextureName(Textures, blockdata, solidName, keys);
        }

        /**/

        private static string TextureName(List<TextureKey> textures, int blockdata,
            string solidName, params string[] keys)
        {
            if (solidName != null && (solidName == "" || solidName[0] == '_'))
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
                //check solid name
                if (!CompareString(solidName, txt.SolidName))
                {
                    continue;
                }

                //check key
                if (txt.Key == null)
                {
                    if (solidName == null)
                    {
                        if (key == null)
                        {
                            return txt.Texture;
                        }

                        continue;
                    }
                    else
                    {
                        if (CompareString(solidName, txt.SolidName))
                        {
                            return txt.Texture;
                        }

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

            if (str1 == null || str2 == null)
            {
                return false;
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
