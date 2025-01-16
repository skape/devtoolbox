using System;
using Newtonsoft.Json;

namespace DevToolbox.Models
{
    public class SSHConfig
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Remark { get; set; }
        public DateTime LastUsed { get; set; }
    }
} 