using System.Threading.Channels;
using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;

namespace MqttDataProcessor.Data
{
    public class DataBuffer : IDataBuffer<SensorData>
    {
        // Channel, ConcurrentBag'e göre çok daha performanslı ve bellek dostudur
        private readonly Channel<SensorData> _channel;

        public DataBuffer()
        {
            // BoundedChannel: Bellek şişmesini önlemek için kapasite sınırı koyar
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait, // Kapasite dolarsa yazarı bekletir, RAM'i korur
                SingleReader = true, // Veriyi sadece BatchWriter okuyacağı için optimizasyon sağlar
                SingleWriter = false // MQTT Worker'lar birden fazla thread üzerinden yazabilir
            };
            _channel = Channel.CreateBounded<SensorData>(options);
        }

        // Kanalın okuyucu kısmını dışarı açıyoruz
        public ChannelReader<SensorData> Reader => _channel.Reader;

        public void AddData(SensorData data)
        {
            _channel.Writer.TryWrite(data);
        }

        public int Count => _channel.Reader.Count;

    }
}