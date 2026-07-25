using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces
{
    public interface IShowtimeIncidentService
    {
        Task<IncidentListViewModel> GetIndexAsync(string? scope, int page, int pageSize = 8, string? search = null);
        Task<DeclareIncidentViewModel> BuildDeclareFormAsync(Guid? showtimeId = null);
        Task<Result> DeclareAsync(DeclareIncidentViewModel form, Guid managerId);
        Task<Result> RestoreShowtimeAsync(Guid showtimeId);
    }
}
