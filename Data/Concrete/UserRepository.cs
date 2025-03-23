using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Identity;

namespace SpeakingClub.Data.Concrete
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly ApplicationDbContext _identityContext;

        public UserRepository(ApplicationDbContext identityContext) : base(identityContext)
        {
            _identityContext = identityContext;
        }

        public void Attach(User user)
        {
            _identityContext.Users.Attach(user);
        }
    }
}