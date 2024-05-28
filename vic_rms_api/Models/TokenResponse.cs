using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using vic_rms_api.Logs;

namespace vic_rms_api.Models
{
    public class TokenResponse
    {
        public string Token { get; set; }

        [JsonPropertyName("ExpiryDate")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonDateTimeNullableConverter))]
        public DateTime? ExpiryDate { get; set; }
        public List<Property> AllowedProperties { get; set; }
    }

    public interface ITokenService
    {
        Task<bool> IsTokenValid();
        Task RefreshToken();
        Task GetToken();
        TokenResponse GetCurrentToken();
    }

    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;
        private TokenResponse _currentToken;
        private readonly string _connectionString;

        public TokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl");  // Đảm bảo cấu hình BaseUrl trong appsettings.json
            _connectionString = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
        }

        public async Task<bool> IsTokenValid()
        {
            return _currentToken != null && _currentToken.ExpiryDate.Value.AddHours(-1) > DateTime.UtcNow;
        }

        public async Task RefreshToken()
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
                    _currentToken = JsonConvert.DeserializeObject<TokenResponse>(json);
                    // Cập nhật token mới vào cơ sở dữ liệu
                    await SaveOrUpdateToken(_currentToken.Token, _currentToken.ExpiryDate);

                }
                else
                {
                    Logger.Log($"RefreshToken(): Unable to refresh token");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"RefreshToken(): {ex.Message}");
            }
        }

        private async Task SaveOrUpdateToken(string token, DateTime? expiryDate)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var checkCommand = new MySqlCommand("SELECT COUNT(1) FROM Token WHERE TokenID = 1", connection);
                var exists = (long)await checkCommand.ExecuteScalarAsync() > 0;

                string sqlCommand;
                if (exists)
                {
                    sqlCommand = "UPDATE Token SET TokenValue = @TokenValue, ExpirationDate = @ExpirationDate WHERE TokenID = 1";
                }
                else
                {
                    sqlCommand = "INSERT INTO Token (TokenID, TokenValue, ExpirationDate) VALUES (1, @TokenValue, @ExpirationDate)";
                }

                using (var command = new MySqlCommand(sqlCommand, connection))
                {
                    command.Parameters.AddWithValue("@TokenValue", token);
                    command.Parameters.AddWithValue("@ExpirationDate", expiryDate);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }



        public async Task GetToken()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                var sqlCommand = "SELECT TokenValue, ExpirationDate FROM Token";
                connection.Open();
                using (var command = new MySqlCommand(sqlCommand, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            _currentToken = new TokenResponse
                            {
                                Token = reader["TokenValue"].ToString(),
                                ExpiryDate = DateTime.Parse(reader["ExpirationDate"].ToString())
                            };
                        }
                    }
                }
            }
        }

        // Exposes the currently valid token to external consumers securely
        public TokenResponse GetCurrentToken()
        {
            if (IsTokenValid().Result)  // Ensure token is valid before exposing any of its information
            {
                // Assuming you only want to expose the token itself or specific parts of it
                return _currentToken;  // Only return the token string; adjust according to what information is needed
            }
            return null;  // Return null or handle as per requirement if the token is not valid
        }
    }
}
