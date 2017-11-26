using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MiscUtils.IO;
using DLogger = MiscUtils.Logging.DebugLogger;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileInfoHeader
    {
        private const string Default = "";
        public ReadOnlyCollection<string> Properties { get; }

        public string Id => this.GetOrDefault(0);
        public string TypeId => this.Properties.Count == 3 ? this.GetOrDefault(1) : Default;
        public string Name => this.Properties.Count == 2 ? this.GetOrDefault(1) : this.GetOrDefault(2);

        private MS2FileInfoHeader(ReadOnlyCollection<string> properties)
        {
            this.Properties = properties;
        }

        internal static MS2FileInfoHeader Create(string id, MS2FileInfoHeader other)
        {
            string[] properties = new string[other.Properties.Count];
            other.Properties.CopyTo(properties, 0);
            properties[0] = id;

            return new MS2FileInfoHeader(Array.AsReadOnly(properties));
        }

        internal static async Task<MS2FileInfoHeader> Load(Stream stream)
        {
            using (var usr = new UnbufferedStreamReader(stream, true))
            {
                string line = await usr.ReadLineAsync().ConfigureAwait(false);
                string[] properties = line?.Split(',') ?? new string[0];
                ReadOnlyCollection<string> propertiesCollection = Array.AsReadOnly(properties);

                DLogger.Write(line);
                var result = new MS2FileInfoHeader(propertiesCollection);

                return result;
            }
        }

        internal Task Save(string filePath)
            => this.Save(File.OpenWrite(filePath));

        internal async Task Save(Stream stream)
        {
            using (var sw = new UnbufferedStreamWriter(stream, Encoding.ASCII, true))
            {
                string line = String.Join(",", this.Properties);

                await sw.WriteLineAsync(line);
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

        private string DebuggerDisplay
            => $"Count = {this.Properties.Count}";
    }
}
