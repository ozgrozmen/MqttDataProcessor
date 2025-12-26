using System.Collections.Generic;
using MqttDataProcessor.Models;

namespace MqttDataProcessor.Interfaces
{
    public interface IDataBuffer<T>
    {
        // MqttSubscriberWorker'ın veri eklemesi için
        void Add(T item);

        // BatchWriterService'in verileri toplu çekip tamponu boşaltması için
        IEnumerable<T> GetDataForBatch();

        // BatchWriterService'in eşik kontrolü (1000 veri doldu mu?) yapabilmesi için
        int Count { get; }
    }
}