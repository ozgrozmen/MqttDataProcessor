using System.Text.Json;
using MqttDataProcessor.Models;
using MQTTnet;
using MQTTnet.Client;
using MqttDataProcessor.Interfaces;
using System.Text;

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

        // Optimizasyon: Her seferinde yeni options oluşturmamak için constructor'da tanımlıyoruz
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        var mqttSettings = _configuration.GetSection("MqttSettings");
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttSettings["BrokerHost"], int.Parse(mqttSettings["BrokerPort"]!))
            .WithCleanSession()
            .Build();

        // [DÜZELTME] Asenkron event handler yapısı
        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            HandleMessage(e); // Senkron veya asenkron yönetim
            return Task.CompletedTask;
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(options, stoppingToken);
                    var topic = mqttSettings["Topic"];
                    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
                    _logger.LogInformation($"MQTT broker'a bağlandı: {topic}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT bağlantı hatası.");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private void HandleMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            // Bu işlem "Heap" üzerinde gereksiz string kopyaları oluşmasını engeller.
            var rawData = JsonSerializer.Deserialize<SensorData>(e.ApplicationMessage.PayloadSegment, _jsonOptions);

            var topic = e.ApplicationMessage.Topic;
            var parts = topic.Split('/');
            var deviceId = parts.Length > 1 ? parts[1] : "unknown";

            // 2. Readonly Struct Kullanımı: Değerleri Constructor (Yapıcı Metot) ile atıyoruz
            var data = new SensorData(
                deviceId,
                rawData?.Temperature ?? 0,
                rawData?.Humidity ?? 0,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            );

            _dataBuffer.AddData(data);

            // Performans Notu: Çok yüksek frekanslı veride LogInformation'ı kapatmak CPU'yu rahatlatır.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj işleme hatası.");
        }
    }
}