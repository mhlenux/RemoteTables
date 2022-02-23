using ProtoBuf;

using K4os.Compression.LZ4.Streams;

using SharedLibrary.Interfaces;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace SharedLibrary
{
    public class PayloadConverter
    {
        private readonly ILogWindow logger;

        // Format we use
        static readonly ImageFormat imageFormat = ImageFormat.Bmp;

        public PayloadConverter(ILogWindow logger)
        {
            this.logger = logger;
        }

        public void Serialize(Payload payload, NetworkStream stream)
        {
            if (null == payload || stream == null || stream.CanWrite == false) return;

            try
            {
                if (payload.WindowImages != null)
                {
                    List<byte[]> windowImagesInBytes = new List<byte[]>();
                    ConvertBmpsToByteArray(payload, windowImagesInBytes);
                    payload.WindowImagesInBytes = windowImagesInBytes;
                }

                using var compressionStream = LZ4Stream.Encode(stream, null, true);
                Serializer.SerializeWithLengthPrefix(compressionStream, payload, PrefixStyle.Base128);
            }
            catch (Exception ex)
            {
                //logger.Add(0.0f, LoggerStateConst.System, $"{nameof(PayloadConverter)}.{nameof(Serialize)} ::: {ex.Message}", LoggerStateConst.DEBUG);
            }
        }

        public Payload Deserialize(NetworkStream stream)
        {
            if (null == stream || stream.CanRead == false) return null;

            try
            {
                Payload payload = null;

                using var decompressionStream = LZ4Stream.Decode(stream, null, true);
                payload = Serializer.DeserializeWithLengthPrefix<Payload>(decompressionStream, PrefixStyle.Base128);

                if (payload == null) return null;
                if (payload.WindowImagesInBytes != null && payload.WindowImagesInBytes.Count > 0)
                {
                    Bitmap[,] bitmaps = new Bitmap[payload.ImageAmountSeed, payload.ImageAmountSeed];
                    ConvertByteArrayToBitmaps(payload, bitmaps);
                    payload.WindowImages = bitmaps;
                }

                return payload;
            }
            catch (Exception ex)
            {
                //logger.Add(0.0f, LoggerStateConst.System, $"{nameof(PayloadConverter)}.{nameof(Deserialize)} ::: {ex.Message}", LoggerStateConst.DEBUG);
            }
            return null;
        }

        private static void ConvertBmpsToByteArray(Payload payload, List<byte[]> windowImagesInBytes)
        {
            for (int col = 0; col < payload.WindowImages.GetLength(0); col++)
            {
                for (int row = 0; row < payload.WindowImages.GetLength(1); row++)
                {
                    var image = payload.WindowImages[col, row];

                    // if null add null bytes array
                    if (image == null)
                    {
                        windowImagesInBytes.Add(Array.Empty<byte>());
                        continue;
                    }

                    // convert bitamp to bytes array, add to list
                    using MemoryStream ms = new MemoryStream();
                    image.Save(ms, imageFormat); // TODO: try PNG
                    byte[] bytes = ms.ToArray();
                    windowImagesInBytes.Add(bytes);
                }
            }
        }

        private static void ConvertByteArrayToBitmaps(Payload payload, Bitmap[,] bitmaps)
        {
            int index = 0;
            for (int col = 0; col < bitmaps.GetLength(0); col++)
            {
                for (int row = 0; row < bitmaps.GetLength(1); row++)
                {
                    if (payload.WindowImagesInBytes[index] == Array.Empty<byte>())
                    {
                        bitmaps[col, row] = null;
                    }
                    else
                    {
                        using var ms = new MemoryStream(payload.WindowImagesInBytes[index]);
                        bitmaps[col, row] = new Bitmap(ms);
                    }

                    index++;
                }
            }
        }
    }
}
