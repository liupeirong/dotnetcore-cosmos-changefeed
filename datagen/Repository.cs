using Azure.Cosmos;
using Azure.Cosmos.Scripts;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosSim.DataGen
{
    public interface IRepository : IDisposable
    {
        void DeleteRecords(List<DailyDeviceReading> records);

        void CreateRecords(List<DailyDeviceReading> records);

        List<DailyDeviceReading> ReadRecords(List<DailyDeviceReading> records);

        void UpdateRecords(List<DailyDeviceReading> records);

        void MergeRecordsStoredProc(List<DailyDeviceReading> records);
    }

    public class CosmosRepository : IRepository
    {
        private CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly ILogger _logger;

        public CosmosRepository(IConfiguration configuration, ILogger logger)
        {
            _container = GetContainerFromConfiguration(configuration);
            _logger = logger;
        }

        public void DeleteRecords(List<DailyDeviceReading> records)
        {
            List<Task> tasks = new List<Task>();

            foreach (DailyDeviceReading record in records)
            {
                tasks.Add(_container.DeleteItemAsync<DailyDeviceReading>(record.deviceId, new PartitionKey(record.tag)));
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException)
            {
            }

            foreach(Task<ItemResponse<DailyDeviceReading>> task in tasks)
            {
                if (task.IsFaulted)
                {
                    _logger.LogWarning(task.Exception.Message);
                }
            }
        }

        public void CreateRecords(List<DailyDeviceReading> records)
        {
            List<Task> tasks = new List<Task>();
            foreach (DailyDeviceReading record in records)
            {
                //tasks.Add(_container.UpsertItemAsync<DailyDeviceReading>(record, new PartitionKey(record.tag)));
                tasks.Add(_container.CreateItemAsync<DailyDeviceReading>(record, new PartitionKey(record.tag)));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (Task<ItemResponse<DailyDeviceReading>> task in tasks)
            {
                ItemResponse<DailyDeviceReading> response = task.Result;
                response.GetRawResponse().Headers.TryGetValue("x-ms-request-charge", out string requestCharge);
                _logger.LogInformation("Create Reading Request Charge:" + requestCharge);
            }
        }

        public List<DailyDeviceReading> ReadRecords(List<DailyDeviceReading> records)
        {
            List<DailyDeviceReading> recordsRead = new List<DailyDeviceReading>();
            List<Task> tasks = new List<Task>();

            foreach (DailyDeviceReading record in records)
            {
                tasks.Add(_container.ReadItemAsync<DailyDeviceReading>(record.deviceId, new PartitionKey(record.tag)));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (Task<ItemResponse<DailyDeviceReading>> task in tasks)
            {
                ItemResponse<DailyDeviceReading> response = task.Result;
                recordsRead.Add(response.Value);
                response.GetRawResponse().Headers.TryGetValue("x-ms-request-charge", out string requestCharge);
                _logger.LogInformation("Read back Reading1 Request Charge:" + requestCharge);
            }

            return recordsRead;
        }

        public void UpdateRecords(List<DailyDeviceReading> records)
        {
            List<Task> tasks = new List<Task>();

            foreach (DailyDeviceReading record in records)
            {
                tasks.Add(_container.ReplaceItemAsync<DailyDeviceReading>(record, record.deviceId, new PartitionKey(record.tag)));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (Task<ItemResponse<DailyDeviceReading>> task in tasks)
            {
                ItemResponse<DailyDeviceReading> response = task.Result;
                response.GetRawResponse().Headers.TryGetValue("x-ms-request-charge", out string requestCharge);
                _logger.LogInformation("Replace Reading1 Request Charge:" + requestCharge);
            }
        }

        public void MergeRecordsStoredProc(List<DailyDeviceReading> records)
        {
            List<Task> tasks = new List<Task>();

            foreach (DailyDeviceReading record in records)
            {
                tasks.Add(_container.Scripts.ExecuteStoredProcedureAsync<string>(
                            "spMerge",
                            new PartitionKey(record.tag),
                            new dynamic[] { record.deviceId, JsonConvert.SerializeObject(record) }));
            }

            Task.WaitAll(tasks.ToArray());

            foreach(Task<StoredProcedureExecuteResponse<string>> task in tasks)
            {
                StoredProcedureExecuteResponse<string> response = task.Result;
                response.GetRawResponse().Headers.TryGetValue("x-ms-request-charge", out string requestCharge);
                _logger.LogInformation("Total Merge Stored Proc Request Charge:" + requestCharge);
            }
        }

        private Container GetContainerFromConfiguration(IConfiguration configuration)
        {
            string cosmosDB_Endpoint = configuration["CosmosDB_Endpoint"];
            string cosmosDB_Database = configuration["CosmosDB_Database"];
            string cosmosDB_Container = configuration["CosmosDB_Container"];

            string cosmosDB_KeyName = configuration["CosmosDB_KeyName"];
            string keyVaultURL = configuration["KeyVault_URL"];
            var kvClient = new SecretClient(new Uri(keyVaultURL), new DefaultAzureCredential());
            string cosmosDB_Key = kvClient.GetSecret(cosmosDB_KeyName).Value.Value;

            CosmosSerializationOptions ignoreNullSerialization = new CosmosSerializationOptions
            {
                IgnoreNullValues = true
            };
            CosmosClientOptions cosmosDB_Options = new CosmosClientOptions
            {
                SerializerOptions = ignoreNullSerialization
            };

            _cosmosClient = new CosmosClient(cosmosDB_Endpoint, cosmosDB_Key, cosmosDB_Options);
            return _cosmosClient.GetContainer(cosmosDB_Database, cosmosDB_Container);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_cosmosClient != null)
            {
                _cosmosClient.Dispose();
                _cosmosClient = null;
            }
        }
    }

}
