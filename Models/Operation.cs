using System;

namespace DevToolbox.Models
{
    public class Operation
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public string OpName { get; set; }
        public int OpType { get; set; }  // 0:本地执行 1:服务器执行 2:docker内执行
        public string Cmd { get; set; }
        public DateTime CreateTime { get; set; }
    }
} 