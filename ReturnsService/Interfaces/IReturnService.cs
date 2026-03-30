using ReturnsService.Models;

namespace ReturnsService.Interfaces
{
    public interface IReturnService
    {
        Task<IEnumerable<Return>> GetAllReturnsAsync();
        Task<Return> InitiateReturnAsync(Return returnRequest);
        Task ApproveReturnAsync(int id, string? note);
        Task RejectReturnAsync(int id, string? note);
    }
}
