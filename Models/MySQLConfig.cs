using System;
using Newtonsoft.Json;

namespace DevToolbox.Models
{
    public class MySQLConfig
    {
        public string ContainerId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime LastUsed { get; set; }
    }
} 