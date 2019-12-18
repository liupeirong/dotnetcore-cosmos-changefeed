using System.Collections.Generic;

namespace CosmosSim.DataGen
{
    public interface IRecordsGenerator
    {
        List<DailyDeviceReading> Generate(int numReadings, double val);
        DailyDeviceReading Merge(DailyDeviceReading reading1, DailyDeviceReading reading2);
    }

    public class RecordsGenerator : IRecordsGenerator
    {
        private const string BaseId = "00000-0000-0000-0000-000000000000";
        private const string BaseTag = "tag";
        private const long BaseReadAt = 1548979200;

        public List<DailyDeviceReading> Generate(int numReadings, double val)
        {
            List<DailyDeviceReading> results = new List<DailyDeviceReading>();

            for (int ii = 0; ii < numReadings; ++ii)
            {
                DailyDeviceReading reading = new DailyDeviceReading();
                string id = ii.ToString("D3") + BaseId;
                reading.readAt = BaseReadAt + ii * 24 * 3600;
                reading.tag = ii.ToString("D3") + BaseTag;
                reading.deviceId = string.Format("{0}_{1}_{2}", id, reading.tag, reading.readAt);

                PointReading p1 = new PointReading
                {
                    d = val,
                    l = reading.readAt,
                    s = "raw"
                };

                var p2 = new PointReading
                {
                    d = val,
                    l = reading.readAt,
                    s = "raw"
                };

                reading.SetPoint1(new PointReading[] {p1});
                reading.SetPoint2(new PointReading[] {p2});
                results.Add(reading);
            }

            return results;
        }

        public DailyDeviceReading Merge(DailyDeviceReading reading1, DailyDeviceReading reading2)
        {
            DailyDeviceReading mr = new DailyDeviceReading
            {
                tag = reading1.tag,
                deviceId = reading1.deviceId,
                readAt = reading1.readAt,
            };
            mr.SetPoint1(new PointReading[reading2.point1().Length + reading1.point1().Length]);
            reading2.point1().CopyTo(mr.point1(), 0);
            reading1.point1().CopyTo(mr.point1(), reading2.point1().Length);

            mr.SetPoint2(new PointReading[reading2.point2().Length + reading1.point2().Length]);
            reading2.point2().CopyTo(mr.point2(), 0);
            reading1.point2().CopyTo(mr.point2(), reading2.point2().Length);

            return mr;
        }
    }
}
