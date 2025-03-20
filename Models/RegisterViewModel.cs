using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        public required string FirstName { get; set; }
        
        [Required(ErrorMessage = "Last Name is required.")]
        [Display(Name = "Last Name")]
        public required string LastName { get; set; }
        
        [Required(ErrorMessage = "Age is required.")]
        [Range(1, 120, ErrorMessage = "Please enter a valid age.")]
        public int Age { get; set; }
        
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public required string Email { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public required string Password { get; set; }
        
        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public required string ConfirmPassword { get; set; }
    }
}