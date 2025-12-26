using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using Npgsql;
using NpgsqlTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MqttDataProcessor.Data
{
    public class PostgreSqlRepository : IDataRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgreSqlRepository> _logger;

        public PostgreSqlRepository(IConfiguration configuration, ILogger<PostgreSqlRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("PostgreSQL bağlantı dizesi bulunamadı.");
            _logger = logger;
        }

        // Tekli kayıt metodu (Gerektiğinde hala kullanılabilir)
        public async Task SaveSensorDataAsync(SensorData data)
        {
            await SaveBatchSensorDataAsync(new List<SensorData> { data });
        }

        // --- ASIL BULK INSERT METODU ---
        public async Task SaveBatchSensorDataAsync(IEnumerable<SensorData> dataBatch)
        {
            if (dataBatch == null || !dataBatch.Any()) return;

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // PostgreSQL Binary COPY komutu: INSERT'ten kat kat daha hızlıdır.
                // Tablo ve sütun isimlerinin DB ile birebir aynı (büyük/küçük harf) olduğundan emin olun.
                string copyCommand = @"
                    COPY public.""SensorReading"" (timestamp, sensor_id, temperature, humidity) 
                    FROM STDIN (FORMAT BINARY)";

                await using var writer = connection.BeginBinaryImport(copyCommand);

                foreach (var data in dataBatch)
                {
                    // Tarih dönüşümü (UTC formatına zorla)
                    DateTime timestampValue = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(data.Timestamp) && DateTime.TryParse(data.Timestamp, out DateTime parsedDate))
                    {
                        timestampValue = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                    }

                    await writer.StartRowAsync();
                    await writer.WriteAsync(timestampValue, NpgsqlDbType.TimestampTz);
                    await writer.WriteAsync(data.DeviceId ?? "Unknown", NpgsqlDbType.Varchar);
                    await writer.WriteAsync(data.Temperature, NpgsqlDbType.Double);
                    await writer.WriteAsync(data.Humidity, NpgsqlDbType.Double);
                }

                // Verileri veritabanına fiziksel olarak işle (Commit)
                await writer.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk Insert (COPY) işlemi sırasında hata oluştu: {Message}", ex.Message);
                throw;
            }
        }
    }
}