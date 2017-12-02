using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiscUtils.IO;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileInfoHeader
    {
        private const int NumberOfProperties = 3;
        private const string Default = "";
        private List<string> Properties { get; }

        public string Id
        {
            get => this.Properties[0];
            set => this.Properties[0] = value;
        }

        public string RootFolderId => BuildRootFolderId(this.Name);

        public string Name
        {
            get => this.Properties[2];
            set => this.Properties[2] = value;
        }

        private MS2FileInfoHeader(List<string> properties)
        {
            Debug.Assert(properties.Count == NumberOfProperties);

            this.Properties = properties;
        }

        internal static MS2FileInfoHeader Create(string id, MS2FileInfoHeader other)
            => Create(id, other.Name);

        internal static MS2FileInfoHeader Create(string id, string name)
            => new MS2FileInfoHeader(new List<string>() { id, BuildRootFolderId(name), name.Replace('\\', '/') });

        private static string BuildRootFolderId(string fileName)
        {
            string rootDirectory = PathEx.GetRootDirectory(fileName);
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
                    throw new Exception($"Unrecognised character in root directory [{c}].");
                }
            }

            return sb.ToString();
        }

        internal static async Task<MS2FileInfoHeader> Load(Stream stream)
        {
            using (var usr = new UnbufferedStreamReader(stream, true))
            {
                string line = await usr.ReadLineAsync().ConfigureAwait(false);

                if (String.IsNullOrWhiteSpace(line))
                {
                    return new MS2FileInfoHeader(new List<string>(Enumerable.Repeat(String.Empty, NumberOfProperties)));
                }

                string[] properties = line.Split(',');
                if (properties.Length == 3)
                {
                    return new MS2FileInfoHeader(properties.ToList());
                }
                else if (properties.Length == 2)
                {
                    return new MS2FileInfoHeader(new List<string>() { properties[0], String.Empty, properties[1] });
                }
                else
                {
                    throw new Exception($"Unrecognised number of properties [{properties.Length}].");
                }
            }
        }

        internal async Task Save(Stream stream)
        {
            if (this.Properties.Count == 0)
            {
                return;
            }

            using (var sw = new UnbufferedStreamWriter(stream, Encoding.ASCII, true))
            {
                string line = String.Join(",", this.Properties.Where(s => !String.IsNullOrEmpty(s)));

                await sw.WriteLineAsync(line).ConfigureAwait(false);
            }
        }

        private string GetOrDefault(int index)
        {
            if (this.Properties.Count > index)
            {
                return this.Properties[index];
            }

            return Default;
        }

        private void SetOrDefault(int index, string value)
        {

        }

        private string DebuggerDisplay
            => $"Count = {this.Properties.Count}";
    }
}
