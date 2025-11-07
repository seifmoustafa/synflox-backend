using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces
{
    public interface IUserRepository : IBaseRepository<Guid, User>
    {
        Task<User> GetByUserNameAsync(string username);

        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    }
}
