using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SumoLogicHangfire.Models
{
    public class ApiResponse
    {
        public string RequestContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string SearchJobId { get; set; }
        public string Content { get; set; }
    }
}
