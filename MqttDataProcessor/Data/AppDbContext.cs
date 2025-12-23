using Microsoft.EntityFrameworkCore;
using MqttDataProcessor.Models;

namespace MqttDataProcessor.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<SensorData> SensorReading { get; set; }
    }
}