namespace MS2Lib
{
    public interface IMultiArray
    {
        int ArraySize { get; }
        int Count { get; }

        byte[] this[long index] { get; }
    }
}
