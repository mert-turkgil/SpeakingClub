using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public class ValidateAtLeastOneCorrectAnswer : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var answers = value as List<AnswerViewModel>;
            if (answers?.Any(a => a.IsCorrect) != true)
            {
                return new ValidationResult("At least one correct answer is required per question");
            }
            return ValidationResult.Success;
        }
    }
}