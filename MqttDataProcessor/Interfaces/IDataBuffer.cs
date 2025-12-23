using System.Threading.Channels;
using MqttDataProcessor.Models;

namespace MqttDataProcessor.Interfaces
{
    /// <summary>
    /// Verileri MQTT'den alıp DB'ye yazılana kadar bellekte güvenli bir şekilde 
    /// kanal (Channel) yapısı ile tutan tampon arayüzü.
    /// </summary>
    public interface IDataBuffer<T>
    {
        // [KRİTİK]: BatchWriterService'in verileri okuyabilmesi için bu tanım şarttır.
        // Channel, veriyi okunduğu anda otomatik olarak tampondan temizler.
        ChannelReader<T> Reader { get; }

        // Gelen veriyi kanala (tampona) eklemek için kullanılan metot.
        void AddData(T data);

        // Tampondaki anlık veri sayısını izlemek için özellik.
        int Count { get; }

        // NOT: GetDataForBatch ve ClearBatch metotları kanal yapısında 
        // gereksiz olduğu için kaldırılmıştır.
    }
}