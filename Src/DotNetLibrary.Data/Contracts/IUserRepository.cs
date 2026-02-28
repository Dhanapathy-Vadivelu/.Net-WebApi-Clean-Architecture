using DotNetLibrary.Data.Entities;
using DotNetLibrary.Shared.Responses;

namespace DotNetLibrary.Data.Contracts;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
