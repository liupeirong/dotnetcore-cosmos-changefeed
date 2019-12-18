using Xunit;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CosmosSim.DataGen.Tests
{
    public class Experiement_Tests {
        private Mock<IRecordsGenerator> _mockRecordsGenerator;
        private Mock<IRepository> _mockRepository;
        private int _numReadings = 2;
        private experiment _sut; //system under test

        public Experiement_Tests() 
        {
            _mockRecordsGenerator = new Mock<IRecordsGenerator>();
            _mockRepository = new Mock<IRepository>();
            var mockLogger = new Mock<ILogger>();

            _sut = new experiment(_mockRecordsGenerator.Object, _numReadings, mockLogger.Object, _mockRepository.Object);
        }

        [Fact]
        public void GivenNumRecordsMarker_WhenGenerateReading1_ThenGenerateDeleteCreateSameNumRecordsMarker()
        {
        //Given
            double marker = 0.1;
            DailyDeviceReading[] records = new DailyDeviceReading[_numReadings];
            _mockRecordsGenerator.Setup(arg => arg.Generate(_numReadings, marker)).Returns(records.ToList());
        
        //When
            _sut.CreateReading1(marker);
        
        //Then
            _mockRecordsGenerator.Verify(arg => arg.Generate(It.Is<int>(i => i == _numReadings), It.Is<double>(d => d == marker)), Times.Once());
            //_mockRepository.Verify(arg => arg.DeleteRecords(It.Is<List<DailyDeviceReading>>(o => o.Count == _numReadings)), Times.Never());
            _mockRepository.Verify(arg => arg.DeleteRecords(It.Is<List<DailyDeviceReading>>(o => o.Count == _numReadings)), Times.Once());
            _mockRepository.Verify(arg => arg.CreateRecords(It.Is<List<DailyDeviceReading>>(o => o.Count == _numReadings)), Times.Once());
        }
    }
}