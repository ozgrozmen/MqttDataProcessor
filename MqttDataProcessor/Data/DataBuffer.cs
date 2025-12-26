using System.Threading.Channels;
using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using System.Collections.Generic; // IEnumerable ve List için

namespace MqttDataProcessor.Data
{
    public class DataBuffer : IDataBuffer<SensorData>
    {
        private readonly Channel<SensorData> _channel;

        public DataBuffer()
        {
            // Kanal seçenekleri: 10.000 veri kapasiteli
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<SensorData>(options);
        }

        // Arayüzle uyumlu olması için Reader özelliğini koruyoruz
        public ChannelReader<SensorData> Reader => _channel.Reader;

        // Arayüzde 'Add' olarak güncellediğimiz için ismi değiştirdik
        public void Add(SensorData data)
        {
            if (data != null)
            {
                _channel.Writer.TryWrite(data);
            }
        }

        // BatchWriterService'in hata vermesine sebep olan EKSİK METOT:
        // Kanal içindeki tüm birikmiş verileri bir kerede çeker ve listeye boşaltır.
        public IEnumerable<SensorData> GetDataForBatch()
        {
            var batch = new List<SensorData>();

            // Kanalda okunmayı bekleyen veri olduğu sürece çek (Non-blocking)
            while (_channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            return batch;
        }

        // Gerektiğinde kanalın doluluk oranını görmek için
        public int Count => _channel.Reader.Count;
    }
}