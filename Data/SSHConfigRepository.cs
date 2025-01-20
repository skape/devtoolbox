using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DevToolbox.Models;

namespace DevToolbox.Data
{
    public class SSHConfigRepository
    {
        private readonly DatabaseContext _context;

        public SSHConfigRepository()
        {
            _context = new DatabaseContext();
        }

        public List<SSHConfig> GetAll()
        {
            using var connection = _context.CreateConnection();
            return connection.Query<SSHConfig>(@"
                SELECT * FROM SSHConfigs 
                ORDER BY LastUsed DESC
            ").ToList();
        }

        public SSHConfig GetById(int id)
        {
            using var connection = _context.CreateConnection();
            return connection.QueryFirstOrDefault<SSHConfig>(
                "SELECT * FROM SSHConfigs WHERE Id = @Id",
                new { Id = id }
            );
        }

        public SSHConfig GetByName(string name)
        {
            using var connection = _context.CreateConnection();
            return connection.QueryFirstOrDefault<SSHConfig>(
                "SELECT * FROM SSHConfigs WHERE Name = @Name",
                new { Name = name }
            );
        }

        public void Add(SSHConfig config)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                INSERT INTO SSHConfigs (Name, Host, Port, Username, Password, LastUsed)
                VALUES (@Name, @Host, @Port, @Username, @Password, @LastUsed)
            ", config);
        }

        public void Update(SSHConfig config)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                UPDATE SSHConfigs 
                SET Name = @Name,
                    Host = @Host,
                    Port = @Port,
                    Username = @Username,
                    Password = @Password,
                    LastUsed = @LastUsed
                WHERE Id = @Id
            ", config);
        }

        public void Delete(int id)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(
                "DELETE FROM SSHConfigs WHERE Id = @Id",
                new { Id = id }
            );
        }

        public void UpdateLastUsed(int id)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                UPDATE SSHConfigs 
                SET LastUsed = @LastUsed
                WHERE Id = @Id
            ", new { Id = id, LastUsed = DateTime.Now });
        }
    }
} 