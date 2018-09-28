using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using SumoLogicHangfire.Models;

namespace SumoLogicHangfire.Services
{
    public class ApiCallService : IApiCallService
    {
        private readonly Random _rand = new Random();

        [Queue("http"), AutomaticRetry(Attempts = 0)]
        public async Task CallApiAsync(ApiData apiData)
        {
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

                BackgroundJob.Enqueue<SumoLogic>((s) => s.Callback(apiData.SearchApi, apiResponse));
            }
        }

        //private string GetSearchJobId(HttpResponseMessage response)
        //{
        //    if (response.Headers.Location != null)

        //}
    }
}
