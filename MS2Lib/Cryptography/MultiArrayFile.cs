using System;
using System.IO;

namespace MS2Lib
{
    public class MultiArrayFile
    {
        public string FilePath { get; }
        public int ArraySize { get; }
        public int Count { get; }

        private readonly Lazy<byte[][]> LazyFile; // TODO: maybe array of lazy (Lazy<byte[]>[])

        public byte[] this[uint index] => this.LazyFile.Value[index % this.Count];

        public MultiArrayFile(string filePath, int count, int arraySize)
        {
            this.FilePath = filePath;
            this.Count = count;
            this.ArraySize = arraySize;

            this.LazyFile = new Lazy<byte[][]>(this.CreateLazyImplementation);
        }

        private byte[][] CreateLazyImplementation()
        {
            byte[][] result = new byte[this.Count][];

            using (var br = new BinaryReader(File.OpenRead(this.FilePath)))
            {
                for (int i = 0; i < this.Count; i++)
                {
                    byte[] bytes = br.ReadBytes(this.ArraySize);
                    if (bytes.Length == this.ArraySize)
                    {
                        result[i] = bytes;
                    }
                }
            }

            return result;
        }
    }
}
