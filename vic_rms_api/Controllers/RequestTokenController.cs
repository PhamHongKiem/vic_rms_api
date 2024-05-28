using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using vic_rms_api.Models;

namespace vic_rms_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestTokenController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public RequestTokenController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        [HttpPost]
        public async Task<IActionResult> RequestToken()
        {
            try
            {
                var requestBody = new
                {
                    agentId = 481,
                    agentPassword = "KQYWeTGZqGLpe9RH!",
                    clientId = 16127,
                    clientPassword = "n*4CN3m$",
                    useTrainingDatabase = true,
                    moduleType = new string[] { "dataWarehouse" }
                };

                var httpClient = _httpClientFactory.CreateClient();
                var authTokenUrl = $"{_baseUrl}/authToken";
                var response = await httpClient.PostAsJsonAsync(authTokenUrl, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);

                    return Ok(tokenResponse);
                }
                return BadRequest("Không thể lấy token");
            }
            catch (Exception ex)
            {
                return BadRequest("Không thể lấy token");
            }
        }

    }
}
