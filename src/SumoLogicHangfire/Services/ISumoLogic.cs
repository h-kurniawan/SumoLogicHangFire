using System;
using SumoLogicHangfire.Models;

namespace SumoLogicHangfire.Services
{
    public enum SearchApi
    {
        CreateSearchJob,
        GetJobStatus,
        GetMessages,
        DeleteSearchJob
    }

    public interface ISumoLogic
    {
        void MineLog(MineLogRequest mineLogRequest, Uri callback);
        void Callback(SearchApi searchApi, ApiResponse response);
    }
}
