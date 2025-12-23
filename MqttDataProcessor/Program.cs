using MqttDataProcessor.Data;
using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using MqttDataProcessor.Services;
using MqttDataProcessor.Hubs; // YENÝ: Hub klasörünüzü ekleyin
using Microsoft.EntityFrameworkCore;

// PostgreSQL için zaman damgasý davranýþýný ayarlýyoruz
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args); // Host yerine WebApplication kullanýyoruz

// 1. Veritabaný Yapýlandýrmasý
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Repository ve Buffer Tanýmlamalarý
builder.Services.AddScoped<IDataRepository, PostgreSqlRepository>();
builder.Services.AddSingleton<IDataBuffer<SensorData>, DataBuffer>();

// 3. Web API ve SignalR Servislerini Ekle (YENÝ)
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// 4. CORS Politikasý (React'in baðlanabilmesi için kritik)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React (Vite) varsayýlan portu
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // SignalR için þart
    });
});

// 5. Arka Plan Servisleri (Mevcut yapýnýz korunuyor)
builder.Services.AddHostedService<MqttSubscriberWorker>();
builder.Services.AddHostedService<BatchWriterService>();

var app = builder.Build();

// 6. HTTP Pipeline ve Middleware Yapýlandýrmasý
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("DashboardCorsPolicy");
app.UseRouting();

// 7. Endpoint Tanýmlamalarý
app.MapControllers(); // API endpointlerini aktif eder
app.MapHub<SensorHub>("/sensorHub"); // SignalR kanalýný açar

await app.RunAsync();