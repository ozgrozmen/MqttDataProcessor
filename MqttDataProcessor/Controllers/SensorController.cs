using Microsoft.AspNetCore.Mvc;
using MqttDataProcessor.Interfaces;
using MqttDataProcessor.Models;
using System.Runtime.InteropServices;

namespace MqttDataProcessor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController : ControllerBase
    {
        private readonly IDataRepository _repository;

        public SensorController(IDataRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentData([FromQuery] int count = 50)
        {
            // IDataRepository içine GetRecentDataAsync metodunu eklemen gerekecek
            var data = await _repository.GetRecentDataAsync(count);
            return Ok(data);
        }
    }
}