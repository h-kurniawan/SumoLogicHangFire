using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SumoLogicHangfire.Models;
using System;
using System.Net;
using System.Net.Http;

namespace SumoLogicHangfire.Services
{
    public class SumoLogic : ISumoLogic
    {
        private const string JsonMediaType = "application/json";
        private const int MessageBatchLimit = 1000;

        private readonly IBackgroundJobClient _jobClient;
        private readonly IApiCallService _apiCall;
        private readonly IMemoryCache _memoryCache;
        private Uri _baseAddress;
        ILogger _logger;

        public SumoLogic(IBackgroundJobClient jobClient, IApiCallService apiCall
            , ILogger<SumoLogic> logger, IMemoryCache memoryCache)
        {
            _jobClient = jobClient;
            _apiCall = apiCall;
            _logger = logger;
            _memoryCache = memoryCache;

            _baseAddress = new Uri("http://localhost:1259");
        }

        public void MineLog(MineLogRequest mineLogRequest, Uri callback)
        {
            var searchJobRequest = new SearchJobRequest
            {
                Query = mineLogRequest.Query,
                From = mineLogRequest.To.ToUnixTimeMilliseconds(),
                To = mineLogRequest.To.ToUnixTimeMilliseconds()
            };
            CreateSearchJob(searchJobRequest);
        }

        private void CreateSearchJob(SearchJobRequest searchJobRequest)
        {
            var jsonPayload = JsonConvert.SerializeObject(new
            {
                query = searchJobRequest.Query,
                from = searchJobRequest.From,
                to = searchJobRequest.To
            });
            var apiData = new ApiData
            {
                SearchApi = SearchApi.CreateSearchJob,
                HttpMethod = HttpMethod.Post,
                RequestUri = new Uri(_baseAddress, "api/v1/search/jobs"),
                JsonPayload = jsonPayload
            };
            ScheduleApiCall(apiData);
        }

        private void GetJobStatus(string jobId)
        {
            var apiData = new ApiData
            {
                SearchApi = SearchApi.GetJobStatus,
                HttpMethod = HttpMethod.Get,
                RequestUri = new Uri(_baseAddress, $"api/v1/search/jobs/{jobId}"),
                SearchJobId = jobId
            };
            ScheduleApiCall(apiData);
        }

        private void GetMessages(string jobId, int offset)
        {
            var getMessagesUri = new Uri(
                _baseAddress, $"api/v1/search/jobs/{jobId}/messages?offset={offset}&limit={MessageBatchLimit}");

            var apiData = new ApiData
            {
                SearchApi = SearchApi.GetMessages,
                HttpMethod = HttpMethod.Get,
                RequestUri = getMessagesUri,
                SearchJobId = jobId
            };
            ScheduleApiCall(apiData);
        }

        private void DeleteSearchJob(string jobId)
        {
            var apiData = new ApiData
            {
                SearchApi = SearchApi.DeleteSearchJob,
                HttpMethod = HttpMethod.Delete,
                RequestUri = new Uri(_baseAddress, $"api/v1/search/jobs/{jobId}"),
                SearchJobId = jobId
            };
            ScheduleApiCall(apiData);
        }

        [Queue("callback")]
        public void Callback(SearchApi searchApi, ApiResponse response)
        {
            switch (searchApi)
            {
                case SearchApi.CreateSearchJob:
                    CreateSearchJobCallback(response);
                    break;
                case SearchApi.GetJobStatus:
                    GetJobStatusCallback(response);
                    break;
                case SearchApi.GetMessages:
                    GetMessagesCallback(response);
                    break;
                case SearchApi.DeleteSearchJob:
                    DeleteSearchJobCallback(response);
                    break;
            }

            _logger.LogInformation($"StatusCode: {response.StatusCode}");
        }

        private void CreateSearchJobCallback(ApiResponse apiResponse)
        {
            if (apiResponse.StatusCode == HttpStatusCode.Accepted)
            {
                var searchJobResponse = JsonConvert.DeserializeObject<SearchJobResponse>(apiResponse.Content);
                GetJobStatus(searchJobResponse.Id);
            }
            else if (apiResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var searchJobRequest = JsonConvert.DeserializeObject<SearchJobRequest>(apiResponse.RequestContent);
                CreateSearchJob(searchJobRequest);
            }
            // else other error handling here
        }

        private void GetJobStatusCallback(ApiResponse apiResponse)
        {
            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var jobStatusResponse = JsonConvert.DeserializeObject<GetJobStatusResponse>(apiResponse.Content);
                switch (jobStatusResponse.State)
                {
                    case "GATHERING RESULTS":
                    case "NOT STARTED":
                    case "FORCE PAUSED":
                        GetJobStatus(apiResponse.SearchJobId);
                        break;
                    case "DONE GATHERING RESULTS":
                        var logMiningInfo = new LogMiningInfo { TotalMessageCount = jobStatusResponse.MessageCount };
                        _memoryCache.Set<LogMiningInfo>(apiResponse.SearchJobId, logMiningInfo);
                        GetMessages(apiResponse.SearchJobId, logMiningInfo.CurrentMessageCount);
                        break;
                }
            }
            else if (apiResponse.StatusCode == HttpStatusCode.TooManyRequests)
                GetJobStatus(apiResponse.SearchJobId);
            // else other error handling here
        }

        private void GetMessagesCallback(ApiResponse apiResponse)
        {
            var logMiningInfo = _memoryCache.GetOrCreate<LogMiningInfo>(apiResponse.SearchJobId, (c) => new LogMiningInfo());

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var messageResponse =
                    JsonConvert.DeserializeObject<LogMessageResponse>(apiResponse.Content);
                // Store messages to DB

                logMiningInfo.CurrentMessageCount += messageResponse.Messages.Count;

                if (logMiningInfo.CurrentMessageCount < logMiningInfo.TotalMessageCount)
                {
                    _memoryCache.Set<LogMiningInfo>(apiResponse.SearchJobId, logMiningInfo);
                    GetMessages(apiResponse.SearchJobId, logMiningInfo.CurrentMessageCount);
                }
                else
                    DeleteSearchJob(apiResponse.SearchJobId);
            }
            else if (apiResponse.StatusCode == HttpStatusCode.TooManyRequests)
                GetMessages(apiResponse.SearchJobId, logMiningInfo.CurrentMessageCount);
            // else other error handling here
        }

        private void DeleteSearchJobCallback(ApiResponse apiResponse)
        {
            if (apiResponse.StatusCode == HttpStatusCode.TooManyRequests)
                DeleteSearchJob(apiResponse.SearchJobId);
            // else other error handling here
        }

        private void ScheduleApiCall(ApiData apiData)
        {
            _jobClient.Schedule(() => 
                _apiCall.CallApiAsync(apiData), TimeSpan.FromMilliseconds(250));
        }
    }
}
