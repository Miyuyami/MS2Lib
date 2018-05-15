using System;
using System.IO;
using System.Resources;

namespace MS2Lib
{
    public class MultiArrayResource : IMultiArray
    {
        public ResourceManager ResourceManager { get; }
        string IMultiArray.Name => this.ResourceName;
        public string ResourceName { get; }
        public int ArraySize { get; }
        public int Count { get; }

        private readonly Lazy<byte[][]> LazyResource; // TODO: maybe array of lazy (Lazy<byte[]>[])

        public byte[] this[uint index] => this.LazyResource.Value[index % this.Count];

        public MultiArrayResource(ResourceManager resourceManager, string resourceName, int count, int arraySize)
        {
            this.ResourceManager = resourceManager;
            this.ResourceName = resourceName;
            this.Count = count;
            this.ArraySize = arraySize;

            this.LazyResource = new Lazy<byte[][]>(this.CreateLazyImplementation);
        }

        private byte[][] CreateLazyImplementation()
        {
            byte[][] result = new byte[this.Count][];

            using (var br = new BinaryReader(new MemoryStream((byte[])this.ResourceManager.GetObject(this.ResourceName))))
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
