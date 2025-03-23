using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Identity;

namespace SpeakingClub.Data.Abstract
{
    public interface IUserRepository : IGenericRepository<User>
    {
        void Attach(User user);
    }
}