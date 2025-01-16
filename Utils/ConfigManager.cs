using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using DevToolbox.Models;

namespace DevToolbox.Utils
{
    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevToolbox",
            "sshconfig.json"
        );

        public static List<SSHConfig> LoadConfigs()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonConvert.DeserializeObject<List<SSHConfig>>(json) ?? new List<SSHConfig>();
                }
            }
            catch (Exception)
            {
                // 如果读取失败，返回空列表
            }
            return new List<SSHConfig>();
        }

        public static void SaveConfigs(List<SSHConfig> configs)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // 处理保存失败的情况
            }
        }

        public static void SaveConfig(SSHConfig config)
        {
            var configs = LoadConfigs();
            var existing = configs.FirstOrDefault(c => 
                c.Host == config.Host && 
                c.Username == config.Username);

            if (existing != null)
            {
                existing.Password = config.Password;
                existing.Remark = config.Remark;
                existing.LastUsed = DateTime.Now;
            }
            else
            {
                config.LastUsed = DateTime.Now;
                configs.Add(config);
            }

            SaveConfigs(configs);
        }
    }
} 