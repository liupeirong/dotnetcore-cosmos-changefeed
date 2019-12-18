using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using CosmosSim.DataGen;

namespace CosmosSim.Changefeed
{
    public interface IChangefeedRepository : IDisposable
    {
        Task StartProcessorAsync();
        Task StopProcessorAsync();
        Task HandleChangesAsync(IReadOnlyCollection<DailyDeviceReading> changes, CancellationToken cancellationToken);
    }

    public class CosmosChangefeedRepository : IChangefeedRepository
    {
        CosmosClient _cosmosClient;
        Container _container;
        Container _leaseContainer;
        ChangeFeedProcessor _changeFeedProcessor;
        readonly IStorage _storageClient;
        readonly ILogger _logger;

        public CosmosChangefeedRepository(ILogger logger, IStorage storageClient) //for testing
        {
            _logger = logger;
            _storageClient = storageClient;
        }

        public CosmosChangefeedRepository(IConfiguration configuration, ILogger logger, IStorage storageClient)
        {
            _storageClient = storageClient;
            _logger = logger;
            InitCosmosFromConfiguration(configuration);
        }

        public async Task StartProcessorAsync()
        {
            _changeFeedProcessor = _container
                .GetChangeFeedProcessorBuilder<DailyDeviceReading>("DeviceSimChangeFeedProc", HandleChangesAsync)
                .WithInstanceName("consoleHost1")
                .WithLeaseContainer(_leaseContainer)
                //.WithStartTime(particularPointInTime)
                .Build();

            await _changeFeedProcessor.StartAsync();
        }

        public async Task StopProcessorAsync()
        {
            await _changeFeedProcessor.StopAsync();
        }

        public async Task HandleChangesAsync(IReadOnlyCollection<DailyDeviceReading> changes, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach (DailyDeviceReading item in changes)
            {
                _logger.LogInformation($"Detected operation for item with id {item.deviceId}.");
                tasks.Add(_storageClient.SaveOverwriteIfExistsAsync(item));
            }

            await Task.WhenAll(tasks);
        }

        private void InitCosmosFromConfiguration(IConfiguration configuration)
        {
            string cosmosDB_Endpoint = configuration["CosmosDB_Endpoint"];
            string cosmosDB_Database = configuration["CosmosDB_Database"];
            string cosmosDB_Container = configuration["CosmosDB_Container"];
            string cosmosDB_LeaseContainer = configuration["CosmosDB_LeaseContainer"];

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
            _container = _cosmosClient.GetContainer(cosmosDB_Database, cosmosDB_Container);
            _leaseContainer = _cosmosClient.GetContainer(cosmosDB_Database, cosmosDB_LeaseContainer);
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