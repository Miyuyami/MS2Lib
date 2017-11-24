using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MiscUtils.IO;
//using DLogger = MiscUtils.Logging.DebugLogger;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileInfoHeader
    {
        public ReadOnlyCollection<string> Properties { get; }

        public string Id => this.Properties[0];
        public string TypeId => this.Properties.Count == 3 ? this.Properties[1] : null;
        public string Name => this.Properties.Count == 2 ? this.Properties[1] : this.Properties[2];

        private MS2FileInfoHeader(ReadOnlyCollection<string> properties)
        {
            this.Properties = properties;
        }

        public static async Task<MS2FileInfoHeader> Create(Stream stream)
        {
            using (var usr = new UnbufferedStreamReader(stream, true))
            {
                string line = await usr.ReadLineAsync().ConfigureAwait(false);
                string[] properties = line.Split(',');
                ReadOnlyCollection<string> propertiesCollection = Array.AsReadOnly(properties);

                //DLogger.Write(line);
                //DLogger.Write($"Number of properties: [{propertiesCollection.Count}]");
                var result = new MS2FileInfoHeader(propertiesCollection);
                //DLogger.Write($"id=[{result.Id}]; type=[{result.TypeId}]; name=[{result.Name}]");

                return result;
            }
        }

        private string DebuggerDisplay
            => $"Count = {this.Properties.Count}";
    }
}
