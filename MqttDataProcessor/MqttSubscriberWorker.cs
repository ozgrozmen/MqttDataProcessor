using System.Text.Json;
using MqttDataProcessor.Models;
using MQTTnet;
using MQTTnet.Client;
using MqttDataProcessor.Interfaces;
using System.Text;

namespace MqttDataProcessor.Workers;

public class MqttSubscriberWorker : BackgroundService
{
    private readonly ILogger<MqttSubscriberWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDataBuffer<SensorData> _dataBuffer;
    private readonly JsonSerializerOptions _jsonOptions;

    public MqttSubscriberWorker(ILogger<MqttSubscriberWorker> logger,
                                IConfiguration configuration,
                                IDataBuffer<SensorData> dataBuffer)
    {
        _logger = logger;
        _configuration = configuration;
        _dataBuffer = dataBuffer;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        var mqttSettings = _configuration.GetSection("MqttSettings");
        var host = mqttSettings["BrokerHost"] ?? "localhost";
        var port = int.Parse(mqttSettings["BrokerPort"] ?? "1883");
        var topic = mqttSettings["Topic"] ?? "#";

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithCleanSession()
            .Build();

        // MESAJ GELDİĞİNDE: Artık her mesajda log basmıyoruz, sadece işleyip buffer'a atıyoruz.
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            HandleMessage(e);
            return Task.CompletedTask;
        };

        // BAĞLANTI KONTROLLERİ: Bunlar önemli olduğu için logda kalmalı.
        mqttClient.DisconnectedAsync += e =>
        {
            _logger.LogWarning("MQTT Bağlantısı koptu! Yeniden bağlanmaya çalışılıyor...");
            return Task.CompletedTask;
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    _logger.LogInformation("MQTT Broker'a bağlanılıyor: {Host}:{Port}...", host, port);
                    await mqttClient.ConnectAsync(options, stoppingToken);
                    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
                    _logger.LogInformation("Bağlantı Başarılı. Konu dinleniyor: {Topic}", topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("MQTT Bağlantı Hatası: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private void HandleMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = e.ApplicationMessage.PayloadSegment;
            if (payload.Count == 0) return;

            var rawData = JsonSerializer.Deserialize<SensorData>(payload, _jsonOptions);
            if (rawData == null) return;

            var topic = e.ApplicationMessage.Topic;
            var parts = topic.Split('/');
            var deviceId = parts.Length > 1 ? parts[1] : "unknown";

            var data = new SensorData
            {
                DeviceId = deviceId,
                Temperature = rawData.Temperature,
                Humidity = rawData.Humidity,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Veriyi toplu yazılmak üzere sessizce buffer'a ekliyoruz.
            _dataBuffer.Add(data);
        }
        catch (Exception ex)
        {
            // Ayrıştırma hatası gibi kritik durumları hala logluyoruz.
            _logger.LogError("Mesaj işleme hatası: {Message}", ex.Message);
        }
    }
}