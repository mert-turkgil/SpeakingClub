using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SpeakingClub.Identity
{
    public class User : IdentityUser
    {
        #nullable disable
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Age { get; set; }
    }
}