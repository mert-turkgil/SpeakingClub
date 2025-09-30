using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public class ValidateAtLeastOneCorrectAnswer : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // THE FIX: Cast to the correct view model used in the edit form.
            var answers = value as List<AnswerEditViewModel>;

            // THE FIX: Check if any answer has its 'IsCorrect' property equal to the string "true".
            if (answers == null || !answers.Any(a => a.IsCorrect == "true"))
            {
                return new ValidationResult(ErrorMessage ?? "At least one correct answer is required per question.");
            }

            return ValidationResult.Success;
        }
    }
}