using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    #nullable disable
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Lütfen adınızı giriniz.")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Lütfen e-posta adresinizi giriniz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Lütfen mesajınızı giriniz.")]
        public string Message { get; set; }
    }
}