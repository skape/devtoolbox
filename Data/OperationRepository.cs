using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DevToolbox.Models;

namespace DevToolbox.Data
{
    public class OperationRepository
    {
        private readonly DatabaseContext _context;

        public OperationRepository()
        {
            _context = new DatabaseContext();
        }

        public List<Operation> GetAll()
        {
            using var connection = _context.CreateConnection();
            return connection.Query<Operation>(@"
                SELECT * FROM Operations 
                ORDER BY CreateTime DESC
            ").ToList();
        }

        public Operation GetById(int id)
        {
            using var connection = _context.CreateConnection();
            return connection.QueryFirstOrDefault<Operation>(
                "SELECT * FROM Operations WHERE Id = @Id",
                new { Id = id }
            );
        }

        public List<Operation> GetByHost(string host)
        {
            using var connection = _context.CreateConnection();
            return connection.Query<Operation>(
                "SELECT * FROM Operations WHERE Host = @Host ORDER BY CreateTime DESC",
                new { Host = host }
            ).ToList();
        }

        public void Add(Operation operation)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                INSERT INTO Operations (Host, OpName, BeforeCmd, LocalPath, RemotePath, DeployedCmd, MyFunction)
                VALUES (@Host, @OpName, @BeforeCmd, @LocalPath, @RemotePath, @DeployedCmd, @MyFunction)
            ", operation);
        }

        public void Update(Operation operation)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                UPDATE Operations 
                SET Host = @Host,
                    OpName = @OpName,
                    BeforeCmd = @BeforeCmd,
                    LocalPath = @LocalPath,
                    RemotePath = @RemotePath,
                    DeployedCmd = @DeployedCmd,
                    MyFunction = @MyFunction
                WHERE Id = @Id
            ", operation);
        }

        public void Delete(int id)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(
                "DELETE FROM Operations WHERE Id = @Id",
                new { Id = id }
            );
        }
    }
} 