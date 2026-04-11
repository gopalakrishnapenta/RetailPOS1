using ReturnsService.Models;
using ReturnsService.DTOs;

namespace ReturnsService.Interfaces
{
    public interface IReturnService
    {
        Task<IEnumerable<Return>> GetAllReturnsAsync();
        Task<Return> InitiateReturnAsync(ReturnInitiationDto returnDto);
        Task ApproveReturnAsync(int id, string? note);
        Task RejectReturnAsync(int id, string? note);
    }
}
