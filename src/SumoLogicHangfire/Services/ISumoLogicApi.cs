using SumoLogicHangfire.Models;
using System.Threading.Tasks;

namespace SumoLogicHangfire.Services
{
    public interface IApiCallService
    {
        Task CallApiAsync(ApiData apiData);
    }
}
