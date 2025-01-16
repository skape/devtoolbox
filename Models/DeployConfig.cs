using System;

namespace DevToolbox.Models
{
    public class DeployConfig
    {
        public string ContainerId { get; set; }
        public string ContainerName { get; set; }
        public string Environment { get; set; }  // "dev" or "prod"
        public string ServerName { get; set; }   // 目标服务器名称
        public string ServerHost { get; set; }   // 目标服务器地址
        public int ServerPort { get; set; }      // 目标服务器端口
        public string LocalPath { get; set; }    // 本地目录
        public string RemotePath { get; set; }   // 远程目录
        public DateTime LastUsed { get; set; }   // 最后使用时间
    }
} 