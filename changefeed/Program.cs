using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CosmosSim.Changefeed
{
    class Program
    {
        static async Task Main()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            //Set up dependency injection 
            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            //Set up logger with dependency injection
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("listening to changefeed on CosmosDB");

            IStorage storageClient = new ADLSGen2Storage(configuration, logger);
            using IChangefeedRepository repository = new CosmosChangefeedRepository(configuration, logger, storageClient);
            await repository.StartProcessorAsync();
            logger.LogInformation("Change Feed Processor started.");
            Console.WriteLine("Press any key to stop listening on change feed...");
            Console.ReadKey();
            await repository.StopProcessorAsync();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection
                .AddApplicationInsightsTelemetryWorkerService(configuration["ApplicationInsights:InstrumentationKey"])
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"))
                        .AddConsole();
                });
        }
    }
}
