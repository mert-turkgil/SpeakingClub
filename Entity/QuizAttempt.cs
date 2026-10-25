using System;

namespace SpeakingClub.Entity
{
    #nullable disable
    // One sitting of a quiz, from "Start" to submission. The server owns the clock (StartedAt/ExpiresAt),
    // so refreshing the exam page resumes the same attempt instead of restarting the timer.
    public class QuizAttempt
    {
        public Guid Id { get; set; }

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public string UserId { get; set; }

        public DateTime StartedAt { get; set; }   // UTC
        public DateTime ExpiresAt { get; set; }   // UTC
        public DateTime? SubmittedAt { get; set; } // UTC, null while the attempt is running

        // Seed for this attempt's question and answer order (stable across reloads)
        public int Seed { get; set; }

        // Autosaved answers: JSON object { "questionId": answerId }
        public string AnswersJson { get; set; }

        // Integrity monitoring: number of violations and a JSON array of { type, at } events
        public int ViolationCount { get; set; }
        public string IntegrityLog { get; set; }

        public int? QuizSubmissionId { get; set; }
    }
}
