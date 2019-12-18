using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CosmosSim.DataGen
{
    class Program
    {
        static void Main()
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
            logger.LogInformation("generate records to insert/merge to CosmosDB");
            TelemetryClient telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();

            IRecordsGenerator generator = new RecordsGenerator();
            using (IRepository repository = new CosmosRepository(configuration, loggerFactory.CreateLogger<CosmosRepository>()))
            {
                experiment experiment = new experiment(generator, 10, loggerFactory.CreateLogger<experiment>(), repository);
                using (telemetryClient.StartOperation<RequestTelemetry>("generate records"))
                {
                    experiment.CreateReading1(0.1);
                    telemetryClient.TrackEvent("createReading1");
                    Console.WriteLine("press a key to merge");
                    Console.ReadKey();
                    experiment.MergeReading2(0.2);
                    telemetryClient.TrackEvent("mergeReading2");
                    Console.WriteLine("press a key to merge with stored proc");
                    Console.ReadKey();
                    experiment.MergeReading2StoredProc(0.3);
                    telemetryClient.TrackEvent("storedProcMerge");
                }
            }

            // Explicitly call Flush() followed by sleep is required in Console Apps.
            // This is to ensure that even if application terminates, telemetry is sent to the back-end.
            telemetryClient.Flush();
            Task.Delay(5000).Wait();
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
