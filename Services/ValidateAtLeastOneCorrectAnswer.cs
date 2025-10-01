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
            // Handle the EditView model where IsCorrect is a string ("true"/"false")
            if (value is List<AnswerEditViewModel> editAnswers)
            {
                if (editAnswers.Any(a => string.Equals(a.IsCorrect, "true", StringComparison.OrdinalIgnoreCase)))
                    return ValidationResult.Success;
                // fall through to form inspection fallback below before failing
            }

            // Handle the CreateView model where IsCorrect is a bool
            if (value is List<AnswerViewModel> answers)
            {
                if (answers.Any(a => a.IsCorrect))
                    return ValidationResult.Success;
                // fall through to form inspection fallback below before failing
            }

            // Generic fallback: try to inspect enumerable items and look for an 'IsCorrect' property
            if (value is System.Collections.IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    var prop = item.GetType().GetProperty("IsCorrect");
                    if (prop == null) continue;
                    var propVal = prop.GetValue(item);
                    if (propVal is bool b && b) return ValidationResult.Success;
                    if (propVal is string s && bool.TryParse(s, out var parsed) && parsed) return ValidationResult.Success;
                    // If it's another type, try a loose conversion
                    try
                    {
                        if (propVal != null && Convert.ToBoolean(propVal)) return ValidationResult.Success;
                    }
                    catch { /* ignore conversion errors */ }
                }
                // If the model-bound values didn't show a correct answer, try to inspect the raw form data as a fallback.
                try
                {
                    var accessor = validationContext.GetService(typeof(Microsoft.AspNetCore.Http.IHttpContextAccessor)) as Microsoft.AspNetCore.Http.IHttpContextAccessor;
                    var http = accessor?.HttpContext;
                    if (http != null && http.Request != null && http.Request.HasFormContentType)
                    {
                        var form = http.Request.Form;
                        // Look for keys like Questions[0].Answers[0].AnswerText and Questions[0].Answers[0].IsCorrect
                        var answerTextRegex = new System.Text.RegularExpressions.Regex(@"Questions\[(\d+)\]\.Answers\[(\d+)\]\.AnswerText$");
                        var isCorrectRegex = new System.Text.RegularExpressions.Regex(@"Questions\[(\d+)\]\.Answers\[(\d+)\]\.IsCorrect$");

                        // Map (q,a) -> answerText
                        var map = new System.Collections.Generic.Dictionary<(int q, int a), string>();
                        foreach (var key in form.Keys)
                        {
                            var m = answerTextRegex.Match(key);
                            if (m.Success)
                            {
                                if (int.TryParse(m.Groups[1].Value, out var qidx) && int.TryParse(m.Groups[2].Value, out var aidx))
                                {
                                    var val = form[key].ToString();
                                    map[(qidx, aidx)] = val ?? string.Empty;
                                }
                            }
                        }

                        // For each mapped answer check IsCorrect value(s)
                        foreach (var kv in map)
                        {
                            var qidx = kv.Key.q;
                            var aidx = kv.Key.a;
                            var isKey = $"Questions[{qidx}].Answers[{aidx}].IsCorrect";
                            if (form.TryGetValue(isKey, out var sval))
                            {
                                // form values may be multiple (hidden,false + checkbox,true) resulting in comma-joined string like "false,true"
                                var parts = sval.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                                foreach (var part in parts)
                                {
                                    if (bool.TryParse(part, out var pb) && pb) return ValidationResult.Success;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // swallow; we'll return the standard ValidationResult below
                }

                return new ValidationResult(ErrorMessage ?? "At least one correct answer is required per question.");
            }

            // If we reach here we couldn't interpret the value; fail validation to be safe
            return new ValidationResult(ErrorMessage ?? "At least one correct answer is required per question.");
        }
    }
}