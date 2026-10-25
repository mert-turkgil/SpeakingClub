using System;
using System.Collections.Generic;
using System.Linq;
using SpeakingClub.Entity;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Rules of the online exam, shared by the lobby, the exam room and grading.
    /// </summary>
    public static class ExamRules
    {
        // Integrity violations (leaving fullscreen, switching tabs, copying…) allowed before the exam is ended
        public const int MaxViolations = 3;

        // Answers posted this long after the deadline are still accepted (network latency, slow devices)
        public const int GraceSeconds = 60;

        // Event types the exam room reports; the ones in ViolationTypes count against MaxViolations
        public static readonly HashSet<string> EventTypes = new(StringComparer.Ordinal)
        {
            "fullscreen-exit", "tab-hidden", "window-blur", "copy", "paste", "cut", "contextmenu", "print", "devtools-key", "resume"
        };
        public static readonly HashSet<string> ViolationTypes = new(StringComparer.Ordinal)
        {
            "fullscreen-exit", "tab-hidden", "copy", "paste", "cut", "print"
        };

        private static readonly string[] Levels = { "A1", "A2", "B1", "B2", "C1", "C2" };

        /// <summary>
        /// Time limit of a quiz: the teacher's setting, or 90 seconds per question (5–120 minutes).
        /// </summary>
        public static int TimeLimitSeconds(Quiz quiz)
        {
            if (quiz.TimeLimitMinutes is > 0)
                return quiz.TimeLimitMinutes.Value * 60;
            var questions = quiz.Questions?.Count ?? 0;
            var minutes = Math.Clamp((int)Math.Ceiling(questions * 1.5), 5, 120);
            return minutes * 60;
        }

        /// <summary>
        /// CEFR level of a quiz, taken from its tags (A1…C2), or null.
        /// </summary>
        public static string? LevelOf(Quiz quiz) =>
            quiz.Tags?.Select(t => t.Name?.Trim().ToUpperInvariant())
                .FirstOrDefault(name => name != null && Levels.Contains(name));

        /// <summary>
        /// YouTube links as teachers paste them (watch?v=, youtu.be/, shorts/) only play in an iframe in their
        /// /embed/ form; anything else is returned unchanged.
        /// </summary>
        public static string? VideoEmbedUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                return url;
            var host = uri.Host.ToLowerInvariant();
            string? id = null;
            if (host.EndsWith("youtu.be"))
                id = uri.AbsolutePath.Trim('/');
            else if (host.EndsWith("youtube.com") || host.EndsWith("youtube-nocookie.com"))
            {
                if (uri.AbsolutePath.StartsWith("/embed/"))
                    return url;
                if (uri.AbsolutePath.StartsWith("/shorts/"))
                    id = uri.AbsolutePath["/shorts/".Length..].Trim('/');
                else
                    id = System.Web.HttpUtility.ParseQueryString(uri.Query)["v"];
            }
            return string.IsNullOrWhiteSpace(id) ? url : $"https://www.youtube.com/embed/{Uri.EscapeDataString(id)}";
        }

        /// <summary>
        /// Deterministic Fisher–Yates shuffle: the same seed always gives the same order, so an attempt keeps
        /// its question and answer order when the page is reloaded.
        /// </summary>
        public static List<T> Shuffle<T>(IEnumerable<T> items, int seed)
        {
            var list = items.ToList();
            var random = new Random(seed);
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        public static int Percent(int correct, int total) =>
            total <= 0 ? 0 : (int)Math.Round(correct * 100.0 / total);
    }
}
