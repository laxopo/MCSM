using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCSMapConv
{
    public class FontDim
    {
        public List<FT> Fonts { get; set; } = new List<FT>();

        public class FT
        {
            public VHE.WAD.Texture Texture { get; set; }
            public Dim[] Dimensions { get; set; }
        }

        public class Dim
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
        }

        public FontDim() { }

        public Dim GetCharDim(VHE.WAD.Texture fontTexture, int index)
        {
            var ft = Fonts.Find(x => x.Texture.Name == fontTexture.Name);

            if (ft == null)
            {
                //create new dim
                var dims = new Dim[256];

                //// Get char width & offset
                var scale = fontTexture.Width / 128;

                for (int chy = 0; chy < 16; chy++)
                {
                    for (int chx = 0; chx < 16; chx++)
                    {
                        int x0 = chx * scale * 8;
                        int y0 = chy * scale * 8;
                        int xmin = 0, xmax = -1, ymin = 0, ymax = -1;
                        bool found = false;

                        for (int y = 0; y < 8; y++)
                        {
                            for (int x = 0; x < 8; x++)
                            {
                                int tx = x0 + x * scale;
                                int ty = y0 + y * scale;
                                int p = fontTexture.Data.Main[tx, ty];

                                if (p != 255)
                                {
                                    if (x < xmin || !found)
                                    {
                                        xmin = x;
                                    }

                                    if (x > xmax || !found)
                                    {
                                        xmax = x;
                                    }

                                    if (y < ymin || !found)
                                    {
                                        ymin = y;
                                    }

                                    if (y > ymax || !found)
                                    {
                                        ymax = y;
                                    }

                                    found = true;
                                }
                            }
                        }

                        dims[chy * 16 + chx] = new Dim()
                        {
                            OffsetX = xmin,
                            OffsetY = ymin,
                            Width = xmax - xmin + 1,
                            Height = ymax - ymin + 1
                        };
                    }
                }

                ft = new FT()
                {
                    Texture = fontTexture,
                    Dimensions = dims,
                };

                Fonts.Add(ft);

            }

            return ft.Dimensions[index % 256];
        }

        public int GetStringWidth(VHE.WAD.Texture fontTexture, string text)
        {
            int w = 0;
            foreach (var ch in text)
            {
                int ascii = ch;
                var dim = GetCharDim(fontTexture, ascii);
                if (dim.Width == 0)
                {
                    w += 3;
                }
                else
                {
                    w += dim.Width;
                }
            }

            return w;
        }
    }
}
