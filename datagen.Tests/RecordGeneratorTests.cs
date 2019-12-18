using System;
using Xunit;

namespace CosmosSim.DataGen.Tests
{
    public class RecordGenerator_Tests
    {
        private RecordsGenerator _sut; //System Under Test

        public RecordGenerator_Tests()
        {
            _sut = new RecordsGenerator();
        }

        [Fact]
        public void GivenNumRecords1_WhenGenerate_ThenReturnListOf1()
        {
            var result = _sut.Generate(1, 0.1);
            Assert.True(result.Count == 1);
        }
    }
}
