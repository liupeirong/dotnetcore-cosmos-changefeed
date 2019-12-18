using System;
using Xunit;
using CosmosSim.DataGen;

namespace CosmosSim.Changefeed.Tests
{
    public class Storage_Tests
    {
        private readonly ADLSGen2Storage _sut; //System under test

        public Storage_Tests()
        {
            _sut = new ADLSGen2Storage();
        }

        [Fact]
        public void GivenReading20191217_WhenGetPath_ThenPathPartitioned()
        {
            DailyDeviceReading record = new DailyDeviceReading
            {
                deviceId = "foo"
            };
            DateTime readAt = new DateTime(2019, 12, 17);
            record.readAt = (long) readAt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            Assert.True(_sut.GetPathForRecord(record).Equals("devicesim/year=2019/month=12/day=17/foo"));
        }
    }
}
