using System;

namespace DevToolbox.Models
{
    public class Deploy
    {
        public int Id { get; set; }
        public int OpId { get; set; }
        public string ContainerId { get; set; }
        public string Name { get; set; }
        public string OpName { get; set; }
        public string CmdPath { get; set; }
        public int Sort { get; set; }
        public DateTime CreateTime { get; set; }

        // 导航属性
        public Operation Operation { get; set; }
    }
} 