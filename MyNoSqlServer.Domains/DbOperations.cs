using System;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.Common;
using MyNoSqlServer.Domains.DataSynchronization;
using MyNoSqlServer.Domains.Db.Tables;
using MyNoSqlServer.Domains.Json;
using MyNoSqlServer.Domains.Persistence;

namespace MyNoSqlServer.Domains
{
    public class DbOperations
    {
        private readonly IReplicaSynchronizationService _dataSynchronizer;
        private readonly PersistenceHandler _persistenceHandler;

        public DbOperations(IReplicaSynchronizationService dataSynchronizer, PersistenceHandler persistenceHandler)
        {
            _dataSynchronizer = dataSynchronizer;
            _persistenceHandler = persistenceHandler;
        }


        public ValueTask<OperationResult> InsertAsync(DbTable table, IMyMemory myMemory,
            DataSynchronizationPeriod synchronizationPeriod, DateTime now)
        {
            
            var entity = myMemory.ParseDynamicEntity();


            if (string.IsNullOrEmpty(entity.PartitionKey))
                return new ValueTask<OperationResult>(OperationResult.PartitionKeyIsNull);

            if (string.IsNullOrEmpty(entity.RowKey))
                return new ValueTask<OperationResult>(OperationResult.RowKeyIsNull);

            if (table.HasRecord(entity))
                return new ValueTask<OperationResult>(OperationResult.RecordExists);
            
            var (result, dbPartition, dbRow) = table.Insert(entity, now);
            
            if (result != OperationResult.Ok)
                return new ValueTask<OperationResult>(result);
            
            _dataSynchronizer.SynchronizeUpdate(table, new[] {dbRow});

            return _persistenceHandler.SynchronizePartitionAsync(table, dbPartition, synchronizationPeriod);
        }
        


        public ValueTask<OperationResult> InsertOrReplaceAsync(DbTable table, IMyMemory myMemory, 
            DataSynchronizationPeriod synchronizationPeriod, DateTime now)
        {
            var entity = myMemory.ParseDynamicEntity();

            if (string.IsNullOrEmpty(entity.PartitionKey))
                return new ValueTask<OperationResult>(OperationResult.PartitionKeyIsNull);

            if (string.IsNullOrEmpty(entity.RowKey))
                return new ValueTask<OperationResult>(OperationResult.RowKeyIsNull);
            
            var (dbPartition, dbRow) = table.InsertOrReplace(entity, now);
            
            _dataSynchronizer.SynchronizeUpdate(table, new[]{dbRow});

            return _persistenceHandler.SynchronizePartitionAsync(table, dbPartition, synchronizationPeriod);
            
        }
        
        public ValueTask<OperationResult> ReplaceAsync(DbTable table, IMyMemory myMemory, 
            DataSynchronizationPeriod synchronizationPeriod, DateTime now)
        {
            
            var entity = myMemory.ParseDynamicEntity();


            var (result, partition, dbRow) = table.Replace(entity, now);
            
            if (result != OperationResult.Ok)
                return new ValueTask<OperationResult>(result);
            
            _dataSynchronizer.SynchronizeUpdate(table, new[] {dbRow});

            return _persistenceHandler.SynchronizePartitionAsync(table, partition, synchronizationPeriod);
        }

        public ValueTask<OperationResult> MergeAsync(DbTable table, IMyMemory myMemory,
            DataSynchronizationPeriod synchronizationPeriod, DateTime now)
        {
            var entity = myMemory.ParseDynamicEntity();

            var (result, partition, dbRow) = table.Merge(entity, now);
            
            if (result != OperationResult.Ok)
                return new ValueTask<OperationResult>(result);
            
            _dataSynchronizer.SynchronizeUpdate(table, new[] {dbRow});

            return _persistenceHandler.SynchronizePartitionAsync(table, partition, synchronizationPeriod);

        }

        public ValueTask<OperationResult> DeleteAsync(DbTable table, string partitionKey, string rowKey, 
            DataSynchronizationPeriod synchronizationPeriod)
        {
            var (dbPartition, dbRow) = table.DeleteRow(partitionKey, rowKey);

            if (dbPartition == null) 
                return new ValueTask<OperationResult>(OperationResult.RowNotFound);
         
            _dataSynchronizer.SynchronizeDelete(table, new[]{dbRow});
            
            return _persistenceHandler.SynchronizeDeletePartitionAsync(table, dbPartition, synchronizationPeriod);
        }


        public ValueTask<OperationResult> CleanAndKeepLastRecordsAsync(DbTable table, string partitionKey, int amount, 
            DataSynchronizationPeriod synchronizationPeriod)
        {
            var (dbPartition, dbRows) = table.CleanAndKeepLastRecords(partitionKey, amount);

            if (dbPartition != null)
            {
                _dataSynchronizer.SynchronizeDelete(table, dbRows);
                
                return _persistenceHandler.SynchronizePartitionAsync(table, dbPartition, synchronizationPeriod);
            }
            
            return new ValueTask<OperationResult>(OperationResult.Ok);
        }
        
    }
}