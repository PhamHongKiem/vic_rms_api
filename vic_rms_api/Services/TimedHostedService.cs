using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using vic_rms_api.Context;
using vic_rms_api.Logs;
using vic_rms_api.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace vic_rms_api.Services
{
    public class TimedHostedService : BackgroundService //IHostedService, IDisposable
    {
        private Timer _timer;
        private double _minute;
        private double _hour_1;
        private double _hour_2;
        private readonly ILogger<TimedHostedService> _logger;
        private readonly object _lockObject = new object();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _baseUrl;
        private readonly string _connectionString;
        
        private readonly IHttpClientFactory _clientFactory;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        //private static readonly HttpClient _client;
        //static TimedHostedService()
        //{
        //    _client = new HttpClient
        //    {
        //        Timeout = TimeSpan.FromMinutes(3) // Tăng thời gian chờ lên 5 phút
        //    };
        //}

        public TimedHostedService(IHttpClientFactory clientFactory, IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TimedHostedService> logger)
        {
            _clientFactory = clientFactory;
            _scopeFactory = scopeFactory;
            _baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl");
            _connectionString = configuration.GetValue<string>("DefaultConnection");
            _minute = Convert.ToDouble(configuration.GetValue<string>("TimerSetting:Minute_timer"));
            _hour_1 = Convert.ToDouble(configuration.GetValue<string>("TimerSetting:hour_timer_1"));
            _hour_2 = Convert.ToDouble(configuration.GetValue<string>("TimerSetting:hour_timer_2"));
            _logger = logger;

            //_client = new HttpClient();
            //_client = new HttpClient
            //{
            //    Timeout = TimeSpan.FromMinutes(3) // Tăng thời gian chờ lên 5 phút
            //};

            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chuyển đổi để sử dụng System.Threading.Timer trong một cách tiếp cận bất đồng bộ
            _timer = new Timer(async _ =>
            {
                // Gọi DoWorkAsync và đợi cho đến khi nó hoàn thành.
                await DoWorkMultiThreadAsync(null);

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(_minute)); // Thay đổi tần suất theo nhu cầu

            // Khi đã lên lịch cho Timer, bạn có thể kết thúc phương thức này - Timer sẽ tiếp tục hoạt động độc lập
            return Task.CompletedTask;
        }
        private async Task DoWorkAsync(object state)
        {
            try
            {
                await _semaphore.WaitAsync();
                Logger.Log($"Timed Background Service is starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                DateTime currentTime = DateTime.Now;
                bool isSpecialTime = (currentTime.Hour == _hour_1 && currentTime.Hour <= 20) ||
                                     (currentTime.Hour == _hour_2 && currentTime.Hour <= 20);
                //isSpecialTime = true;
                // Các tác vụ đồng bộ hóa bất đồng bộ bên trong đây
                using (var scope = _scopeFactory.CreateScope())
                {
                    ITokenService tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                    await tokenService.GetToken();

                    if (!await tokenService.IsTokenValid())
                    {
                        await tokenService.RefreshToken();
                    }

                    var hotelsService = scope.ServiceProvider.GetRequiredService<HotelsService>();
                    var hotels = await hotelsService.GetHotelsAsync();
                    List<Task> tasks = new List<Task>();
                    foreach (var h in hotels) 
                    {
                        IRates_GridService rates_gridService = scope.ServiceProvider.GetRequiredService<IRates_GridService>();

                        var ratesService = scope.ServiceProvider.GetRequiredService<RatesService>();

                        var rates = await ratesService.GetRatesAsync(Convert.ToInt16(h.RMS_propertyID.ToString()));
                        var Rates_Grid = await rates_gridService.GetRates_GridAsync(Convert.ToInt16(h.RMS_propertyID.ToString()));
                        var groupedSpecialRate = rates.GroupBy(rate => rate.RMS_propertyID)
                        .Select(group => new
                        {
                            PropertyId = group.Key,
                            CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
                            RateIds = group.Where(g => g.RMS_rateID == 17).Select(g => g.RMS_rateID).Distinct().ToArray()
                        }).Where(g => g.RateIds.Any());

                        var groupedRegularRate = rates.GroupBy(rate => rate.RMS_propertyID)
                            .Select(group => new
                            {
                                PropertyId = group.Key,
                                CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
                                RateIds = group.Where(g => g.RMS_rateID != 17).Select(g => g.RMS_rateID).Distinct().ToArray()
                            }).Where(g => g.RateIds.Any());


                        TokenResponse tokenResponse = tokenService.GetCurrentToken();
                        if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                        {
                            if (isSpecialTime)
                            {
                                foreach (var group in groupedSpecialRate)
                                {
                                    //await ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime);
                                    //tasks.Add(ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime));
                                }
                            }
                            foreach (var group in groupedRegularRate)
                            {
                                //await ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime);
                                //tasks.Add(ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime));
                            }
                        }
                    }
                    await Task.WhenAll(tasks);
                }
                Logger.Log($"Task completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Logger.Log($"DoWorkAsync(): An error occurred: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        //private async Task DoWorkMultiThreadAsync(object state)
        //{
        //    try
        //    {
        //        await _semaphore.WaitAsync();
        //        Logger.Log($"Timed Background Service is starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        //        DateTime currentTime = DateTime.Now;
        //        bool isSpecialTime = (currentTime.Hour == _hour_1 && currentTime.Hour <= 20) ||
        //                             (currentTime.Hour == _hour_2 && currentTime.Hour <= 20);
        //        //isSpecialTime = true;
        //        // Các tác vụ đồng bộ hóa bất đồng bộ bên trong đây
        //        using (var scope = _scopeFactory.CreateScope())
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
        //            var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        //            await executionStrategy.ExecuteAsync(async () =>
        //            {
        //                using (var transaction = await dbContext.Database.BeginTransactionAsync())
        //                {
        //                    ITokenService tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        //                    await tokenService.GetToken();

        //                    if (!await tokenService.IsTokenValid())
        //                    {
        //                        await tokenService.RefreshToken();
        //                    }

        //                    var hotelsService = scope.ServiceProvider.GetRequiredService<HotelsService>();
        //                    var hotels = await hotelsService.GetHotelsAsync();

        //                    IRates_GridService rates_gridService = scope.ServiceProvider.GetRequiredService<IRates_GridService>();

        //                    var ratesService = scope.ServiceProvider.GetRequiredService<RatesService>();

        //                    var rates = await ratesService.GetRatesAsync();
        //                    var Rates_Grid = await rates_gridService.GetRates_GridAsync();

        //                    var groupedSpecialRate = rates.GroupBy(rate => rate.RMS_propertyID)
        //                        .Select(group => new
        //                        {
        //                            PropertyId = group.Key,
        //                            CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
        //                            RateIds = group.Where(g => g.RMS_rateID == 17).Select(g => g.RMS_rateID).Distinct().ToArray()
        //                        }).Where(g => g.RateIds.Any());

        //                    var groupedRegularRate = rates.GroupBy(rate => rate.RMS_propertyID)
        //                        .Select(group => new
        //                        {
        //                            PropertyId = group.Key,
        //                            CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
        //                            RateIds = group.Where(g => g.RMS_rateID != 17).Select(g => g.RMS_rateID).Distinct().ToArray()
        //                        }).Where(g => g.RateIds.Any());


        //                    TokenResponse tokenResponse = tokenService.GetCurrentToken();
        //                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
        //                    {
        //                        List<Task> tasks = new List<Task>();

        //                        if (isSpecialTime)
        //                        {
        //                            foreach (var group in groupedSpecialRate)
        //                            {
        //                                tasks.Add(ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime));
        //                            }
        //                        }
        //                        foreach (var group in groupedRegularRate)
        //                        {
        //                            tasks.Add(ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime));
        //                        }

        //                        await Task.WhenAll(tasks);
        //                    }

        //                    await transaction.CommitAsync();
        //                }
        //            });
        //        }
        //        Logger.Log($"Task completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"DoWorkMultiThreadAsync(): An error occurred: {ex.Message}");
        //    }
        //    finally
        //    {
        //        _semaphore.Release();
        //    }
        //}

        private async Task DoWorkMultiThreadAsync_bk(object state)
        {
            try
            {
                await _semaphore.WaitAsync();
                Logger.Log($"Timed Background Service is starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                DateTime currentTime = DateTime.Now;
                bool isSpecialTime = (currentTime.Hour == _hour_1 && currentTime.Hour <= 20) ||
                                     (currentTime.Hour == _hour_2 && currentTime.Hour <= 20);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
                    var executionStrategy = dbContext.Database.CreateExecutionStrategy();

                    await executionStrategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await dbContext.Database.BeginTransactionAsync())
                        {
                            ITokenService tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                            await tokenService.GetToken();

                            if (!await tokenService.IsTokenValid())
                            {
                                await tokenService.RefreshToken();
                            }

                            var hotelsService = scope.ServiceProvider.GetRequiredService<HotelsService>();
                            var hotels = await hotelsService.GetHotelsAsync();

                            IRates_GridService rates_gridService = scope.ServiceProvider.GetRequiredService<IRates_GridService>();

                            var ratesService = scope.ServiceProvider.GetRequiredService<RatesService>();

                            var rates = await ratesService.GetRatesAsync();
                            var Rates_Grid = await rates_gridService.GetRates_GridAsync();

                            var groupedSpecialRate = rates.GroupBy(rate => rate.RMS_propertyID)
                                .Select(group => new
                                {
                                    PropertyId = group.Key,
                                    CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
                                    RateIds = group.Where(g => g.RMS_rateID == 17).Select(g => g.RMS_rateID).Distinct().ToArray()
                                }).Where(g => g.RateIds.Any());

                            var groupedRegularRate = rates.GroupBy(rate => rate.RMS_propertyID)
                                .Select(group => new
                                {
                                    PropertyId = group.Key,
                                    CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
                                    RateIds = group.Where(g => g.RMS_rateID != 17).Select(g => g.RMS_rateID).Distinct().ToArray()
                                }).Where(g => g.RateIds.Any());

                            TokenResponse tokenResponse = tokenService.GetCurrentToken();
                            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                            {
                                List<Task> tasks = new List<Task>();

                                if (isSpecialTime)
                                {
                                    foreach (var group in groupedSpecialRate)
                                    {
                                        //tasks.Add(Task.Run(() => ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime)));
                                    }
                                }
                                foreach (var group in groupedRegularRate)
                                {
                                    //tasks.Add(Task.Run(() => ProcessRate(Rates_Grid, group, tokenService, rates_gridService, isSpecialTime)));
                                }

                                await Task.WhenAll(tasks);
                            }

                            await transaction.CommitAsync();
                        }
                    });
                }
                Logger.Log($"Task completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Logger.Log($"DoWorkMultiThreadAsync(): An error occurred: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task DoWorkMultiThreadAsync(object state)
        {
            try
            {
                await _semaphore.WaitAsync();
                Logger.Log($"Timed Background Service is starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                DateTime currentTime = DateTime.Now;
                bool isSpecialTime = (currentTime.Hour == _hour_1 && currentTime.Hour <= 20) ||
                                     (currentTime.Hour == _hour_2 && currentTime.Hour <= 20);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
                    var executionStrategy = dbContext.Database.CreateExecutionStrategy();

                    await executionStrategy.ExecuteAsync(async () =>
                    {
                        //using (var transaction = await dbContext.Database.BeginTransactionAsync())
                        {
                            ITokenService tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                            await tokenService.GetToken();

                            if (!await tokenService.IsTokenValid())
                            {
                                await tokenService.RefreshToken();
                            }

                            var hotelsService = scope.ServiceProvider.GetRequiredService<HotelsService>();
                            var hotels = await hotelsService.GetHotelsAsync();

                            IRates_GridService rates_gridService = scope.ServiceProvider.GetRequiredService<IRates_GridService>();

                            var ratesService = scope.ServiceProvider.GetRequiredService<RatesService>();

                            var rates = await ratesService.GetRatesAsync();
                            var Rates_Grid = await rates_gridService.GetRates_GridAsync();

                            var groupedSpecialRate = rates.GroupBy(rate => rate.RMS_propertyID)
                                .Select(group => new
                                {
                                    PropertyId = group.Key,
                                    CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
                                    RateIds = group.Where(g => g.RMS_rateID == 17).Select(g => g.RMS_rateID).Distinct().ToArray()
                                }).Where(g => g.RateIds.Any());

                            var groupedRegularRate = rates.GroupBy(rate => rate.RMS_propertyID)
                                .Select(group => new
                                {
                                    PropertyId = group.Key,
                                    CategoryIds = group.Select(g => g.RMS_categoryID).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray(),
                                    RateIds = group.Where(g => g.RMS_rateID != 17).Select(g => g.RMS_rateID).Distinct().ToArray()
                                }).Where(g => g.RateIds.Any());

                            TokenResponse tokenResponse = tokenService.GetCurrentToken();
                            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                            {
                                List<Task> tasks = new List<Task>();

                                if (isSpecialTime)
                                {
                                    foreach (var group in groupedSpecialRate)
                                    {
                                        tasks.Add(Task.Run(() => ProcessRate(group, tokenService, rates_gridService, true)));
                                    }
                                }
                                foreach (var group in groupedRegularRate)
                                {
                                    tasks.Add(Task.Run(() => ProcessRate(group, tokenService, rates_gridService, false)));
                                }

                                await Task.WhenAll(tasks);
                            }

                            //await transaction.CommitAsync();
                        }
                    });
                }
                Logger.Log($"Task completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Logger.Log($"DoWorkMultiThreadAsync(): An error occurred: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessRate(dynamic group, ITokenService tokenService, IRates_GridService rate_gridService, bool isSpecialRate = true)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
                //var _client = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var _client = _clientFactory.CreateClient("MyHttpClient");

                if (group.CategoryIds != null && group.RateIds != null)
                {
                    DateTime dateTo_temp = DateTime.UtcNow.Date.AddYears(2);
                    DateTime dateFrom_temp = DateTime.UtcNow.Date;

                    if (!await tokenService.IsTokenValid())
                    {
                        await tokenService.RefreshToken();
                    }

                    // Update the authtoken header for each request
                    string currentToken = tokenService.GetCurrentToken().Token;
                    _client.DefaultRequestHeaders.Remove("authtoken");  // Remove the previous token if any
                    _client.DefaultRequestHeaders.Add("authtoken", currentToken);  // Add the new token

                    var checkAvailUrl = $"{_baseUrl}/availabilityRateGrid";

                    // Tạo một đối tượng mới từ class RateRequest
                    AvailabilityRequest request = new AvailabilityRequest
                    {
                        Adults = 2,
                        AgentId = 0,
                        CategoryIds = group.CategoryIds,
                        Children = 0,
                        DateFrom = dateFrom_temp,
                        DateTo = dateFrom_temp,
                        Infants = 0,
                        PropertyId = group.PropertyId,
                        RateIds = group.RateIds
                    };

                    var ratesGrid = await dbContext.wp_rates_grid.Where(g => g.Datesell >= DateTime.Today).ToListAsync();
                    while (dateFrom_temp <= dateTo_temp.AddDays(-14))
                    {
                        try
                        {
                            request.DateFrom = dateFrom_temp;
                            request.DateTo = dateFrom_temp.AddDays(14);

                            var jsonRequest = JsonConvert.SerializeObject(request);
                            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                            HttpResponseMessage response = new HttpResponseMessage();
                            for (int i = 0; i <= 10; i++) // Retry loop
                            {
                                try
                                {
                                    response = await _client.PostAsync(checkAvailUrl, content);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var json = await response.Content.ReadAsStringAsync();
                                        var availabilityResponse = JsonConvert.DeserializeObject<AvailabilityResponse>(json);
                                        if (availabilityResponse != null)
                                        {

                                            if (isSpecialRate)
                                            {
                                                await rate_gridService.UpdateRates_Grid_SpecialRateAsync(ratesGrid, availabilityResponse, group.PropertyId);
                                            }
                                            else
                                            {
                                                await rate_gridService.UpdateRates_Grid_RegularRateAsync(ratesGrid, availabilityResponse, group.PropertyId);
                                            }
                                        }
                                        break;
                                    }
                                }
                                catch (TaskCanceledException ex) when (ex.CancellationToken == CancellationToken.None)
                                {
                                    // Xử lý trường hợp hết thời gian chờ
                                    Logger.Log($"ProcessRate(): Request timed out for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. Retry {i + 1} of 10.");
                                }
                                catch (HttpRequestException ex)
                                {
                                    // Xử lý các lỗi kết nối khác
                                    Logger.Log($"ProcessRate(): HttpRequestException for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. Retry {i + 1} of 10. Error: {ex.Message}");
                                }
                            }
                            if (!response.IsSuccessStatusCode)
                            {
                                Logger.Log($"ProcessRate(): Failed to send data for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. request: {jsonRequest}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"ProcessRate(): An error occurred: {ex.Message}");
                            Logger.Log($"ProcessRate(): track: {ex.StackTrace}");
                            if (ex.InnerException != null)
                                Logger.Log($"ProcessRate(): Inner exception: {ex.InnerException.Message}");
                        }

                        dateFrom_temp = dateFrom_temp.AddDays(14);
                    }
                }
            }
        }


        private async Task ProcessRate_bk_1(dynamic group, ITokenService tokenService, IRates_GridService rate_gridService, bool isSpecialRate = true)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
                var _client = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(2) // Tăng thời gian chờ lên 2 phút
                };

                if (group.CategoryIds != null && group.RateIds != null)
                {
                    DateTime dateTo_temp = DateTime.UtcNow.Date.AddYears(2);
                    DateTime dateFrom_temp = DateTime.UtcNow.Date;

                    if (!await tokenService.IsTokenValid())
                    {
                        await tokenService.RefreshToken();
                    }

                    // Update the authtoken header for each request
                    string currentToken = tokenService.GetCurrentToken().Token;
                    _client.DefaultRequestHeaders.Remove("authtoken");  // Remove the previous token if any
                    _client.DefaultRequestHeaders.Add("authtoken", currentToken);  // Add the new token

                    var checkAvailUrl = $"{_baseUrl}/availabilityRateGrid";

                    // Tạo một đối tượng mới từ class RateRequest
                    AvailabilityRequest request = new AvailabilityRequest
                    {
                        Adults = 2,
                        AgentId = 0,
                        CategoryIds = group.CategoryIds,
                        Children = 0,
                        DateFrom = dateFrom_temp,
                        DateTo = dateFrom_temp,
                        Infants = 0,
                        PropertyId = group.PropertyId,
                        RateIds = group.RateIds
                    };

                    while (dateFrom_temp <= dateTo_temp.AddDays(-14))
                    {
                        try
                        {
                            request.DateFrom = dateFrom_temp;
                            request.DateTo = dateFrom_temp.AddDays(14);

                            var jsonRequest = JsonConvert.SerializeObject(request);
                            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                            HttpResponseMessage response = new HttpResponseMessage();
                            for (int i = 0; i <= 10; i++)
                            {
                                try
                                {
                                    response = await _client.PostAsync(checkAvailUrl, content);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var json = await response.Content.ReadAsStringAsync();
                                        var availabilityResponse = JsonConvert.DeserializeObject<AvailabilityResponse>(json);
                                        if (availabilityResponse != null)
                                        {
                                            var ratesGrid = await dbContext.wp_rates_grid.Where(g => g.Datesell >= DateTime.Today).ToListAsync();

                                            if (isSpecialRate)
                                            {
                                                await rate_gridService.UpdateRates_Grid_SpecialRateAsync(ratesGrid, availabilityResponse, group.PropertyId);
                                            }
                                            else
                                            {
                                                await rate_gridService.UpdateRates_Grid_RegularRateAsync(ratesGrid, availabilityResponse, group.PropertyId);
                                            }
                                        }
                                        break;
                                    }
                                }
                                catch (TaskCanceledException ex) when (ex.CancellationToken == CancellationToken.None)
                                {
                                    // Xử lý trường hợp hết thời gian chờ
                                    Logger.Log($"ProcessRate(): Request timed out for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. Retry {i + 1} of 10.");
                                }
                                catch (HttpRequestException ex)
                                {
                                    // Xử lý các lỗi kết nối khác
                                    Logger.Log($"ProcessRate(): HttpRequestException for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. Retry {i + 1} of 10. Error: {ex.Message}");
                                }
                            }
                            if (!response.IsSuccessStatusCode)
                            {
                                Logger.Log($"ProcessRate(): Failed to send data for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. request: {jsonRequest}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"ProcessRate(): An error occurred: {ex.Message}");
                            if (ex.InnerException != null)
                                Logger.Log($"ProcessRate(): Inner exception: {ex.InnerException.Message}");
                        }

                        dateFrom_temp = dateFrom_temp.AddDays(14);
                    }
                }
            }
        }


        private async Task ProcessRate_bk(List<wp_rates_grid> Rates_Grid, dynamic group, ITokenService tokenService, IRates_GridService rate_gridService, bool isSpecialRate=true)
        {
            #region MyRegion
            using (var scope = _scopeFactory.CreateScope())
            {
                if (group.CategoryIds != null && group.RateIds != null)
                {
                    DateTime dateTo_temp = DateTime.UtcNow.Date.AddYears(2);
                    DateTime dateFrom_temp = DateTime.UtcNow.Date;
                    var _client = _clientFactory.CreateClient("MyHttpClient");
                    if (!tokenService.IsTokenValid().Result)
                    {
                        await tokenService.RefreshToken();
                    }
                    //try
                    {
                        // Update the authtoken header for each request
                        string currentToken = tokenService.GetCurrentToken().Token;
                        _client.DefaultRequestHeaders.Remove("authtoken");  // Remove the previous token if any
                        _client.DefaultRequestHeaders.Add("authtoken", currentToken);  // Add the new token
                    }
                    //catch (Exception ex)
                    //{
                    //    // Update the authtoken header for each request
                    //    string currentToken = tokenService.GetCurrentToken().Token;
                    //    _client.DefaultRequestHeaders.Remove("authtoken");  // Remove the previous token if any
                    //    _client.DefaultRequestHeaders.Add("authtoken", currentToken);  // Add the new token
                    //}

                    var checkAvailUrl = $"{_baseUrl}/availabilityRateGrid";

                    // Tạo một đối tượng mới từ class RateRequest
                    AvailabilityRequest request = new AvailabilityRequest
                    {
                        Adults = 2,
                        AgentId = 0,
                        CategoryIds = group.CategoryIds,
                        Children = 0,
                        DateFrom = Convert.ToDateTime(dateFrom_temp.ToString("yyyy-MM-dd HH:mm:ss")),
                        DateTo = Convert.ToDateTime(dateFrom_temp.ToString("yyyy-MM-dd HH:mm:ss")),
                        Infants = 0,
                        PropertyId = group.PropertyId,
                        RateIds = group.RateIds
                    };

                    while (dateFrom_temp <= dateTo_temp.AddDays(-14))
                    {
                        //await semaphore.WaitAsync(); // Đợi đến khi có slot trống

                        try
                        {
                            request.DateFrom = Convert.ToDateTime(dateFrom_temp.ToString("yyyy-MM-dd HH:mm:ss"));
                            request.DateTo = Convert.ToDateTime(dateFrom_temp.AddDays(14).ToString("yyyy-MM-dd HH:mm:ss"));

                            var jsonRequest = JsonConvert.SerializeObject(request);
                            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                            {
                                HttpResponseMessage response=new HttpResponseMessage();
                                for ( var i = 0; i <= 10; i++)
                                {
                                    response = await _client.PostAsync(checkAvailUrl, content);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var json = await response.Content.ReadAsStringAsync();
                                        var availabilityResponse = JsonConvert.DeserializeObject<AvailabilityResponse>(json);
                                        if (availabilityResponse != null)
                                        {
                                            if (isSpecialRate)
                                            {
                                                await rate_gridService.UpdateRates_Grid_SpecialRateAsync(Rates_Grid, availabilityResponse, group.PropertyId);
                                            }
                                            else
                                            {
                                                await rate_gridService.UpdateRates_Grid_RegularRateAsync(Rates_Grid, availabilityResponse, group.PropertyId);
                                            }
                                        }
                                        break;
                                    }
                                    
                                }
                                if (!response.IsSuccessStatusCode)
                                {
                                    Logger.Log($"ProcessRate(): Failed to send data for Property ID: {group.PropertyId}, CategoryIds: {group.CategoryIds}, RateIds: {group.RateIds}. request: {jsonRequest}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"ProcessRate(): An error occurred: {ex.Message}");
                            if (ex.InnerException != null)
                                Logger.Log($"ProcessRate(): Inner exception: {ex.InnerException.Message}");
                        }
                        finally
                        {
                            //semaphore.Release(); // Phải luôn release semaphore
                        }

                        dateFrom_temp = dateFrom_temp.AddDays(14);
                    }
                }
                
            }
            #endregion
        }



        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }


    }


    #region MyRegion
    //public class ScheduledTaskService : IHostedService, IDisposable
    //{
    //    private readonly IServiceScopeFactory _scopeFactory;
    //    private Timer _timer;

    //    public ScheduledTaskService(IServiceScopeFactory scopeFactory)
    //    {
    //        _scopeFactory = scopeFactory;
    //    }


    //    public Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(0.1)); // Chạy mỗi phút
    //        return Task.CompletedTask;
    //    }

    //    private void DoWork(object state)
    //    {
    //        using (var scope = _scopeFactory.CreateScope())
    //        {
    //            //var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
    //            CheckAvailability();
    //        }
    //        FetchToken();
    //    }

    //    public Task StopAsync(CancellationToken cancellationToken)
    //    {
    //        _timer?.Change(Timeout.Infinite, 0);
    //        return Task.CompletedTask;
    //    }

    //    public void Dispose()
    //    {
    //        _timer?.Dispose();
    //    }

    //    private void FetchToken()
    //    {
    //        // Viết logic lấy token tại đây
    //    }

    //private async Task<List<wp_rate>> GetRate(vicweb_2022DbContext context)
    //{
    //    return await context.Wp_Rates.ToListAsync();
    //}

    //    private async void CheckAvailability()
    //    {
    //        using (var scope = _scopeFactory.CreateScope())
    //        {
    //            var dbContext = scope.ServiceProvider.GetRequiredService<vicweb_2022DbContext>();
    //            var rates = await GetRate(dbContext);
    //            // Xử lý kiểm tra tính khả dụng tại đây
    //            foreach (var rate in rates)
    //            {
    //                // Giả sử bạn muốn kiểm tra tính khả dụng cho mỗi rate
    //                Console.WriteLine($"Checking availability for Rate Name: {rate.Rate_Name}, Property ID: {rate.RMS_propertyID}");
    //            }
    //        }
    //    }

    //} 
    #endregion


}
