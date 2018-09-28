using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SumoLogicHangfire.Configurations;
using SumoLogicHangfire.Models;

namespace SumoLogicHangfire.Services
{
    public class ApiCallService : IApiCallService
    {
        private readonly AppSettings _appSettings;
        private readonly IBackgroundJobClient _jobClient;
        private readonly ILogger _logger;

        public ApiCallService(IOptions<AppSettings> appSettingsAccessor, IBackgroundJobClient jobClient, 
            ILogger<ApiCallService> logger)
        {
            _appSettings = appSettingsAccessor.Value;
            _jobClient = jobClient;
            _logger = logger;
        }

        [Queue("http"), AutomaticRetry(Attempts = 0)]
        public async Task CallApiAsync(ApiData apiData)
        {
            //await Task.Delay(_appSettings.ApiThrottlingInMillisecond);

            _logger.LogInformation(
                $"{DateTimeOffset.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff")} - " +
                JsonConvert.SerializeObject(new {
                    apiData.HttpMethod, SearchApi = apiData.SearchApi.ToString(), apiData.RequestUri, apiData.SearchJobId })
            );

            var request = new HttpRequestMessage(apiData.HttpMethod, apiData.RequestUri);
            if (!string.IsNullOrEmpty(apiData.JsonPayload))
                request.Content = new StringContent(apiData.JsonPayload, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);
                var apiResponse = new ApiResponse
                {
                    RequestContent = apiData.JsonPayload,
                    StatusCode = response.StatusCode,
                    Content = response.Content.ReadAsStringAsync().Result,
                    SearchJobId = apiData.SearchJobId
                };

                _jobClient.Enqueue<SumoLogic>((s) => s.Callback(apiData.SearchApi, apiResponse));
            }
        }
    }
}
