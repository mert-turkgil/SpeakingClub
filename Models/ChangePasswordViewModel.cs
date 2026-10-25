using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Current Password")]
        public required string CurrentPassword { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "New Password"), MinLength(8)] // Identity requires 8 (Program.cs)
        public required string NewPassword { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "Confirm New Password"), Compare("NewPassword")]
        public required string ConfirmNewPassword { get; set; }
    }
}