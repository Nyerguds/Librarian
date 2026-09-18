using System;
using System.Collections.Generic;
using System.Linq;
using Nyerguds.Util;

namespace Nyerguds.FileData.Dynamix
{
    /// <summary>
    /// LZW compression class. Experimental.
    /// </summary>
    public class DynamixLzwEncoder
    {
        private List<byte[]> dictKeys = new List<byte[]>();
        private List<int> dictCodes = new List<int>();


        private int GetCode(byte[] sequence)
        {
            int seqLen = sequence.Length;
            if (seqLen == 1)
                return sequence[0];
            // 256 is not used; it's the "reset" code.
            for (int i = 257; i < dictKeys.Count; ++i)
            {
                byte[] check = dictKeys[i];
                if (seqLen != check.Length)
                    continue;
                bool noMatch = false;
                for (int bi = 0; bi < check.Length; ++bi)
                {
                    if (sequence[bi] == check[bi])
                        continue;
                    noMatch = true;
                    break;
                }
                if (noMatch)
                    continue;
                return i;
            }
            return -1;
        }

        private bool ContainsCode(byte[] sequence)
        {
            return GetCode(sequence) != -1;
        }

        public DynamixLzwEncoder()
        {
            for (int i = 0; i < 256; ++i)
            {
                dictKeys.Add(new byte[] { (byte)i });
                dictCodes.Add(i);
            }
            // Reset code
            dictKeys.Add(null);
            dictCodes.Add(256);
        }

        public byte[] Compress(byte[] buffer)
        {
            int codeLen = 9;
            int bitIndex = 0;
            int outbuffSize = buffer.Length * 2;
            byte[] outbuff = new byte[outbuffSize];
            int addedSize = 0;
            for (int i = 0; i < buffer.Length; ++i)
            {
                byte b = buffer[i];
                // increase code length to amount of bits needed by intCode.
                ArrayUtils.WriteBitsToByteArray(outbuff, bitIndex, codeLen, b);
                bitIndex += codeLen;
                if (((i + addedSize + 1) % 24) != 23)
                    continue;
                ArrayUtils.WriteBitsToByteArray(outbuff, bitIndex, codeLen, 0x100);
                bitIndex += codeLen;
                addedSize++;
            }
            int bufSize = (bitIndex + 7) / 8;
            byte[] outbuf2 = new byte[bufSize];
            Array.Copy(outbuff, outbuf2, bufSize);
            return outbuf2;
        }


        public int[] CompressToInts(byte[] buffer)
        {
            byte[] match = new byte[0];
            int[] compressed = new int[(buffer.Length * 2) / 3];
            int index = 0;
            foreach (byte b in buffer)
            {
                int oldLen = match.Length;
                byte[] nextMatch = new byte[oldLen + 1];
                nextMatch[oldLen] = b;
                if (ContainsCode(nextMatch))
                    match = nextMatch;
                else
                {
                    int code = GetCode(match);
                    // Add current code to list
                    compressed[index++]= dictCodes[code];
                    // new sequence; add it to the dictionary
                    dictKeys.Add(nextMatch.ToArray());
                    dictCodes.Add(dictCodes.Count);
                    match = new byte[] { b };
                }
            }
            // write remaining output if necessary
            if (match.Length > 0)
            {
                int code = GetCode(match);
                compressed[index++] = dictCodes[code];
            }
            int[] finalCodes = new int[index];
            Array.Copy(compressed, 0, finalCodes, 0, index);
            return finalCodes;
        }
    }

}