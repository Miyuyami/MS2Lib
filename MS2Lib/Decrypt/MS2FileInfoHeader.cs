using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MiscUtils.IO;
using DLogger = MiscUtils.Logging.DebugLogger;

namespace MS2Lib.Decrypt
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

        public static async Task<MS2FileInfoHeader> Create(Stream stream)
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
