using System.Collections.Generic;

namespace SpeakingClub.Models
{
    // A row of Admin → Kullanıcılar. CanEdit/CanDelete mirror the checks in AdminController
    // (only Root manages Root and Admin accounts; nobody deletes themselves), so the list only offers
    // what will work.
    public class UserListModel
    {
        public required string Id { get; set; }
        public string? Email { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsSelf { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
