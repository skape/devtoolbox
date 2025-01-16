using System.Text;
using Renci.SshNet;

namespace DevToolbox.Utils
{
    public static class StreamExtensions
    {
        public static string Read(this ShellStream shellStream)
        {
            var result = new StringBuilder();
            var buffer = new byte[1024];
            while (shellStream.DataAvailable)
            {
                var read = shellStream.Read(buffer, 0, buffer.Length);
                result.Append(Encoding.UTF8.GetString(buffer, 0, read));
            }
            return result.ToString();
        }
    }
} 