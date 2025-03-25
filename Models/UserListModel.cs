using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class UserListModel
    {
        public required string Id { get; set; }
        public string? Email { get; set; }
        public bool IsLockedOut { get; set; }
    }
}