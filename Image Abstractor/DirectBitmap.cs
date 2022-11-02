using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Image_Abstractor {
    public class DirectBitmap : IDisposable {
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height) {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
        }

        public DirectBitmap(Image image) {
            Width = image.Width;
            Height = image.Height;
            Bits = new Int32[Width * Height];
            Bitmap Bitmap = new Bitmap(image);

            // Set Bits to Bitmap values
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    SetPixel(x, y, Bitmap.GetPixel(x, y));
                }
            }
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
        }

        public void SetPixel(int x, int y, Color colour) {
            int pos = x + (y * Width);
            int col = colour.ToArgb();
            Bits[pos] = col;
        }

        public Color GetPixel(int x, int y) {
            int argb = x + (y * Width);
            Color a = Color.FromArgb(Bits[argb]);
            //Color b = Bitmap.GetPixel(x, y);

            //if (a == b) return Color.Green;
            //return Color.Red;
            //else if (a != b) Console.WriteLine($"ERROR AT ({x}, {y})");
            return a;

        }

        public void Dispose() {
            if (Disposed) return;
            Disposed = true;
            BitsHandle.Free();
        }
    }
}
