using System.ComponentModel.DataAnnotations.Schema;

namespace MqttDataProcessor.Models
{
    [Table("SensorReading", Schema = "public")]
    public class SensorData
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("sensor_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Column("temperature")]
        public double Temperature { get; set; }

        [Column("humidity")]
        public double Humidity { get; set; }

        [Column("timestamp")]
        public DateTime ProcessedTimestamp { get; set; }

        [NotMapped]
        public string? Timestamp { get; set; }

        public SensorData(string deviceId, double temperature, double humidity, string? timestamp)
        {
            DeviceId = deviceId;
            Temperature = temperature;
            Humidity = humidity;
            Timestamp = timestamp;
            ProcessedTimestamp = DateTime.UtcNow;
        }

        public SensorData()
        {
            ProcessedTimestamp = DateTime.UtcNow;
        }
    }
}