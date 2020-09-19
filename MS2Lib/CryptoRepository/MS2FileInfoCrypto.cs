using System;
using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public sealed class MS2FileInfoCrypto : IMS2FileInfoCrypto
    {
        private const char PropertySeparator = ',';

        public async Task<IMS2FileInfo> ReadAsync(TextReader textReader)
        {
            string line = await textReader.ReadLineAsync().ConfigureAwait(false);
            string[] properties = line.Split(PropertySeparator, StringSplitOptions.RemoveEmptyEntries);
            string id = properties[0];
            string path = GetPathFromProperties(properties);
            IMS2FileInfo fileInfo = new MS2FileInfo(id, path);

            return fileInfo;
        }

        public Task WriteAsync(TextWriter textWriter, IMS2FileInfo fileInfo)
        {
            if (String.IsNullOrWhiteSpace(fileInfo.RootFolderId))
            {
                return textWriter.WriteLineAsync(String.Join(PropertySeparator, fileInfo.Id, fileInfo.Path));
            }
            else
            {
                return textWriter.WriteLineAsync(String.Join(PropertySeparator, fileInfo.Id, fileInfo.RootFolderId, fileInfo.Path));
            }
        }

        private static string GetPathFromProperties(string[] properties)
        {
            if (properties.Length == 3)
            {
                return properties[2];
            }
            else if (properties.Length == 2)
            {
                return properties[1];
            }
            else
            {
                throw new ArgumentException(nameof(properties), $"Unrecognised number of properties [{properties.Length}].");
            }
        }
    }
}
