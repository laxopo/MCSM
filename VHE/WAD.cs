using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MCSMapConv.VHE
{
    public class WAD
    {
        public List<Texture> Textures { get; set; } = new List<Texture>();
        private int Count { get; set; }
        private int DataOffset { get; set; }

        public class Texture
        {
            public string Name { get; set; }
            public bool Transparent { get; set; }
            public bool Water { get; set; }
            public bool Animated { get; set; }
            public bool ToggledAnimate { get; set; }
            public bool RandomTiling { get; set; }
            public bool Sky { get; set; }
        }

        public WAD(string filepath)
        {
            var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            if (GetString(fs, 0x00, 4) != "WAD3")
            {
                throw new Exception("WAD: invalid file format");
            }

            Count = GetInt(fs);
            DataOffset = GetInt(fs);

            for (int i = 0; i < Count; i++)
            {
                var texture = new Texture();
                var name = GetString(fs, DataOffset + i * 32 + 16, 16);

                switch (name[0])
                {
                    case '{':
                        texture.Transparent = true;
                        break;

                    case '!':
                        texture.Water = true;
                        break;

                    case '+':
                        if (name.Length > 2 && name[1] == 'A')
                        {
                            texture.ToggledAnimate = true;
                        }
                        else
                        {
                            texture.Animated = true;
                        }
                        break;

                    case '-':
                        texture.RandomTiling = true;
                        break;

                    default:
                        if (name.Length > 3 && name.Substring(0, 3) == "sky")
                        {
                            texture.Sky = true;
                        }
                        break;
                }

                texture.Name = name;
                Textures.Add(texture);
            }

            fs.Close();
        }

        private string GetString(FileStream fs, int offset, int count)
        {
            fs.Position = offset;
            return GetString(fs, count);
        }

        private string GetString(FileStream fs, int count)
        {
            byte[] buf = new byte[count];
            fs.Read(buf, 0, count);
            return Encoding.UTF8.GetString(buf).Trim('\0');
        }

        private int GetInt(FileStream fs, int offset)
        {
            fs.Position = offset;
            return GetInt(fs);
        }

        private int GetInt(FileStream fs)
        {
            byte[] buf = new byte[4];
            fs.Read(buf, 0, 4);
            return BitConverter.ToInt32(buf, 0);
        }
    }
}
