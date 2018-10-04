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

    public interface ISumoLogMining
    {
        void MineLog(MineLogRequest mineLogRequest, Uri callback);
        void Callback(SearchApi searchApi, ApiResponse response);
    }
}
