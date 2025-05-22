using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace MCSM.VHE
{
    public class WAD
    {
        public string FilePath { get; set; }
        public List<Texture> Textures { get; set; } = new List<Texture>();

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

            //System
            internal int Offset { get; set; }
            internal int Size { get; set; }

            public enum Format
            {
                QPic = 0x42,
                MipTex = 0x43
            }

            public void Rename(string name)
            {
                Name = name;

                switch (name[0])
                {
                    case '{':
                        Transparent = true;
                        break;

                    case '!':
                        Water = true;
                        break;

                    case '+':
                        if (name.Length > 2 && name[1] == 'A')
                        {
                            ToggledAnimate = true;
                        }
                        else
                        {
                            Animated = true;
                        }
                        break;

                    case '-':
                        RandomTiling = true;
                        break;

                    default:
                        if (name.Length > 3 && name.Substring(0, 3) == "sky")
                        {
                            Sky = true;
                        }
                        break;
                }
            }

            public void SetTextureData(Image image)
            {
                Data.Main = ConvertToBitmap(image);
                Height = image.Height;
                Width = image.Width;
                ReMipMap();
            }

            public void TransparencyFix()
            {
                switch ((Format)Type)
                {
                    case Format.QPic:
                        break;

                    case Format.MipTex:
                        break;

                    default:
                        throw GetException(ref Exceptions.UnsupportedFormat,
                            "Unsupported texture format: " + string.Format("{0:X}", Type));
                }

                TransparencyFix(Data.Main);
                ReMipMap();
            }

            public void ReMipMap()
            {
                if ((Format)Type != Format.MipTex)
                {
                    return;
                }

                Data.Mip1 = MipMap(Data.Main);
                Data.Mip2 = MipMap(Data.Mip1);
                Data.Mip3 = MipMap(Data.Mip2);
            }

            /**/

            private Bitmap MipMap(Bitmap image)
            {
                var mip = new Bitmap(image.Width / 2, image.Height / 2);
                var bmd_mip = GetBitmapData(mip);
                var bmd_image = GetBitmapData(image);

                for (int y = 0; y < mip.Height; y++)
                {
                    for (int x = 0; x < mip.Width; x++)
                    {
                        var c = GetPixel(bmd_image, x * 2, y * 2);
                        SetPixel(bmd_mip, x, y, c);
                    }
                }

                image.UnlockBits(bmd_image);
                mip.UnlockBits(bmd_mip);

                return mip;
            }

            private static void TransparencyFix(Bitmap bmp)
            {
                var cc = bmp.Palette.Entries[255];
                if (cc.R == 0 && cc.G == 0 && cc.B == 255)
                {
                    throw GetException(ref Exceptions.AlreadyOK, "Texture is already ok");
                }

                var bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadWrite, bmp.PixelFormat);

                //find blue index
                int bidx = -1;
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        var c = GetPixel(bmd, x, y);
                        var color = bmp.Palette.Entries[c];

                        if (color.R == 0 && color.G == 0 && color.B == 255)
                        {
                            bidx = c;
                            break;
                        }
                    }

                    if (bidx != -1)
                    {
                        break;
                    }
                }

                if (bidx == -1)
                {
                    bmp.UnlockBits(bmd);
                    throw GetException(ref Exceptions.TransColorNotFound, "Blue color not found");
                }

                //swap colors
                var bufColor = bmp.Palette.Entries[255];
                var pal = bmp.Palette;
                pal.Entries[255] = Color.FromArgb(0, 0, 255);
                pal.Entries[bidx] = bufColor;
                bmp.Palette = pal;

                //swap indexes
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        var c = GetPixel(bmd, x, y);

                        if (c == bidx)
                        {
                            SetPixel(bmd, x, y, 255);
                        }
                        else if (c == 255)
                        {
                            SetPixel(bmd, x, y, (byte)bidx);
                        }
                    }
                }

                bmp.UnlockBits(bmd);
            }
        }

        public class BMP
        {
            public Bitmap Main { get; set; }
            public Bitmap Mip1 { get; set; }
            public Bitmap Mip2 { get; set; }
            public Bitmap Mip3 { get; set; }
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

        public static class Exceptions
        {
            public static Exception UnsupportedFormat;
            public static Exception AlreadyOK;
            public static Exception TransColorNotFound;
            public static Exception NameAlreadyPresent;
            public static Exception PaletteOverflow;
        }

        public WAD(string filepath)
        {
            FilePath = filepath;
            var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            //Header
            if (ReadString(fs, 4) != "WAD3")
            {
                throw new Exception("Error: invalid file format " + filepath);
            }

            int count = ReadInt(fs);
            fs.Position = ReadInt(fs);

            //Lump item info
            for (int i = 0; i < count; i++)
            {
                var offset = ReadInt(fs);
                var compLength = ReadInt(fs);
                var fullLength = ReadInt(fs);
                var type = fs.ReadByte();
                var compType = fs.ReadByte();

                if (compType != 0)
                {
                    throw new Exception("Compressed textures are not supported");
                }

                fs.Position += 2;
                var name = ReadString(fs, 16);
                var pos = fs.Position;

                var texture = ReadTexture(fs, offset, type);
                texture.Rename(name);
                Textures.Add(texture);
                fs.Position = pos;
            }

            fs.Close();
        }

        /**/

        public Texture GetTexture(string name)
        {
            return Textures.Find(t => t.Name.ToUpper() == name.ToUpper());
        }

        public void AddTexture(Image image, string name)
        {
            if (!CheckTextureName(name))
            {
                throw GetException(ref Exceptions.NameAlreadyPresent, 
                    "Texture name \"" + name + "\" is already present in the WAD.");
            }

            var texbmp = ConvertToBitmap(image);

            var tex = new Texture()
            {
                Type = (byte)Texture.Format.MipTex,
                Height = texbmp.Height,
                Width = texbmp.Width,
                Data = new BMP()
                {
                    Main = texbmp
                }
            };

            tex.Rename(name);
            tex.ReMipMap();

            Textures.Add(tex);
        }

        public bool CheckTextureName(string name)
        {
            return Textures.Find(x => x.Name.ToUpper() == name.ToUpper()) == null;
        }

        public void Save()
        {
            var ms = new MemoryStream();

            //Header
            WriteString(ms, "WAD3");
            WriteInt(ms, Textures.Count);
            ms.Position += 4;

            //Texture Data
            foreach (var tex in Textures)
            {
                tex.Offset = (int)ms.Position;
                switch ((Texture.Format)tex.Type)
                {
                    case Texture.Format.QPic:
                        WriteInt(ms, tex.Width);
                        WriteInt(ms, tex.Height);
                        WriteBMPData(ms, tex.Data.Main);
                        ms.WriteByte(0x00);
                        ms.WriteByte(0x01);
                        WriteBMPPalette(ms, tex.Data.Main);
                        ms.WriteByte(0x00);
                        ms.WriteByte(0x00);
                        break;

                    case Texture.Format.MipTex:
                        WriteString(ms, tex.Name, 16);
                        WriteInt(ms, tex.Width);
                        WriteInt(ms, tex.Height);
                        var offsPos = ms.Position;
                        ms.Position += 16;
                        var offMain = ms.Position - tex.Offset;
                        WriteBMPData(ms, tex.Data.Main);
                        var offM1 = ms.Position - tex.Offset;
                        WriteBMPData(ms, tex.Data.Mip1);
                        var offM2 = ms.Position - tex.Offset;
                        WriteBMPData(ms, tex.Data.Mip2);
                        var offM3 = ms.Position - tex.Offset;
                        WriteBMPData(ms, tex.Data.Mip3);
                        ms.WriteByte(0x00);
                        ms.WriteByte(0x01);
                        WriteBMPPalette(ms, tex.Data.Main);
                        ms.WriteByte(0x00);
                        ms.WriteByte(0x00);

                        var pos = ms.Position;
                        ms.Position = offsPos;
                        WriteInt(ms, (int)offMain);
                        WriteInt(ms, (int)offM1);
                        WriteInt(ms, (int)offM2);
                        WriteInt(ms, (int)offM3);
                        ms.Position = pos;
                        tex.Size = (int)pos - tex.Offset;
                        break;
                }
            }

            //Lumps offset
            var dataoffset = (int)ms.Position;
            ms.Position = 8;
            WriteInt(ms, dataoffset);
            ms.Position = dataoffset;

            //Lump item info
            foreach (var tex in Textures)
            {
                WriteInt(ms, tex.Offset);
                WriteInt(ms, tex.Size);
                WriteInt(ms, tex.Size);
                ms.WriteByte(tex.Type);
                ms.WriteByte(0x00);
                ms.WriteByte(0x00);
                ms.WriteByte(0x00);
                WriteString(ms, tex.Name, 16);
            }

            var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
            ms.WriteTo(fs);
            fs.Close();
            ms.Close();
        }

        public static unsafe void SetPixel(BitmapData bmd, int x, int y, byte c)
        {
            if (x >= bmd.Width || y >= bmd.Height || x < 0 || y < 0)
            {
                throw new Exception();
            }

            byte* p = (byte*)bmd.Scan0.ToPointer();
            int offset = y * bmd.Stride + (x);
            p[offset] = c;
        }

        public static unsafe Byte GetPixel(BitmapData bmd, int x, int y)
        {
            if (x >= bmd.Width || y >= bmd.Height || x < 0 || y < 0)
            {
                throw new Exception();
            }

            byte* p = (byte*)bmd.Scan0.ToPointer();
            int offset = y * bmd.Stride + x;
            return p[offset];
        }

        public static BitmapData GetBitmapData(Bitmap bmp)
        {
            return bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly, bmp.PixelFormat);
        }

        /**/

        private static Exception GetException(ref Exception e, string text)
        {
            e = new Exception(text);
            return e;
        }

        /*Serial data read*/

        private Texture ReadTexture(FileStream fs, int offset, int type)
        {
            var texture = new Texture();
            fs.Position = offset;

            switch ((Texture.Format)type)
            {
                case Texture.Format.QPic:
                    texture.Width = ReadInt(fs);
                    texture.Height = ReadInt(fs);
                    texture.Data = new BMP()
                    {
                        Main = ReadBMPData(fs, texture.Width, texture.Height),
                    };
                    ReadBMPPalette(fs, texture.Data.Main);
                    break;

                case Texture.Format.MipTex:
                    fs.Position += 16;
                    texture.Width = ReadInt(fs);
                    texture.Height = ReadInt(fs);
                    /*var offm = ReadInt(fs) + offset;
                    var offm1 = ReadInt(fs) + offset;
                    var offm2 = ReadInt(fs) + offset;
                    var offm3 = ReadInt(fs) + offset;*/
                    fs.Position += 16;
                    texture.Data = new BMP()
                    {
                        Main = ReadBMPData(fs, texture.Width, texture.Height),
                        Mip1 = ReadBMPData(fs, texture.Width / 2, texture.Height / 2),
                        Mip2 = ReadBMPData(fs, texture.Width / 4, texture.Height / 4),
                        Mip3 = ReadBMPData(fs, texture.Width / 8, texture.Height / 8),
                    };
                    fs.Position += 2;
                    ReadBMPPalette(fs, texture.Data.Main);
                    texture.Data.Mip1.Palette = texture.Data.Main.Palette;
                    texture.Data.Mip2.Palette = texture.Data.Main.Palette;
                    texture.Data.Mip3.Palette = texture.Data.Main.Palette;
                    break;

                default:
                    texture.Width = -1;
                    texture.Height = -1;
                    throw new Exception("Unsupported texture format");
            }

            texture.Type = (byte)type;
            return texture;
        }

        private string ReadString(FileStream fs, int count)
        {
            byte[] buf = new byte[count];
            fs.Read(buf, 0, count);
            return Encoding.UTF8.GetString(buf).Trim('\0');
        }

        private int ReadInt(FileStream fs)
        {
            byte[] buf = new byte[4];
            fs.Read(buf, 0, 4);
            return BitConverter.ToInt32(buf, 0);
        }

        private Bitmap ReadBMPData(FileStream fs, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            var bmd = GetBitmapData(bmp);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SetPixel(bmd, x, y, (byte)fs.ReadByte());
                }
            }

            bmp.UnlockBits(bmd);

            return bmp;
        }

        private void ReadBMPPalette(FileStream fs, Bitmap bmp)
        {
            var pal = bmp.Palette;

            for (int i = 0; i < 256; i++)
            {
                var rgb = new byte[3];
                fs.Read(rgb, 0, 3);
                pal.Entries[i] = Color.FromArgb(rgb[0], rgb[1], rgb[2]);
            }

            bmp.Palette = pal;
        }

        /*Serial data write*/

        private void WriteString(MemoryStream ms, string value, int length = -1)
        {
            if (length < 0)
            {
                length = value.Length;
            }

            for (int i = 0; i < length; i++)
            {
                if (i < value.Length)
                {
                    ms.WriteByte((byte)value[i]);
                }
                else
                {
                    ms.WriteByte(0x00);
                }
            }
        }

        private void WriteInt(MemoryStream ms, int value)
        {
            var buf = BitConverter.GetBytes(value);
            ms.Write(buf, 0, buf.Length);
        }

        private void WriteBMPData(MemoryStream ms, Bitmap bmp)
        {
            var bmd = GetBitmapData(bmp);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    ms.WriteByte(GetPixel(bmd, x, y));
                }
            }

            bmp.UnlockBits(bmd);
        }

        private void WriteBMPPalette(MemoryStream ms, Bitmap bmp)
        {
            for (int i = 0; i < 256; i++)
            {
                var c = bmp.Palette.Entries[i];
                ms.WriteByte(c.R);
                ms.WriteByte(c.G);
                ms.WriteByte(c.B);
            }
        }

        /**/

        private static void BMPSetPalette(Bitmap bmp, ColorPalette palette)
        {
            var pal = bmp.Palette;
            bmp.Palette = pal;
        }

        private static void BMPSetPalette(Bitmap bmp, Color[] palette)
        {
            var pal = bmp.Palette;
            for (int i = 0; i < 256; i++)
            {
                pal.Entries[i] = palette[i];
            }
            bmp.Palette = pal;
        }

        private static Bitmap ConvertToBitmap(Image image)
        {
            var texbmp = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);
            var srcbmp = new Bitmap(image);
            var texbmd = GetBitmapData(texbmp);
            var pal = new Color[256];
            var palIdx = 0;

            for (int y = 0; y < texbmp.Height; y++)
            {
                for (int x = 0; x < texbmp.Width; x++)
                {
                    var c = srcbmp.GetPixel(x, y);

                    if (c.A == 0)
                    {
                        c = Color.FromArgb(0, 0, 255);

                        if (pal[255].A != 255)
                        {
                            pal[255] = c;
                        }

                        SetPixel(texbmd, x, y, 255);
                    }
                    else
                    {
                        int index;
                        bool found = false;
                        for (index = 0; index < 256; index++)
                        {
                            var p = pal[index];
                            if (p.A == 255 && p.R == c.R && p.G == c.G && p.B == c.B)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            index = palIdx++;

                            if (index == 256)
                            {
                                throw GetException(ref Exceptions.PaletteOverflow, "The texture has more than 256 colors");
                            }

                            pal[index] = c;
                        }

                        SetPixel(texbmd, x, y, (byte)index);
                    }
                }
            }

            texbmp.UnlockBits(texbmd);
            BMPSetPalette(texbmp, pal);
            return texbmp;
        }
    }
}
