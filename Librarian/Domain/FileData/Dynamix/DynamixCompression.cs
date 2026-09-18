using System;
using Nyerguds.FileData.Compression;
using Nyerguds.Util;

namespace Nyerguds.FileData.Dynamix
{
    /// <summary>
    /// Dynamix compression / decompression class. Offers functionality to decompress chunks using RLE or LZW decompression,
    /// and has functions to compress to RLE.
    /// </summary>
    public class DynamixCompression
    {

        public static byte[] EnrichFourBit(byte[] vgaData, byte[] binData)
        {
            byte[] fullData = new byte[vgaData.Length * 2];
            // ENRICHED 4-BIT IMAGE LOGIC
            // Basic principle: The data in the VGA chunk is already perfectly viewable as 4-bit image. The colour palettes
            // are designed so each block of 16 colours consists of different tints of the same colour. The 16-colour palette
            // for the VGA chunk alone can be constructed by taking a palette slice where each colour is 16 entries apart.

            // This VGA data [AB] gets "ennobled" to 8-bit by adding detail data [ab] from the BIN chunk, to get bytes [Aa Bb].
            for (int i = 0; i < vgaData.Length; ++i)
            {
                int offs = i * 2;
                // This can be written much simpler, but I expanded it to clearly show each step.
                byte vgaPix = vgaData[i]; // 0xAB
                byte binPix = binData[i]; // 0xab
                byte vgaPixHi = (byte)((vgaPix & 0xF0) >> 4); // 0x0A
                byte binPixHi = (byte)((binPix & 0xF0) >> 4); // 0x0a
                byte finalPixHi = (byte)((vgaPixHi << 4) + binPixHi); // Aa
                byte vgaPixLo = (byte)(vgaPix & 0x0F); // 0x0B
                byte binPixLo = (byte)(binPix & 0x0F); // 0x0b
                byte finalPixLo = (byte)((vgaPixLo << 4) + binPixLo); // Bb
                // Final result: AB + ab == [Aa Bb]
                fullData[offs] = finalPixHi;
                fullData[offs + 1] = finalPixLo;
            }
            return fullData;
        }

        public static void SplitEightBit(byte[] imageData, out byte[] vgaData, out byte[] binData)
        {
            vgaData = new byte[(imageData.Length + 1) / 2];
            binData = new byte[(imageData.Length + 1) / 2];
            for (int i = 0; i < imageData.Length; ++i)
            {
                byte pixData = imageData[i];
                int pixHi = pixData & 0xF0;
                int pixLo = pixData & 0x0F;
                if (i % 2 == 0)
                    pixLo = pixLo << 4;
                else
                    pixHi = pixHi >> 4;
                int pixOffs = i / 2;
                vgaData[pixOffs] |= (byte)pixHi;
                binData[pixOffs] |= (byte)pixLo;
            }
        }

        /// <summary>
        /// Decompresses Dynamix chunk data. The chunk data should start with the compression
        /// type byte, followed by a 32-bit integer specifying the uncompressed length.
        /// </summary>
        /// <param name="chunkData">Chunk data to decompress. </param>
        /// <returns>The uncompressed data.</returns>
        public static byte[] DecodeChunk(byte[] chunkData)
        {
            if (chunkData.Length < 5)
                throw new FileTypeLoadException("Chunk is too short to read compression header!");
            byte compression = chunkData[0];
            int uncompressedLength = (int)ArrayUtils.ReadIntFromByteArray(chunkData, 1, 4, true);
            return Decode(chunkData, 5, null, compression, uncompressedLength);
        }

        /// <summary>
        /// Decompresses Dynamix data.
        /// </summary>
        /// <param name="buffer">Buffer to decompress</param>
        /// <param name="startOffset">Start offset of the data in the buffer</param>
        /// <param name="endOffset">End offset of the data in the buffer</param>
        /// <param name="compression">Compression type: 0 for uncompressed, 1 for RLE, 2 for LZA</param>
        /// <param name="decompressedSize">Decompressed size.</param>
        /// <returns>The uncompressed data.</returns>
        public static byte[] Decode(byte[] buffer, int? startOffset, int? endOffset, int compression, int decompressedSize)
        {
            int start = startOffset ?? 0;
            int end = endOffset ?? buffer.Length;
            if (end < start)
                throw new ArgumentException("End offset cannot be smaller than start offset!", "endOffset");
            if (start < 0 || start > buffer.Length)
                throw new ArgumentOutOfRangeException("startOffset");
            if (end < 0 || end > buffer.Length)
                throw new ArgumentOutOfRangeException("endOffset");
            switch (compression)
            {
                case 0:
                    byte[] outBuff = new byte[decompressedSize];
                    int len = Math.Min(end - start, decompressedSize);
                    Array.Copy(buffer, start, outBuff, 0, len);
                    return outBuff;
                case 1:
                    return RleDecode(buffer, (uint)start, (uint)end, decompressedSize, true);
                case 2:
                    return LzwDecode(buffer, start, end, decompressedSize);
                case 3:
                    return LzssDecode(buffer, start, end, decompressedSize);
                default:
                    throw new ArgumentException("Unknown compression type: \"" + compression + "\".", "compression");
            }
        }

        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool abortOnError)
        {
            byte[] outputBuffer = new byte[decompressedSize];
            // Uses standard RLE implementation.
            RleCompressionHighBitRepeat rle = new RleCompressionHighBitRepeat();
            rle.RleDecodeData(buffer, startOffset, endOffset, ref outputBuffer, abortOnError);
            return outputBuffer;
        }

        public static byte[] LzwDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            DynamixLzwDecoder lzwDec = new DynamixLzwDecoder();
            byte[] outputBuffer = new byte[decompressedSize];
            lzwDec.LzwDecode(buffer, startOffset, endOffset, outputBuffer);
            return outputBuffer;
        }

        public static byte[] LzssDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            return LzssHuffDecoder.LzssDecode(buffer, startOffset, endOffset, decompressedSize);
        }

        /// <summary>
        /// Applies LZW Encoding to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static byte[] LzssEncode(byte[] buffer)
        {
            LzssHuffDecoder enc = new LzssHuffDecoder();
            return null; // enc.Encode(buffer, null, null);
        }
        
        /// <summary>
        /// Applies LZW Encoding to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static byte[] LzwEncode(byte[] buffer)
        {
            DynamixLzwEncoder enc= new DynamixLzwEncoder();
            return enc.Compress(buffer);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static byte[] RleEncode(byte[] buffer)
        {
            // Uses standard RLE implementation.
            RleCompressionHighBitRepeat rle = new RleCompressionHighBitRepeat();
            return rle.RleEncodeData(buffer);
        }

        /// <summary>Switches index 00 and FF on indexed image data, to compensate for this oddity in the MA8 chunks.</summary>
        /// <param name="imageData">Image data to process.</param>
        public static void SwitchBackground(byte[] imageData)
        {
            for (int i = 0; i < imageData.Length; ++i)
            {
                if (imageData[i] == 0x00)
                    imageData[i] = 0xFF;
                else if (imageData[i] == 0xFF)
                    imageData[i] = 0x00;
            }
        }
    }
}