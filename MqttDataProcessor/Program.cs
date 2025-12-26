using MqttDataProcessor.Data;
using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using MqttDataProcessor.Workers; // Bu satýr hatayý çözer

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // 1. Veritabaný iþlemleri (Saf Npgsql)
        services.AddSingleton<IDataRepository, PostgreSqlRepository>();

        // 2. Tampon Bellek (Kanal tabanlý)
        services.AddSingleton(typeof(IDataBuffer<SensorData>), typeof(DataBuffer));

        // 3. Arka Plan Servisleri (Workers)
        services.AddHostedService<MqttSubscriberWorker>();
        services.AddHostedService<BatchWriterService>();
    })
    .Build();

await host.RunAsync();