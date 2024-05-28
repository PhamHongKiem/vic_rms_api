using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using vic_rms_api.Models;

namespace vic_rms_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckAvailabilityController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public CheckAvailabilityController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClientFactory.CreateClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        //public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityRequest request, string token)
        [HttpPost]
        public async Task<IActionResult> CheckAvailability()
        {
            try
            {
                string authToken = @"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhZ2lkIjoiNDgxIiwiY2xpZCI6IjE2MTI3IiwiaXN0cm4iOiIxIiwicjEyMTUiOiI2NzBiNzczMy01ZGJmLTQ1YjEtYTZmMy0wODIwZWQxMDVkNmMiLCJuYmYiOjE3MTM0NDg0MDQsImV4cCI6MTcxMzUzNDgwNCwiaWF0IjoxNzEzNDQ4NDA0LCJpc3MiOiJ3d3cucm1zY2xvdWQuY29tIn0.Y0gUFSqyXqZvfdeNs1_2eX1xAgfCQedDzxxA4IteCT8";
                
                var request = new AvailabilityRequest
                {
                    Adults = 2,
                    AgentId = 0,
                    CategoryIds = new int[] { 53, 54 },
                    Children = 0,
                    DateFrom = Convert.ToDateTime("2024-05-15 00:00:00"), // Đảm bảo định dạng ngày tháng đúng
                    DateTo = Convert.ToDateTime("2024-05-18 00:00:00"),   // Đảm bảo định dạng ngày tháng đúng
                    Infants = 0,
                    PropertyId = 8,
                    RateIds = new int[] { 15, 23 }
                };
               
                var httpClient = _httpClientFactory.CreateClient();

                httpClient.DefaultRequestHeaders.Add("authtoken", authToken);
                var availabilityUrl = $"{_baseUrl}/availabilityRateGrid";
                var response = await httpClient.PostAsJsonAsync(availabilityUrl, request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var availabilityResponse = JsonConvert.DeserializeObject<AvailabilityResponse>(json);
                    return Ok(availabilityResponse);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    // Log errorResponse để phân tích lỗi
                    return BadRequest("Không thể kiểm tra tình trạng sẵn có: " + errorResponse); // Cung cấp thêm thông tin lỗi
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return BadRequest("Lỗi khi kiểm tra tình trạng sẵn có");
            }
        }

    }
}
