using System;

namespace DevToolbox.Models
{
    public class JavaDeployConfig
    {
        public string ProjectPath { get; set; }        // Java项目路径
        public string ServerName { get; set; }         // 目标服务器名称
        public string ServerHost { get; set; }         // 目标服务器地址
        public int ServerPort { get; set; }           // 目标服务器端口
        public string RemotePath { get; set; }        // 远程目录
        public string DeployScript { get; set; }      // 部署脚本
        public DateTime LastUsed { get; set; }        // 最后使用时间
    }
} 