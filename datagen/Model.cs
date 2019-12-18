using Newtonsoft.Json;

namespace CosmosSim.DataGen
{
    public class DailyDeviceReading
    {
        [JsonProperty(PropertyName = "p1")]
        private PointReading[] _point1;
        [JsonProperty(PropertyName = "p2")]
        private PointReading[] _point2;

        [JsonProperty(PropertyName = "tag")]
        public string tag { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string deviceId { get; set; }

        [JsonProperty(PropertyName = "t")]
        public long readAt { get; set; }

        public PointReading[] point1() { return _point1; }
        public void SetPoint1(PointReading[] point1) { _point1 = point1; }

        public PointReading[] point2() { return _point2; }
        public void SetPoint2(PointReading[] point2) { _point2 = point2; }
    }

    public class PointReading
    {
        public double d { get; set; }

        public long l { get; set; }

        public string s { get; set; }
    }
}
