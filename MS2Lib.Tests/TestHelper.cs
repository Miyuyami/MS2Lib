using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MS2Lib.Tests
{
    public static class TestHelper
    {
        public static readonly Encoding EncodingTest = Encoding.UTF8;

        public static MemoryStream StringToStream(string s)
        {
            byte[] bytes = EncodingTest.GetBytes(s);
            return new MemoryStream(bytes);
        }

        public static async Task<string> StreamToString(Stream s)
        {
            var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            var actualBytes = ms.ToArray();
            return EncodingTest.GetString(actualBytes);
        }
    }
}
