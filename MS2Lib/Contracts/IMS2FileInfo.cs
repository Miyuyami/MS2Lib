using System;

namespace MS2Lib
{
    public interface IMS2FileInfo : IEquatable<IMS2FileInfo>
    {
        string Id { get; }
        string Path { get; }
        string RootFolderId { get; }
    }
}