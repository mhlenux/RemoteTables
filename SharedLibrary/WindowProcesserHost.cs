using LowLevelLibrary;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SharedLibrary
{
    public class WindowProcesserHost
    {
        // Used to read publicly settings of this window
        public int WindowWidth { get; private set; } = 0;
        public int WindowHeight { get; private set; } = 0;
        public readonly int ImageAmountSeed;

        // Private fields
        private readonly Bitmap[,] currentImages;
        private readonly IntPtr handle;
        int imageWidth = 0;
        int imageHeight = 0;
        PixelFormat pixelFormat = PixelFormat.Format32bppArgb;

        // How many updatable images single window contains (imageAmountSeed * imageAmountSeed = imageAmount)
        readonly int imageAmount;

        public WindowProcesserHost(int handle, int imageAmountSeed)
        {
            if (imageAmountSeed < 1) throw new Exception($"Image amount seed not parsed correctly. It was ${imageAmountSeed}.");

            this.ImageAmountSeed = imageAmountSeed;
            this.currentImages = new Bitmap[ImageAmountSeed, ImageAmountSeed];
            this.imageAmount = ImageAmountSeed * ImageAmountSeed;
            this.handle = new IntPtr(handle);
        }

        /// <summary>
        /// Returns only changed images.
        /// </summary>
        /// <returns></returns>
        public Bitmap[,] CaptureWindowAsImages()
        {
            Graphics[,] graphics = new Graphics[ImageAmountSeed, ImageAmountSeed];
            Bitmap[,] fetchedImages = new Bitmap[ImageAmountSeed, ImageAmountSeed];

            try
            {
                User32.GetWindowRect(handle, out User32.RECT windowRect);

                // set single image size
                imageWidth = windowRect.Width / ImageAmountSeed;
                imageHeight = windowRect.Height / ImageAmountSeed;

                // set window size
                WindowWidth = windowRect.Width;
                WindowHeight = windowRect.Height;

                // Initialize new images
                for (int col = 0; col < fetchedImages.GetLength(0); col++)
                {
                    for (int row = 0; row < fetchedImages.GetLength(1); row++)
                    {
                        fetchedImages[col, row] = new Bitmap(imageWidth, imageHeight, pixelFormat);
                    }
                }

                CopyWindowToNewImages(graphics, fetchedImages, windowRect, imageWidth, imageHeight);

                // Update new images to current images
                for (int col = 0; col < fetchedImages.GetLength(0); col++)
                {
                    for (int row = 0; row < fetchedImages.GetLength(1); row++)
                    {
                        // If fetched image is same as current, remove from fetched list.
                        if (CompareBitmaps(currentImages[col, row], fetchedImages[col, row]))
                        {
                            if (fetchedImages[col, row] != null)
                            {
                                fetchedImages[col, row].Dispose();
                                fetchedImages[col, row] = null;
                            }
                        }
                        else // if fetched is not same as current update current with new image
                        { // fetched list arleady includes new image
                            if (currentImages[col, row] != null) currentImages[col, row].Dispose();
                            currentImages[col, row] = new Bitmap(fetchedImages[col, row]);
                        }
                    }
                }
            }
            finally
            {
                //clean up memory
                foreach (var graphic in graphics)
                {
                    if (graphic != null) graphic.Dispose();
                }
            }
            return fetchedImages;
        }

        private void CopyWindowToNewImages(Graphics[,] graphics, Bitmap[,] newImages, User32.RECT windowRect,
            int imageWidth, int imageHeight)
        {
            var size = new Size(imageWidth, imageHeight);

            for (int col = 0; col < graphics.GetLength(0); col++)
            {
                for (int row = 0; row < graphics.GetLength(1); row++)
                {
                    int x = windowRect.Left + this.imageWidth * row;
                    int y = windowRect.Top + this.imageHeight * col;
                    graphics[col, row] = Graphics.FromImage(newImages[col, row]);
                    graphics[col, row].CopyFromScreen(x, y, 0, 0, size);
                }
            }
        }

        private static bool CompareBitmaps(Bitmap bmp1, Bitmap bmp2)
        {
            if (bmp1 == null || bmp2 == null)
                return false;
            if (object.Equals(bmp1, bmp2))
                return true;
            if (!bmp1.Size.Equals(bmp2.Size) || !bmp1.PixelFormat.Equals(bmp2.PixelFormat))
                return false;

            int bytes = bmp1.Width * bmp1.Height * (Image.GetPixelFormatSize(bmp1.PixelFormat) / 8);

            bool result = true;
            byte[] b1bytes = new byte[bytes];
            byte[] b2bytes = new byte[bytes];

            BitmapData bitmapData1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadOnly, bmp1.PixelFormat);
            BitmapData bitmapData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, bmp2.PixelFormat);

            try
            {
                Marshal.Copy(bitmapData1.Scan0, b1bytes, 0, bytes);
                Marshal.Copy(bitmapData2.Scan0, b2bytes, 0, bytes);

                for (int n = 0; n <= bytes - 1; n++)
                {
                    if (b1bytes[n] != b2bytes[n])
                    {
                        result = false;
                        break;
                    }
                }
            }
            finally
            {
                bmp1.UnlockBits(bitmapData1);
                bmp2.UnlockBits(bitmapData2);
            }
            return result;
        }
    }
}
