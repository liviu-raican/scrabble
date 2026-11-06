using System.Collections;

namespace DAWGCompressor
{
    internal class ByteArrayBuilder(int capacity)
    {
        private readonly BitArray bitArray = new(capacity);

        private int position = 0;

        internal void SetPosition(int position)
        {
            this.position = position;
        }

        internal void AppendBits(int value, byte bitCount)
        {
            int mask = 1 << (bitCount - 1);
            while (mask != 0)
            {
                bool bit = (value & mask) != 0;
                bitArray[position++] = bit;
                mask >>= 1;
            }
        }

        internal byte[] ToByteArray()
        {
            int byteCount = (bitArray.Length + 7) / 8;
            byte[] bytes = new byte[byteCount];
            bitArray.CopyTo(bytes, 0);
            return bytes;
        }
    }
}
