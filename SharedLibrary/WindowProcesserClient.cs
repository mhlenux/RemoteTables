
using System;
using System.Drawing;

namespace SharedLibrary
{
    public class WindowProcesserClient
    {
        private Bitmap[,] currentImages;

        public Bitmap RenderImage(Payload payload)
        {
            var seed = payload.ImageAmountSeed;
            if (currentImages == null) currentImages = new Bitmap[seed, seed];

            Bitmap finalImage = null;
            UpdateCurrentImages(payload.WindowImages);

            try
            {
                // Draw final
                finalImage = new Bitmap(payload.WindowWidth, payload.WindowHeight);
                using Graphics finalGraphics = Graphics.FromImage(finalImage);

                //set background color
                finalGraphics.Clear(Color.Black);

                //
                int width = currentImages[0, 0].Width;
                int height = currentImages[0, 0].Height;
                int posx = 0;
                int posy = 0;
                //go through each image and draw it on the final image
                for (int col = 0; col < currentImages.GetLength(0); col++)
                {
                    for (int row = 0; row < currentImages.GetLength(1); row++)
                    {
                        posx = width * row;
                        posy = height * col;
                        finalGraphics.DrawImage(currentImages[col, row], new Rectangle(posx, posy, width, height));
                    }
                }
            }
            catch (Exception)
            {
                if (finalImage != null)
                    finalImage.Dispose();
            }
            finally
            {
                // Cleanup
            }
            return finalImage;
        }

        private void UpdateCurrentImages(Bitmap[,] newImages)
        {
            for (int col = 0; col < currentImages.GetLength(0); col++)
            {
                for (int row = 0; row < currentImages.GetLength(1); row++)
                {
                    // If incoming new images not null, update it to current images
                    if (newImages[col, row] != null)
                    {
                        if (currentImages[col, row] != null) currentImages[col, row].Dispose();
                        currentImages[col, row] = new Bitmap(newImages[col, row]);
                    }
                }
            }
        }
    }
}
