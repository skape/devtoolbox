using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DevToolbox.Models;

namespace DevToolbox.Data
{
    public class DeployRepository
    {
        private readonly DatabaseContext _context;

        public DeployRepository()
        {
            _context = new DatabaseContext();
        }

        public List<Deploy> GetAll()
        {
            using var connection = _context.CreateConnection();
            return connection.Query<Deploy>(@"
                SELECT * FROM Deploys 
                ORDER BY CreateTime DESC
            ").ToList();
        }

        public Deploy GetById(int id)
        {
            using var connection = _context.CreateConnection();
            return connection.QueryFirstOrDefault<Deploy>(
                "SELECT * FROM Deploys WHERE Id = @Id",
                new { Id = id }
            );
        }

        public List<Deploy> GetByContainerId(string containerId)
        {
            using var connection = _context.CreateConnection();
            return connection.Query<Deploy>(
                "SELECT * FROM Deploys WHERE ContainerId = @ContainerId ORDER BY Sort",
                new { ContainerId = containerId }
            ).ToList();
        }

        public List<Deploy> GetByOpId(int opId)
        {
            using var connection = _context.CreateConnection();
            return connection.Query<Deploy>(
                "SELECT * FROM Deploys WHERE OpId = @OpId ORDER BY Sort",
                new { OpId = opId }
            ).ToList();
        }

        public void Add(Deploy deploy)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                INSERT INTO Deploys (OpId, ContainerId, Name, OpName, Sort, ParamJson)
                VALUES (@OpId, @ContainerId, @Name, @OpName, @Sort, @ParamJson)
            ", deploy);
        }

        public void Update(Deploy deploy)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(@"
                UPDATE Deploys 
                SET OpId = @OpId,
                    ContainerId = @ContainerId,
                    Name = @Name,
                    OpName = @OpName,
                    Sort = @Sort,
                    ParamJson = @ParamJson
                WHERE Id = @Id
            ", deploy);
        }

        public void Delete(int id)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(
                "DELETE FROM Deploys WHERE Id = @Id",
                new { Id = id }
            );
        }

        public void DeleteByOpId(int opId)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(
                "DELETE FROM Deploys WHERE OpId = @OpId",
                new { OpId = opId }
            );
        }

        public void DeleteByContainerId(string containerId)
        {
            using var connection = _context.CreateConnection();
            connection.Execute(
                "DELETE FROM Deploys WHERE ContainerId = @ContainerId",
                new { ContainerId = containerId }
            );
        }

        public Deploy GetWithOperation(int id)
        {
            using var connection = _context.CreateConnection();
            var sql = @"
                SELECT d.*, o.* 
                FROM Deploys d
                LEFT JOIN Operations o ON d.OpId = o.Id
                WHERE d.Id = @Id";

            var result = connection.Query<Deploy, Operation, Deploy>(
                sql,
                (deploy, operation) =>
                {
                    deploy.Operation = operation;
                    return deploy;
                },
                new { Id = id },
                splitOn: "Id"
            ).FirstOrDefault();

            return result;
        }

        public List<Deploy> GetAllWithOperations()
        {
            using var connection = _context.CreateConnection();
            var sql = @"
                SELECT d.*, o.* 
                FROM Deploys d
                LEFT JOIN Operations o ON d.OpId = o.Id
                ORDER BY d.CreateTime DESC";

            var result = connection.Query<Deploy, Operation, Deploy>(
                sql,
                (deploy, operation) =>
                {
                    deploy.Operation = operation;
                    return deploy;
                },
                splitOn: "Id"
            ).ToList();

            return result;
        }
    }
} 