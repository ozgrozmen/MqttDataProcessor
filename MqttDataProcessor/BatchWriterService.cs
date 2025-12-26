using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MqttDataProcessor.Workers
{
    public class BatchWriterService : BackgroundService
    {
        private readonly IDataBuffer<SensorData> _dataBuffer;
        private readonly IDataRepository _repository;
        private readonly ILogger<BatchWriterService> _logger;

        private const int BATCH_SIZE = 8300;
        private DateTime _lastWriteTime = DateTime.UtcNow;

        public BatchWriterService(
            IDataBuffer<SensorData> dataBuffer,
            IDataRepository repository,
            ILogger<BatchWriterService> logger)
        {
            _dataBuffer = dataBuffer;
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BatchWriterService aktif. Kesin paket boyutu: {Size}", BATCH_SIZE);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Sadece tamponda 1000 ve üzeri veri varsa işlem yap
                    if (_dataBuffer.Count >= BATCH_SIZE)
                    {
                        // Tampondaki her şeyi al
                        var allData = _dataBuffer.GetDataForBatch().ToList();

                        // Veriyi 1000'lik parçalara böl
                        var chunks = allData.Chunk(BATCH_SIZE).ToList();

                        foreach (var chunk in chunks)
                        {
                            // Eğer parça tam 1000 ise yaz
                            if (chunk.Length == BATCH_SIZE)
                            {
                                await _repository.SaveBatchSensorDataAsync(chunk);
                                _logger.LogInformation(">>> [BULK INSERT SUCCESS] {Count} adet kayıt işlendi. Saat: {Time}",
                                    chunk.Length, DateTime.Now.ToString("HH:mm:ss"));
                            }
                            else
                            {
                                // 1000'den az kalan artığı (Örn: 137 veri) tampona geri ekle
                                foreach (var item in chunk)
                                {
                                    _dataBuffer.Add(item);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Batch yazma hatası: {Message}", ex.Message);
                }

                // Çok hızlı döngüye girip CPU tüketmemesi için kısa bekleme
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}