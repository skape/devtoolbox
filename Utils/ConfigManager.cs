using System;
using System.Collections.Generic;
using System.IO;
using DevToolbox.Models;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Linq;

namespace DevToolbox.Utils
{
    public static class ConfigManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevToolbox"
        );

        private static readonly string MySQLConfigPath = Path.Combine(AppDataPath, "mysql_config.json");
        private static readonly string SSHConfigPath = Path.Combine(AppDataPath, "ssh_config.json");
        private static readonly string DeployConfigPath = Path.Combine(AppDataPath, "deploy_configs.json");

        static ConfigManager()
        {
            // 确保配置目录存在
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }
        }

        public static Dictionary<string, MySQLConfig> LoadMySQLConfigs()
        {
            if (!File.Exists(MySQLConfigPath))
            {
                return new Dictionary<string, MySQLConfig>();
            }

            try
            {
                var json = File.ReadAllText(MySQLConfigPath);
                return JsonConvert.DeserializeObject<Dictionary<string, MySQLConfig>>(json) 
                    ?? new Dictionary<string, MySQLConfig>();
            }
            catch
            {
                return new Dictionary<string, MySQLConfig>();
            }
        }

        public static void SaveMySQLConfig(MySQLConfig config)
        {
            var configs = LoadMySQLConfigs();
            configs[config.ContainerId] = config;

            var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            File.WriteAllText(MySQLConfigPath, json);
        }

        public static List<SSHConfig> LoadSSHConfigs()
        {
            if (!File.Exists(SSHConfigPath))
            {
                return new List<SSHConfig>();
            }

            try
            {
                var json = File.ReadAllText(SSHConfigPath);
                return JsonConvert.DeserializeObject<List<SSHConfig>>(json) 
                    ?? new List<SSHConfig>();
            }
            catch
            {
                return new List<SSHConfig>();
            }
        }

        public static void SaveSSHConfig(SSHConfig config)
        {
            var configs = LoadSSHConfigs();
            
            // 检查是否已存在相同名称的配置
            var existingIndex = configs.FindIndex(c => c.Name == config.Name);
            if (existingIndex >= 0)
            {
                configs[existingIndex] = config;  // 更新现有配置
            }
            else
            {
                configs.Add(config);  // 添加新配置
            }

            var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            File.WriteAllText(SSHConfigPath, json);
        }

        public static void DeleteSSHConfig(string name)
        {
            var configs = LoadSSHConfigs();
            configs.RemoveAll(c => c.Name == name);

            var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            File.WriteAllText(SSHConfigPath, json);
        }

        public static List<DeployConfig> LoadDeployConfigs()
        {
            try
            {
                if (File.Exists(DeployConfigPath))
                {
                    var json = File.ReadAllText(DeployConfigPath);
                    return JsonConvert.DeserializeObject<List<DeployConfig>>(json) ?? new List<DeployConfig>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载部署配置失败: {ex.Message}");
            }
            return new List<DeployConfig>();
        }

        public static DeployConfig LoadLastDeployConfig(string containerId, string environment)
        {
            try
            {
                var configs = LoadDeployConfigs();
                return configs.FirstOrDefault(c => 
                    c.ContainerId == containerId && 
                    c.Environment == environment);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载部署配置失败: {ex.Message}");
            }
            return null;
        }

        public static void SaveDeployConfig(DeployConfig config)
        {
            try
            {
                var configs = LoadDeployConfigs();
                
                // 查找并更新现有配置
                var existingConfig = configs.FirstOrDefault(c => 
                    c.ContainerId == config.ContainerId && 
                    c.Environment == config.Environment);

                if (existingConfig != null)
                {
                    configs.Remove(existingConfig);
                }
                configs.Add(config);

                var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
                File.WriteAllText(DeployConfigPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存部署配置失败: {ex.Message}");
            }
        }
    }
} 