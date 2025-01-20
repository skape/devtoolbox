using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace DevToolbox.Data
{
    public class DatabaseContext
    {
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "devtools.db"
        );

        public DatabaseContext()
        {
            var directory = Path.GetDirectoryName(DbPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(DbPath))
            {
                InitializeDatabase();
                InitializeDefaultOperations();
            }
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={DbPath}");
        }

        private void InitializeDatabase()
        {
            using var connection = CreateConnection();
            connection.Open();

            // 创建SSH配置表
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SSHConfigs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Host TEXT NOT NULL,
                    Port INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    Password TEXT NOT NULL,
                    LastUsed TEXT
                );

                CREATE TABLE IF NOT EXISTS Operations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Host TEXT NOT NULL,
                    OpName TEXT NOT NULL,
                    OpType INTEGER NOT NULL DEFAULT 0,  -- 0:本地执行 1:服务器执行 2:docker内执行
                    Cmd TEXT,
                    CreateTime TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Deploys (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OpId INTEGER NOT NULL,
                    ContainerId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    OpName TEXT NOT NULL,
                    CmdPath TEXT,
                    Sort INTEGER NOT NULL DEFAULT 0,
                    CreateTime TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(OpId) REFERENCES Operations(Id)
                )";
            command.ExecuteNonQuery();
        }

        private void InitializeDefaultOperations()
        {
            using var connection = CreateConnection();
            connection.Open();

            // 基础操作 - 本地执行
            var basicOps = new[]
            {
                new { OpName = "启动", Cmd = "docker start {containerId}", OpType = 0 },
                new { OpName = "停止", Cmd = "docker stop {containerId}", OpType = 0 },
                new { OpName = "重启", Cmd = "docker restart {containerId}", OpType = 0 }
            };

            // 打包操作 - docker内执行
            var buildOps = new[]
            {
                new { OpName = "打包Dev", Cmd = "docker exec {containerId} yarn build:dev", OpType = 2 },
                new { OpName = "打包Prod", Cmd = "docker exec {containerId} yarn build:prod", OpType = 2 }
            };

            // 部署操作 - 服务器执行
            var deployOps = new[]
            {
                new { OpName = "开发环境部署", Cmd = "cd {cmdPath} && sh deploy.sh", OpType = 1 },
                new { OpName = "生产环境部署", Cmd = "cd {cmdPath} && sh deploy.sh", OpType = 1 }
            };

            // 组合操作 - 混合执行
            var combinedOps = new[]
            {
                new { OpName = "开发环境打包及部署", Cmd = "docker exec {containerId} yarn build:dev", OpType = 2 },
                new { OpName = "生产环境打包及部署", Cmd = "docker exec {containerId} yarn build:prod", OpType = 2 }
            };

            // 查看操作 - docker内执行
            var viewOps = new[]
            {
                new { OpName = "查看日志", Cmd = "docker logs {containerId}", OpType = 2 },
                new { OpName = "查看详细信息", Cmd = "docker inspect {containerId}", OpType = 2 }
            };

            using var command = connection.CreateCommand();
            foreach (var op in basicOps.Concat(buildOps).Concat(deployOps).Concat(combinedOps).Concat(viewOps))
            {
                command.CommandText = @"
                    INSERT INTO Operations (Host, OpName, OpType, Cmd)
                    VALUES ('*', @OpName, @OpType, @Cmd)";

                command.Parameters.Clear();
                command.Parameters.AddWithValue("@OpName", op.OpName);
                command.Parameters.AddWithValue("@OpType", op.OpType);
                command.Parameters.AddWithValue("@Cmd", op.Cmd ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
            }
        }
    }
}