using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NN
{
    public struct ImageInput
    {
        public float[] input;
        public int label;
        public string desc;
    }

    public static class Utils
    {
        public static byte[] BitmapToByteArray(Bitmap bitmap)
        {
            BitmapData bmpdata = null;

            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                byte[] bytedata = new byte[bmpdata.Stride * bitmap.Height];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, bytedata.Length);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }
        }

        public static float[] PixelsToFloats(byte[] pixels, int bytesPerPixel)
        {
            float[] output = new float[pixels.Length / bytesPerPixel];
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (pixels[i * bytesPerPixel] + pixels[i * bytesPerPixel + 1] + pixels[i * bytesPerPixel + 2]) / 3; // combine RGB
            }
            return output;
        }

        public static float[] NormalizeVector(float[] input)
        {
            float[] output = new float[input.Length];
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = input[i] / 255;
            }
            return output;
        }

        public static ImageInput[] LoadImageFile(string imagePath, string labelPath, int n = Int32.MaxValue)
        {
            float[] FixImageColumns(float[] fa)
            {
                float[] o = new float[fa.Length];
                for (int y = 0; y < 28; y++)
                {
                    for (int x = 0; x < 28; x++)
                    {
                        int offsetX = x + 22; // offset
                        if (offsetX >= 28)
                            offsetX -= 28;
                        o[y * 28 + x] = fa[y * 28 + offsetX];
                    }
                }
                return o;
            }

            Stream imageStream = null;
            Stream labelStream = null;
            try
            {
                imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                labelStream = new FileStream(labelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch
            {
                Console.WriteLine("Error loading image files.");
                return new ImageInput[0];
            }

            byte[] buffer32 = new byte[4];

            imageStream.Position = 0x04;
            imageStream.Read(buffer32, 0, 4);
            Array.Reverse(buffer32); // little endian
            int imageCount = BitConverter.ToInt32(buffer32, 0);

            labelStream.Position = 0x04;
            labelStream.Read(buffer32, 0, 4);
            Array.Reverse(buffer32); // little endian
            int labelCount = BitConverter.ToInt32(buffer32, 0);

            // figure out how many images to load
            n = Math.Min(imageCount, n);
            n = Math.Min(labelCount, n);
            ImageInput[] output = new ImageInput[n];

            // start reading data
            imageStream.Position = 0x16;
            labelStream.Position = 0x08;
            for (int i = 0; i < n; i++)
            {
                byte[] buffer = new byte[28 * 28];
                imageStream.Read(buffer, 0, 28 * 28);
                float[] pixelFloats = new float[28 * 28];
                for (int p = 0; p < buffer.Length; p++)
                    pixelFloats[p] = buffer[p];
                output[i].input = FixImageColumns(pixelFloats);
                output[i].input = NormalizeVector(output[i].input); // normalize

                // label
                output[i].label = labelStream.ReadByte();
            }

            Console.WriteLine("Loaded image file " + imagePath + ".");
            Console.WriteLine("Loaded image file " + labelPath + ".");
            return output;
        }

        public static ImageInput LoadPNG(string path)
        {
            ImageInput output = new ImageInput();
            BitmapSource bitmapSource = null;
            try
            {
                Stream streamSource = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                PngBitmapDecoder decoder = new PngBitmapDecoder(streamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                bitmapSource = decoder.Frames[0];
            }
            catch
            {
                Console.WriteLine("Error loading file");
                return output;
            }

            int pixelBytes = (bitmapSource.Format.BitsPerPixel + 7) / 8;
            int stride = bitmapSource.PixelWidth * pixelBytes;
            byte[] pixels = new byte[bitmapSource.PixelHeight * stride];
            bitmapSource.CopyPixels(pixels, stride, 0);

            output.input = PixelsToFloats(pixels, pixelBytes);
            output.input = NormalizeVector(output.input);
            output.label = (int)Char.GetNumericValue(Path.GetFileName(path).ElementAt(0));
            output.desc = Path.GetFileNameWithoutExtension(path);
            return output;
        }

        public static ImageInput[] LoadPNGsFromFolder(string path)
        {
            string[] files = Directory.GetFiles(path);
            ImageInput[] output = new ImageInput[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                output[i] = LoadPNG(files[i]);
            }
            return output;
        }

        public static void SaveImages(ImageInput[] inputs, int count = Int32.MaxValue)
        {

            for (int i = 0; i < Math.Min(count, inputs.Length); i++)
            {
                Bitmap bmp = new Bitmap(28, 28);
                for (int y = 0; y < 28; y++)
                {
                    for (int x = 0; x < 28; x++)
                    {
                        int p = (int)(inputs[i].input[y * 28 + x] * 255);
                        bmp.SetPixel(x, y, Color.FromArgb(p, p, p));
                    }
                }

                // save image
                bmp.Save(i + ".bmp");
            }
        }
    }
}
