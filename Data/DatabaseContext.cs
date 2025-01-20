using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace DevToolbox.Data
{
    public class DatabaseContext
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevToolbox",
            "devtools.db"
        );

        public DatabaseContext()
        {
            var directory = Path.GetDirectoryName(DbPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(DbPath))
                InitializeDatabase();
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
                )";
            command.ExecuteNonQuery();
        }
    }
} 