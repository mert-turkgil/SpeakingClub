using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxSize;
        public MaxFileSizeAttribute(int maxSize) => _maxSize = maxSize;

        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxSize)
                {
                    return new ValidationResult($"Maximum allowed file size is {_maxSize/1024/1024}MB");
                }
            }
            return ValidationResult.Success!;
        }
    }
}