using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    public class RequiredNonEmptyListAttribute: ValidationAttribute
    {
        #nullable disable
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var list = value as List<int>;
            if (list == null || !list.Any())
            {
                return new ValidationResult(ErrorMessage ?? "At least one category must be selected.");
            }

            return ValidationResult.Success;
        }
    }
}