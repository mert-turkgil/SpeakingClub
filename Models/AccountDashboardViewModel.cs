using System;
using System.Collections.Generic;

namespace SpeakingClub.Models
{
    // Account/Account ("Hesabım"): profile, quiz progress, security and staff shortcuts
    public class AccountDashboardViewModel
    {
        public string FullName { get; set; } = "";
        public string Initials { get; set; } = "?";
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public int Age { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public bool IsStaff { get; set; }      // Root, Admin or Teacher: admin panel + quiz monitor
        public bool CanDelete { get; set; }    // Root and Admin accounts cannot be deleted from here
        public bool HasPassword { get; set; }  // false for accounts created with Google
        public bool GoogleLinked { get; set; }
        // Announcement e-mails (exam results are always sent)
        public bool NewPostEmails { get; set; }
        public bool NewQuizEmails { get; set; }
        public bool EmailChoicePending { get; set; }   // signed up with Google and has not chosen yet

        // Quiz progress
        public int TotalAttempts { get; set; }
        public int DistinctQuizzes { get; set; }
        public int AverageScore { get; set; }
        public int? BestScore { get; set; }
        public int? LastScore { get; set; }
        public int? Trend { get; set; }        // last score minus the one before
        public string? EstimatedLevel { get; set; }
        public List<int> ScoreHistory { get; set; } = new();   // oldest first, at most 12
        public List<AccountAttemptViewModel> RecentAttempts { get; set; } = new();
        public AccountActiveExamViewModel? ActiveExam { get; set; }
    }

    public class AccountAttemptViewModel
    {
        public int QuizId { get; set; }
        public int SubmissionId { get; set; }
        public string QuizTitle { get; set; } = "";
        public int Score { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int DurationSeconds { get; set; }
        public string? EndReason { get; set; }
        public int AttemptNumber { get; set; }
    }

    public class AccountActiveExamViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = "";
        public int RemainingSeconds { get; set; }
    }
}
