using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SumoLogicHangfire.Models;
using Hangfire;
using SumoLogicHangfire.Services;

namespace SumoLogicHangfire.Controllers
{
    [Route("api/v1/[controller]")]
    public class LogsController : Controller
    {
        private readonly ISumoLogic _sumoLogic;
        private readonly IBackgroundJobClient _jobClient;
        private readonly ILogger _logger;

        private const string MineStatusCompleted = "COMPLETED";
        private const string MineStatusCancelled = "CANCELLED";

        public LogsController(ISumoLogic sumoLogic, IBackgroundJobClient jobClient, ILogger<LogsController> logger)

        {
            _jobClient = jobClient;
            _logger = logger;
            _sumoLogic = sumoLogic;
        }

        [HttpPost("mine")]
        public IActionResult Mine([FromBody]MineLogRequest mineLogRequest, [FromBody]Uri callback)
        {
            //var logRequest = new LogRequest
            //{
            //    Id = Guid.NewGuid(),
            //    Query = query
            //};

            //var location = Url.RouteUrl("LogMineStatus", new { uuid = logRequest.Id });
            //return Accepted(location);

            _sumoLogic.MineLog(mineLogRequest, callback);
            return Ok();
        }

        [HttpGet("mine/{uuid:guid}", Name = "LogMineStatus")]
        public IActionResult MineStatus(Guid mineId)
        {
            var mineStatus = new MineStatus()
            {
                State = MineStatusCompleted,
                MessageCount = 100
            };
            return Ok(mineStatus);
        }

        [HttpGet("mine/{uuid:guid}/result")]
        public IActionResult MineJobResult(Guid searchJobId)
        {
            var traceLogs = new TraceLog[] {
                new TraceLog() {
                    Id = Guid.NewGuid(),
                    ComponentName = "ComponentName 1",
                    HasException = true,
                    Log = "Log 1",
                    TraceId = Guid.NewGuid().ToString(),
                    SearchJobId = searchJobId
                },
                new TraceLog() {
                    Id = Guid.NewGuid(),
                    ComponentName = "ComponentName 2",
                    HasException = false,
                    Log = "Log 2",
                    TraceId = Guid.NewGuid().ToString(),
                    SearchJobId = searchJobId
                }
            };

            return Ok(traceLogs);
        }
    }
}
