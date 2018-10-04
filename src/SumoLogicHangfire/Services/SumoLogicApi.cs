using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public class SumoLogicApi : IApiCallService
    {
        private readonly SumoLogicSettings _sumoLogicSettings;
        private readonly IBackgroundJobClient _jobClient;
        private readonly ILogger _logger;

        public SumoLogicApi(SumoLogicSettings sumoLogicSettings, IBackgroundJobClient jobClient, 
            ILogger<SumoLogicApi> logger)
        {
            _sumoLogicSettings = sumoLogicSettings;
            _jobClient = jobClient;
            _logger = logger;
        }

        [Queue("api"), AutomaticRetry(Attempts = 0)]
        public async Task CallApiAsync(ApiData apiData)
        {
            _logger.LogInformation(
                $"{DateTimeOffset.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff")} - " +
                JsonConvert.SerializeObject(new {
                    apiData.HttpMethod, SearchApi = apiData.SearchApi.ToString(), apiData.RequestUri, apiData.SearchJobId })
            );

            var request = new HttpRequestMessage(apiData.HttpMethod, apiData.RequestUri);
            request.Headers.Authorization = 
                new AuthenticationHeaderValue(
                    "Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_sumoLogicSettings.AccessId}:{_sumoLogicSettings.AccessKey}")));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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

                _jobClient.Enqueue<SumoLogMining>((s) => s.Callback(apiData.SearchApi, apiResponse));
            }
        }
    }
}
