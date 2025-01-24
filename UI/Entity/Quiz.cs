using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UI.Entity
{
    public class Quiz
    {
        #nullable disable
        [Key]
        public int Id {get; set;}
        [Required]
        [DisplayName("Soru")]
        public string Soru { get; set; }
        [Required]
        public string Cevap { get; set; }
        [Required]
        public string[] Text { get; set; }
        [Required]
        public int Zaman { get; set; }
        [Required]
        public DateTime Date { get; set; }
    }
}