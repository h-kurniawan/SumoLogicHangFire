using SumoLogicHangfire.Services;
using System;
using System.Net.Http;

namespace SumoLogicHangfire.Models
{
    public class ApiData
    {
        public SearchApi SearchApi { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public Uri RequestUri { get; set; }
        public string JsonPayload { get; set; }
        public string SearchJobId { get; set; }
    }
}
