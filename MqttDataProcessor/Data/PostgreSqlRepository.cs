using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using MqttDataProcessor.Data;
using EFCore.BulkExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MqttDataProcessor.Data
{
    public class PostgreSqlRepository : IDataRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PostgreSqlRepository> _logger;

        public PostgreSqlRepository(AppDbContext context, ILogger<PostgreSqlRepository> logger)
        {
            _context = context;
            _logger = logger;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public async Task SaveSensorDataAsync(SensorData data)
        {
            try
            {
                if (data == null) return;

                // [DÜZELTME] string olan Timestamp'i Parse edip DateTime'a çeviriyoruz
                if (DateTime.TryParse(data.Timestamp, out DateTime parsedDate))
                {
                    data.ProcessedTimestamp = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                }
                else
                {
                    data.ProcessedTimestamp = DateTime.UtcNow;
                }

                await _context.SensorReading.AddAsync(data);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tekli veri kayıt hatası.");
            }
        }

        public async Task SaveBatchSensorDataAsync(IEnumerable<SensorData> dataBatch)
        {
            if (dataBatch == null || !dataBatch.Any()) return;

            var list = dataBatch is List<SensorData> l ? l : dataBatch.ToList();

            try
            {
                foreach (var item in list)
                {
                    // [DÜZELTME] Güvenli Parse ve UTC dönüşümü
                    if (!string.IsNullOrEmpty(item.Timestamp) && DateTime.TryParse(item.Timestamp, out DateTime pDate))
                    {
                        item.ProcessedTimestamp = DateTime.SpecifyKind(pDate, DateTimeKind.Utc);
                    }
                    else
                    {
                        item.ProcessedTimestamp = DateTime.UtcNow;
                    }
                }

                var bulkConfig = new BulkConfig
                {
                    PreserveInsertOrder = false,
                    BatchSize = 5000,
                    EnableShadowProperties = false,
                    CalculateStats = false
                };

                await _context.BulkInsertAsync(list, bulkConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkInsert (Toplu Yazma) hatası oluştu.");
                throw;
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }
    }
}