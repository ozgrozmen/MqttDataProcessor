using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR; // YENİ: SignalR kütüphanesi
using MqttDataProcessor.Hubs;      // YENİ: Oluşturacağınız Hub klasörü

namespace MqttDataProcessor.Services
{
    public class BatchWriterService : BackgroundService
    {
        private readonly ILogger<BatchWriterService> _logger;
        private readonly IDataBuffer<SensorData> _dataBuffer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SensorHub> _hubContext; // YENİ: Hub context'i ekledik

        private const int BATCH_SIZE = 5000;
        private readonly TimeSpan _maxWaitTime = TimeSpan.FromSeconds(60);

        public BatchWriterService(ILogger<BatchWriterService> logger,
                                  IDataBuffer<SensorData> dataBuffer,
                                  IServiceScopeFactory scopeFactory,
                                  IHubContext<SensorHub> hubContext) // YENİ: DI ile inject ettik
        {
            _logger = logger;
            _dataBuffer = dataBuffer;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext; // YENİ
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BatchWriterService (EF Core, GC & SignalR Optimized) başladı.");

            var batch = new List<SensorData>(BATCH_SIZE);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                while (await _dataBuffer.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (_dataBuffer.Reader.TryRead(out var data))
                    {
                        batch.Add(data);

                        if (batch.Count >= BATCH_SIZE || stopwatch.Elapsed >= _maxWaitTime)
                        {
                            await WriteToDbAndNotifyAsync(batch); // Metot adını güncelledik
                            stopwatch.Restart();
                        }
                    }

                    if (batch.Count > 0 && stopwatch.Elapsed >= _maxWaitTime)
                    {
                        await WriteToDbAndNotifyAsync(batch); // Metot adını güncelledik
                        stopwatch.Restart();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("BatchWriterService durduruluyor...");
                if (batch.Count > 0) await WriteToDbAndNotifyAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "BatchWriterService beklenmedik bir hata ile durdu!");
            }
        }

        private async Task WriteToDbAndNotifyAsync(List<SensorData> batch)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

                try
                {
                    // 1. Veritabanına Kayıt (Mevcut mantığın)
                    await repository.SaveBatchSensorDataAsync(batch);
                    _logger.LogInformation("{Count} adet veri veritabanına başarıyla aktarıldı.", batch.Count);

                    // 2. Dashboard'a Bildirim (YENİ)
                    // Veriyi olduğu gibi gönderiyoruz. 
                    // İstersen performans için batch'in tamamı yerine sadece ilk 10 kaydı veya özetini gönderebilirsin.
                    await _hubContext.Clients.All.SendAsync("ReceiveSensorBatch", batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Veritabanına yazma veya Dashboard bildirimi sırasında hata!");
                }
                finally
                {
                    batch.Clear();
                }
            }
        }
    }
}