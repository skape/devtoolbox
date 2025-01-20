using System;

namespace DevToolbox.Models
{
    public class SSHConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime? LastUsed { get; set; }

        public SSHConfig()
        {
            Port = 22;  // 默认SSH端口
            LastUsed = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Name} ({Host}:{Port})";
        }
    }
} 