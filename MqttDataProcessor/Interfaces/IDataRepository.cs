using MqttDataProcessor.Models;
using System.Collections.Generic; // IEnumerable için gereklidir
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MqttDataProcessor.Interfaces
{
    public interface IDataRepository
    {
        public interface IDataBuffer<T>
        {
            void AddData(T data);
            int Count { get; }
            // Kanal okuyucusunu interface'e ekliyoruz
            ChannelReader<T> Reader { get; }
        }

        Task SaveSensorDataAsync(SensorData data);

        Task SaveBatchSensorDataAsync(IEnumerable<SensorData> dataBatch);

    }
}


