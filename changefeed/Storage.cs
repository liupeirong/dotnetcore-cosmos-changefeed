using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CosmosSim.DataGen;

namespace CosmosSim.Changefeed
{
    public interface IStorage
    {
        Task SaveOverwriteIfExistsAsync(DailyDeviceReading record);
        string GetPathForRecord(DailyDeviceReading record);
    }

    public class ADLSGen2Storage : IStorage
    {
        private readonly ILogger _logger;
        private readonly BlobContainerClient _adlsClient;

        public ADLSGen2Storage() //for testing
        {}

        public ADLSGen2Storage(IConfiguration configuration, ILogger logger)
        {
            Uri uri = new Uri(configuration["ADLS_Uri"]);
            string fs = configuration["ADLS_FileSystem"];

            BlobServiceClient blobServiceClient = new BlobServiceClient(uri, new DefaultAzureCredential());
            _adlsClient = blobServiceClient.GetBlobContainerClient(fs);
            _logger = logger;
        }

        public async Task SaveOverwriteIfExistsAsync(DailyDeviceReading record)
        {
            string path = GetPathForRecord(record);
            _logger.LogInformation("Writing to ADLS as file:\n\t {0}\n", path);
            BlobClient fileClient = _adlsClient.GetBlobClient(path);
            string recordJson = JsonConvert.SerializeObject(record);
            byte[] byteArray = Encoding.UTF8.GetBytes(recordJson);
            using MemoryStream stream = new MemoryStream(byteArray);
            await fileClient.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
            await fileClient.UploadAsync(stream);
        }

        public string GetPathForRecord(DailyDeviceReading record)
        {
            DateTime dt = UnixTimeStampToDateTime(record.readAt);
            string filePath = String.Format("devicesim/year={0}/month={1}/day={2}/{3}", dt.Year, dt.Month, dt.Day, record.deviceId);
            return filePath;
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dtDateTime;
        }
    }
}