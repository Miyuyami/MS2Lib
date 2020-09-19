using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MiscUtils.IO;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileInfo : IMS2FileInfo, IEquatable<MS2FileInfo>
    {
        public string Id { get; }
        public string Path { get; }
        public string RootFolderId { get; }

        public MS2FileInfo(string id, string path)
        {
            this.Id = id;
            this.Path = path.Replace('\\', '/');
            this.RootFolderId = BuildRootFolderId(this.Path);
        }

        private static string BuildRootFolderId(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return String.Empty;
            }

            string rootDirectory = PathEx.GetRootDirectory(path);
            if (String.IsNullOrWhiteSpace(rootDirectory))
            {
                return String.Empty;
            }

            var sb = new StringBuilder(rootDirectory.Length * 2);

            for (int i = 0; i < rootDirectory.Length; i++)
            {
                char c = rootDirectory[i];
                if (c == '_')
                {
                    sb.Append(c);
                    continue;
                }

                if (c >= '0' && c <= '9' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= 'a' && c <= 'z')
                {
                    // valid
                    sb.Append((byte)(c - '0'));
                }
                else
                {
                    throw new ArgumentException(nameof(path), $"Path contains an invalid character '{c}'.");
                }
            }

            return sb.ToString();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay =>
            $"Id = {this.Id}, Path = {this.Path}";

        #region Equality
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MS2FileInfo);
        }

        public virtual bool Equals(MS2FileInfo other)
        {
            return other != null &&
                   this.Id == other.Id &&
                   this.Path == other.Path;
        }

        bool IEquatable<IMS2FileInfo>.Equals(IMS2FileInfo other)
        {
            return this.Equals(other as MS2FileInfo);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Path);
        }

        public static bool operator ==(MS2FileInfo left, MS2FileInfo right)
        {
            return EqualityComparer<MS2FileInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(MS2FileInfo left, MS2FileInfo right)
        {
            return !(left == right);
        }
        #endregion
    }
}
