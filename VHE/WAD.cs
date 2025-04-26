using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace MCSMapConv.VHE
{
    public class WAD
    {
        public string FilePath { get; set; }
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
            public int Height { get; set; }
            public int Width { get; set; }
            public BMP Data { get; set; }
            public byte Type { get; set; }

            public Bitmap GetTextureBitmap()
            {
                if (Data.MainBMP == null)
                {
                    Data.MainBMP = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);

                    //set palette
                    var pal = Data.MainBMP.Palette;
                    for (int i = 0; i < 256; i++)
                    {
                        var rgb = Data.Palette[i];
                        var color = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                        pal.Entries[i] = color;
                    }

                    Data.MainBMP.Palette = pal;

                    //set indexes
                    var data = Data.MainBMP.LockBits(new Rectangle(0, 0, Data.MainBMP.Width, Data.MainBMP.Height),
                        ImageLockMode.ReadWrite, Data.MainBMP.PixelFormat);

                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            SetPixel(data, x, y, Data.Main[x, y]);
                        }
                    }

                    Data.MainBMP.UnlockBits(data);
                }

                
                return Data.MainBMP;
            }

            private unsafe void SetPixel(BitmapData data, int x, int y, byte color)
            {
                byte* p = (byte*)data.Scan0.ToPointer();
                int offset = y * data.Stride + x;
                p[offset] = color;
            }
        }

        public class BMP
        {
            public byte[,] Main { get; set; }
            public Bitmap MainBMP { get; set; }
            public byte[,] Mip1 { get; set; }
            public byte[,] Mip2 { get; set; }
            public byte[,] Mip3 { get; set; }
            public RGB[] Palette { get; set; }
        }

        public class RGB
        {
            public int R { get; set; }
            public int G { get; set; }
            public int B { get; set; }

            public Color GetColor()
            {
                return Color.FromArgb(255, R, G, B);
            }
        }

        public WAD(string filepath)
        {
            FilePath = filepath;
            var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            if (GetString(fs, 0x00, 4) != "WAD3")
            {
                throw new Exception("Error: invalid file format " + filepath);
            }

            Count = GetInt(fs);
            DataOffset = GetInt(fs);

            for (int i = 0; i < Count; i++)
            {
                var texture = new Texture();
                var name = GetString(fs, DataOffset + i * 32 + 16, 16);

                int imgOffset = GetInt(fs, DataOffset + i * 32);
                texture.Type = (byte)GetInt(fs, DataOffset + i * 32 + 12);

                switch (texture.Type)
                {
                    case 0x42://qpic
                        texture.Width = GetInt(fs, imgOffset);
                        texture.Height = GetInt(fs, imgOffset + 4);
                        texture.Data = new BMP()
                        {
                            Main = ReadBMP(fs, imgOffset + 8, texture.Width, texture.Height),
                            Palette = ReadPalette(fs, (int)fs.Position + 2)
                        };
                        break;

                    case 0x43://miptex
                        texture.Width = GetInt(fs, imgOffset + 16);
                        texture.Height = GetInt(fs, imgOffset + 20);
                        var offm = GetInt(fs, imgOffset + 24) + imgOffset;
                        var offm1 = GetInt(fs, imgOffset + 28) + imgOffset;
                        var offm2 = GetInt(fs, imgOffset + 32) + imgOffset;
                        var offm3 = GetInt(fs, imgOffset + 36) + imgOffset;
                        texture.Data = new BMP() {
                            Main = ReadBMP(fs, offm, texture.Width, texture.Height),
                            Mip1 = ReadBMP(fs, offm1, texture.Width / 2, texture.Height / 2),
                            Mip2 = ReadBMP(fs, offm2, texture.Width / 4, texture.Height / 4),
                            Mip3 = ReadBMP(fs, offm3, texture.Width / 8, texture.Height / 8),
                            Palette = ReadPalette(fs, (int)fs.Position + 2)
                        };
                        break;

                    default:
                        texture.Width = -1;
                        texture.Height = -1;
                        break;
                }           

                

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

        public Texture GetTexture(string name)
        {
            return Textures.Find(t => t.Name.ToUpper() == name.ToUpper());
        }

        /**/

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

        private byte[,] ReadBMP(FileStream fs, int offset, int width, int height)
        {
            fs.Position = offset;

            var data = new byte[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    data[x, y] = (byte)fs.ReadByte();
                }
            }

            return data;
        }

        private RGB[] ReadPalette(FileStream fs, int offset)
        {
            fs.Position = offset;

            var data = new RGB[256];
            for (int i = 0; i < 256; i++)
            {
                var rgb = new byte[3];
                fs.Read(rgb, 0, 3);
                data[i] = new RGB()
                {
                    R = rgb[0],
                    G = rgb[1],
                    B = rgb[2]
                };
            }

            return data;
        }
    }
}
