using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CosmosSim.DataGen
{
    public class experiment
    {
        private readonly int _numReadings;
        private readonly IRecordsGenerator _generator;
        private readonly ILogger _logger;
        private readonly IRepository _repository;

        public experiment(IRecordsGenerator generator, int numReadings, ILogger logger, IRepository repository)
        {
            _generator = generator;
            _numReadings = numReadings;
            _logger = logger;
            _repository = repository;
        }

        public void CreateReading1(double marker)
        {
            List<DailyDeviceReading> readings1 = _generator.Generate(_numReadings, marker);
            _repository.DeleteRecords(readings1);

            _logger.LogInformation("Start UTC: " + DateTime.UtcNow);

            _repository.CreateRecords(readings1);
        }

        public void MergeReading2(double marker)
        {
            List<DailyDeviceReading> readings2 = _generator.Generate(_numReadings, marker);
            List<DailyDeviceReading> readings1 = _repository.ReadRecords(readings2);
            List<DailyDeviceReading> merged = new List<DailyDeviceReading>();
            int cnt = readings2.Count;
            for (int ii = 0; ii < cnt; ++ii)
            {
                DailyDeviceReading mr = _generator.Merge(readings1[ii], readings2[ii]);
                merged.Add(mr);
            }

            _repository.UpdateRecords(merged);
        }

        public void MergeReading2StoredProc(double marker)
        {
            List<DailyDeviceReading> readings2 = _generator.Generate(_numReadings, marker);
            _repository.MergeRecordsStoredProc(readings2);
        }
    }
}
