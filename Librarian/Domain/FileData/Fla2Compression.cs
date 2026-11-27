using System;
using System.Collections.Generic;

namespace LibrarianTool.Domain.FileData
{
    /// <summary>
    /// Compression used by Interactive Studios, in the games Glover (N64) and WarGames: Defcon 1 (PC).
    /// Info based on C code at https://github.com/DSLL32/nviewer/blob/master/codecs/fla2.cpp
    /// and further info on its wiki https://dsll32.github.io/nviewer/wiki/compression/fla2.html
    /// 
    /// No license was given, and I suspect the code is derived from an LLM decompilation.
    /// </summary>
    public static class Fla2Compression
    {
        public static byte[] Fla2Decode(byte[] src, int index, int size, int outputSize, out int written, out int consumed)
        {
            written = 0;
            consumed = 0;

            if (src == null || index < 0 || size < 2 ||
                index > src.Length - size)
                return null;
            byte[] dst = new byte[outputSize];

            byte[] ring = new byte[4096];

            int input = index;
            int end = index + size;
            int output = 0;
            int cursor = 0;

            while (true)
            {
                if (input == end)
                    return null;

                int flags = src[input++];

                for (int bit = 0; bit < 8; bit++)
                {
                    if ((flags & (0x80 >> bit)) != 0)
                    {
                        if (input == end)
                            return null;

                        int first = src[input++];

                        if (first == 0)
                        {
                            if (output != outputSize)
                                return null;

                            written = output;
                            consumed = input - index;
                            return dst;
                        }

                        if (input == end)
                            return null;

                        int second = src[input++];

                        int distance = ((first & 0xF0) << 4) | second;
                        int length = (first & 0x0F) + 2;

                        if (length > outputSize - output)
                            return null;

                        for (int j = 0; j < length; j++)
                        {
                            byte value = ring[(cursor - distance) & 4095];

                            dst[output++] = value;
                            ring[cursor++ & 4095] = value;
                        }
                    }
                    else
                    {
                        if (input == end || output == outputSize)
                            return null;

                        byte value = src[input++];

                        dst[output++] = value;
                        ring[cursor++ & 4095] = value;
                    }
                }
            }
        }

        private struct Match
        {
            public ushort Length;
            public ushort Distance;
            public ushort TwoDistance;
        }

        private static Match ExamineMatch(
            byte[] src, int index, int pos, int limit,
            int distance, Match best)
        {
            int length = 0;
            while (length < limit)
            {
                int from = pos + length - distance;
                byte value = from < 0 ? (byte)0 : src[index + from];
                if (value != src[index + pos + length])
                    break;
                length++;
            }
            if (length >= 2 && distance >= 256 && distance < 4096 && best.TwoDistance == 0)
            {
                best.TwoDistance = (ushort)distance;
            }
            if (length > best.Length)
            {
                best.Length = (ushort)length;
                best.Distance = (ushort)distance;
            }
            return best;
        }

        public static byte[] Fla2Encode(byte[] src, int Depth = 4096)
        {
            if (src == null)
                return null;
            int index = 0;
            int size = src.Length;
            try
            {
                Match[] matches = new Match[size];
                int[] previous = new int[size];
                int[] head = new int[65536];

                for (int i = 0; i < head.Length; i++)
                    head[i] = -1;

                for (int i = 0; i < previous.Length; i++)
                    previous[i] = -1;

                // Find matches.
                for (int pos = 0; pos < size; pos++)
                {
                    Match best = new Match();
                    int limit = Math.Min(size - pos, 17);
                    if (limit >= 2)
                    {
                        int key = (src[index + pos] << 8) |
                                   src[index + pos + 1];
                        int at = head[key];
                        for (int n = 0;
                             n < Depth && at >= 0 && pos - at <= 4096;
                             n++, at = previous[at])
                        {
                            int distance = pos - at;
                            int length = 2;
                            while (length < limit &&
                                   src[index + at + length] ==
                                   src[index + pos + length])
                            {
                                length++;
                            }
                            if (distance >= 256 && distance < 4096 &&
                                best.TwoDistance == 0)
                            {
                                best.TwoDistance = (ushort)distance;
                            }
                            if (length > best.Length)
                            {
                                best.Length = (ushort)length;
                                best.Distance = (ushort)distance;
                            }
                            if (best.Length == 17 && best.TwoDistance != 0)
                                break;
                        }
                        previous[pos] = head[key];
                        head[key] = pos;
                    }
                    // Examine matches against the initial zero-filled ring buffer.
                    if (pos < 4096 && limit >= 2 && src[index + pos] == 0)
                    {
                        int last = Math.Min(pos + 17, 4096);
                        for (int d = pos + 1; d <= last; d++)
                        {
                            best = ExamineMatch(src, index, pos, limit, d, best);
                        }
                        if (last != 4096)
                        {
                            best = ExamineMatch(src, index, pos, limit, 4096, best);
                        }
                    }
                    matches[pos] = best;
                }
                // Calculate exact compressed byte cost.
                uint[] cost = new uint[(size + 1) * 8];
                for (int phase = 0; phase < 8; phase++)
                {
                    cost[size * 8 + phase] = (uint)(1 + (phase == 0 ? 1 : 0));
                }
                for (int pos = size - 1; pos >= 0; pos--)
                {
                    Match m = matches[pos];
                    for (int phase = 0; phase < 8; phase++)
                    {
                        int next = (phase + 1) & 7;
                        uint best = 1 + cost[(pos + 1) * 8 + next];
                        if (m.TwoDistance != 0)
                        {
                            uint candidate = 2 + cost[(pos + 2) * 8 + next];
                            if (candidate < best)
                            {
                                best = candidate;
                            }
                        }
                        for (int length = 3; length <= m.Length; length++)
                        {
                            uint candidate = 2 + cost[(pos + length) * 8 + next];
                            if (candidate < best)
                            {
                                best = candidate;
                            }
                        }
                        cost[pos * 8 + phase] = best + (uint)(phase == 0 ? 1 : 0);
                    }
                }
                // Write compressed stream (without the 8-byte header).
                List<byte> dst = new List<byte>();
                int input = 0;
                int flagAt = 0;
                int phaseOut = 0;
                int flags = 0;
                while (true)
                {
                    if (phaseOut == 0)
                    {
                        flagAt = dst.Count;
                        dst.Add(0);
                        flags = 0;
                    }
                    if (input == size)
                    {
                        flags |= 0x80 >> phaseOut;
                        dst.Add(0);
                        dst[flagAt] = (byte)flags;
                        return dst.ToArray();
                    }
                    Match m = matches[input];
                    int next = (phaseOut + 1) & 7;
                    int lengthOut = 1;
                    uint bestCost = 1 + cost[(input + 1) * 8 + next];
                    if (m.TwoDistance != 0)
                    {
                        uint candidate = 2 + cost[(input + 2) * 8 + next];
                        if (candidate < bestCost)
                        {
                            bestCost = candidate;
                            lengthOut = 2;
                        }
                    }
                    for (int n = 3; n <= m.Length; n++)
                    {
                        uint candidate = 2 + cost[(input + n) * 8 + next];
                        if (candidate < bestCost || (candidate == bestCost && lengthOut > 1 && n > lengthOut))
                        {
                            bestCost = candidate;
                            lengthOut = n;
                        }
                    }

                    if (lengthOut == 1)
                    {
                        dst.Add(src[index + input]);
                    }
                    else
                    {
                        flags |= 0x80 >> phaseOut;
                        int distance = lengthOut == 2 ? m.TwoDistance : m.Distance;
                        dst.Add((byte)(((distance >> 8) << 4) | (lengthOut - 2)));
                        dst.Add((byte)distance);
                    }
                    input += lengthOut;
                    phaseOut = next;
                    if (phaseOut == 0)
                    {
                        dst[flagAt] = (byte)flags;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

    }
}
