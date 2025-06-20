using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class TagCreateViewModel
    {
        #nullable disable
        [Required, MaxLength(50)]
        public string Name { get; set; }
    }
}