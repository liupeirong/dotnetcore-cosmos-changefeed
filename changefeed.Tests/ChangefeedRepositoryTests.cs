using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using CosmosSim.DataGen;

namespace CosmosSim.Changefeed.Tests
{
    public class ChangefeedRepository_Tests
    {
        private readonly Mock<IStorage> _mockStorage;
        private readonly CosmosChangefeedRepository _sut; //System under test

        public ChangefeedRepository_Tests()
        {
            var mockLogger = new Mock<ILogger>();
            _mockStorage = new Mock<IStorage>();

            _sut = new CosmosChangefeedRepository(mockLogger.Object, _mockStorage.Object);
        }

        [Fact]
        public async void GivenChangedRecord_WhenHandleChangesAsync_ThenCallStorage()
        {
            //Given
            DailyDeviceReading record = new DailyDeviceReading
            {
                deviceId = "foo"
            };
            DateTime readAt = new DateTime(2019, 12, 17);
            record.readAt = (long) readAt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            DailyDeviceReading [] records = new DailyDeviceReading[] {record};
            CancellationToken cancellationToken = new CancellationToken();
            Task task = Task.CompletedTask;
            _mockStorage.Setup(arg => arg.SaveOverwriteIfExistsAsync(record)).Returns(task);

            //When
            await _sut.HandleChangesAsync(records, cancellationToken);

            //Then
            _mockStorage.Verify(arg => arg.SaveOverwriteIfExistsAsync(record), Times.Once());
        }
    }
}
