using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SumoLogicHangfire.Configurations;
using SumoLogicHangfire.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Linq;

namespace SumoLogicHangfire.Services
{
    public class SumoLogMining : ISumoLogMining
    {
        private const string JsonMediaType = "application/json";
        private const int MessageBatchLimit = 1000;

        private readonly SumoLogicSettings _sumoLogicSettings;
        private readonly IBackgroundJobClient _jobClient;
        private readonly IApiCallService _apiCall;
        private readonly IMemoryCache _memoryCache;
        ILogger _logger;

        public SumoLogMining(
            SumoLogicSettings sumoLogicSettings,
            IBackgroundJobClient jobClient, 
            IApiCallService apiCall, 
            ILogger<SumoLogMining> logger, 
            IMemoryCache memoryCache)
        {
            _sumoLogicSettings = sumoLogicSettings;
            _jobClient = jobClient;
            _apiCall = apiCall;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public void MineLog(MineLogRequest mineLogRequest, Uri callback)
        {
            var searchJobRequest = new SearchJobRequest
            {
                Query = mineLogRequest.Query,
                From = mineLogRequest.From.ToUnixTimeMilliseconds(),
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
                RequestUri = new Uri(_sumoLogicSettings.BaseUri, "search/jobs"),
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
                RequestUri = new Uri(_sumoLogicSettings.BaseUri, $"search/jobs/{jobId}"),
                SearchJobId = jobId
            };
            ScheduleApiCall(apiData);
        }

        private void GetMessages(string jobId, int offset)
        {
            var getMessagesUri = new Uri(
                _sumoLogicSettings.BaseUri, $"search/jobs/{jobId}/messages?offset={offset}&limit={MessageBatchLimit}");

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
                RequestUri = new Uri(_sumoLogicSettings.BaseUri, $"search/jobs/{jobId}"),
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
        }

        private void CreateSearchJobCallback(ApiResponse apiResponse)
        {
            _logger.LogInformation(JsonConvert.SerializeObject(new { apiResponse.StatusCode, apiResponse.Content }));

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
            _logger.LogInformation(JsonConvert.SerializeObject(new { apiResponse.StatusCode, apiResponse.Content }));

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
                        var logMiningInfo = new LogMiningState { TotalMessageCount = jobStatusResponse.MessageCount };
                        _memoryCache.Set<LogMiningState>(apiResponse.SearchJobId, logMiningInfo);
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
            _logger.LogInformation(JsonConvert.SerializeObject(new { apiResponse.StatusCode }));

            var logMiningState = _memoryCache.GetOrCreate(apiResponse.SearchJobId, (c) => new LogMiningState());

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var messageResponse =
                    JsonConvert.DeserializeObject<LogMessageResponse>(apiResponse.Content);
                // Store messages to DB

                logMiningState.CurrentMessageCount += messageResponse.Messages.Count;
                logMiningState.LogMessages.AddRange(messageResponse.Messages.Select(m => m.Map.RawMessage));
                _memoryCache.Set(apiResponse.SearchJobId, logMiningState);

                if (logMiningState.CurrentMessageCount < logMiningState.TotalMessageCount)
                    GetMessages(apiResponse.SearchJobId, logMiningState.CurrentMessageCount);
                else
                    DeleteSearchJob(apiResponse.SearchJobId);
            }
            else if (apiResponse.StatusCode == HttpStatusCode.TooManyRequests)
                GetMessages(apiResponse.SearchJobId, logMiningState.CurrentMessageCount);
            // else other error handling here
        }

        private void DeleteSearchJobCallback(ApiResponse apiResponse)
        {
            _logger.LogInformation(JsonConvert.SerializeObject(new { apiResponse.StatusCode }));

            if (apiResponse.StatusCode == HttpStatusCode.TooManyRequests)
                DeleteSearchJob(apiResponse.SearchJobId);
            // else other error handling here
        }

        private void ScheduleApiCall(ApiData apiData)
        {
            _jobClient.Enqueue(() =>
                _apiCall.CallApiAsync(apiData));
            //_jobClient.Schedule(() =>
            //    _apiCall.CallApiAsync(apiData), TimeSpan.FromMilliseconds(250));
        }
    }
}
