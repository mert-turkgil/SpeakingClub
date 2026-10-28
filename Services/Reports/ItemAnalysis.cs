using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Question-by-question analysis of one quiz (Admin → Sınav sonuçları → Soru analizi): difficulty,
    /// discrimination, distractors, reliability and answering effort. Pure: the caller loads the data
    /// (ExamReportService) and the tests feed made-up attempts.
    /// </summary>
    public static class ItemAnalysis
    {
        // Thresholds shown to teachers (Moodle/Canvas practice; see docs/DESIGN.md "Exam analytics")
        public const double HardBelow = 0.30;
        public const double EasyAbove = 0.90;
        public const double GoodDiscrimination = 0.25;
        public const double DeadDistractorShare = 0.05;
        public const double LowEffortRte = 0.90;
        public const int MinAttemptsForStatistics = 10;

        public record OptionInput(int Id, string Text, bool IsKey);
        public record QuestionInput(int Id, string Text, IReadOnlyList<OptionInput> Options);
        /// <param name="Correct">null: unknown (the chosen option was deleted later)</param>
        public record ResponseInput(int? AnswerId, bool? Correct, bool Skipped, int Seconds);
        public record AttemptInput(int SubmissionId, string UserId, bool TimesRecorded, IReadOnlyDictionary<int, ResponseInput> Responses);

        public record OptionRow(int AnswerId, string Text, bool IsKey, double Share, double? ShareTop, double? ShareBottom, double? PointBiserial);
        public record ItemRow(int QuestionId, int Position, string Text, int N, double P, double? PointBiserial, double? Discrimination,
            double OmitRate, double? MedianSeconds, double? RapidRate, IReadOnlyList<OptionRow> Options, IReadOnlyList<string> Flags);
        public record AttemptRow(int SubmissionId, string UserId, int Total, IReadOnlyList<int?> Cells, double? GuttmanErrors, double? Rte,
            int RapidAnswers, int TimedAnswers);
        public record Result(int Attempts, int Excluded, int Questions, double? MeanPercent, double? MedianPercent, double? SdPercent,
            double? Kr20, double? SemPercent, IReadOnlyList<ItemRow> Items, IReadOnlyList<AttemptRow> Rows, double? LowEffortShare);

        /// <summary>
        /// Analyses the attempts that answered (or skipped) every current question ("complete cases"); attempts made
        /// before a question was added, or with an unknown result, are counted as excluded.
        /// With <paramref name="excludeLowEffort"/> attempts with response-time effort below 0.90 are left out
        /// ("motivation filtering"), so rushed attempts don't distort the question figures.
        /// </summary>
        public static Result Analyze(IReadOnlyList<QuestionInput> questions, IReadOnlyList<AttemptInput> attempts, bool excludeLowEffort = false)
        {
            var k = questions.Count;

            // Per-question mean time over answered, timed responses → rapid-guess thresholds
            var thresholds = new double?[k];
            for (var j = 0; j < k; j++)
            {
                var times = attempts.Where(a => a.TimesRecorded)
                    .Select(a => a.Responses.TryGetValue(questions[j].Id, out var r) ? r : null)
                    .Where(r => r is { Skipped: false })
                    .Select(r => (double)r!.Seconds).ToList();
                thresholds[j] = times.Count > 0 ? ItemStatistics.RapidThreshold(times.Average()) : null;
            }

            var complete = new List<(AttemptInput Attempt, int[] Scores, ResponseInput[] Responses, double? Rte, int Rapid, int Timed)>();
            var excluded = 0;
            foreach (var attempt in attempts)
            {
                var scores = new int[k];
                var responses = new ResponseInput[k];
                var ok = true;
                for (var j = 0; j < k && ok; j++)
                {
                    if (!attempt.Responses.TryGetValue(questions[j].Id, out var r) || (!r.Skipped && r.Correct == null))
                    {
                        ok = false;
                        break;
                    }
                    responses[j] = r;
                    scores[j] = !r.Skipped && r.Correct == true ? 1 : 0;
                }
                if (!ok || k == 0)
                {
                    excluded++;
                    continue;
                }
                int rapid = 0, timed = 0;
                if (attempt.TimesRecorded)
                {
                    for (var j = 0; j < k; j++)
                    {
                        if (responses[j].Skipped || thresholds[j] is not { } limit) continue;
                        timed++;
                        if (responses[j].Seconds < limit) rapid++;
                    }
                }
                double? rte = timed > 0 ? (timed - rapid) / (double)timed : null;
                complete.Add((attempt, scores, responses, rte, rapid, timed));
            }

            var lowEffortShare = complete.Count(c => c.Rte != null) is var withTimes and > 0
                ? complete.Count(c => c.Rte < LowEffortRte) / (double)withTimes
                : (double?)null;
            if (excludeLowEffort)
                complete = complete.Where(c => c.Rte is null or >= LowEffortRte).ToList();

            var n = complete.Count;
            var totals = complete.Select(c => (double)c.Scores.Sum()).ToList();
            var tieBreak = complete.Select(c => c.Attempt.SubmissionId).ToList();
            var (upper, lower) = ItemStatistics.UpperLowerGroups(totals, tieBreak);

            var items = new List<ItemRow>();
            for (var j = 0; j < k; j++)
            {
                var question = questions[j];
                var itemScores = complete.Select(c => (double)c.Scores[j]).ToList();
                var rest = complete.Select((c, i) => totals[i] - c.Scores[j]).ToList();
                var p = n > 0 ? itemScores.Average() : 0;   // n = 0 is shown as "too few attempts" anyway
                var rpb = ItemStatistics.Pearson(itemScores, rest);
                double? d = upper.Count > 0 && lower.Count > 0
                    ? upper.Average(i => (double)complete[i].Scores[j]) - lower.Average(i => (double)complete[i].Scores[j])
                    : null;
                var omit = n > 0 ? complete.Count(c => c.Responses[j].Skipped) / (double)n : 0;

                var answeredTimes = complete.Where(c => c.Attempt.TimesRecorded && !c.Responses[j].Skipped)
                    .Select(c => (double)c.Responses[j].Seconds).ToList();
                double? median = answeredTimes.Count > 0 ? ItemStatistics.Median(answeredTimes) : null;
                double? rapidRate = thresholds[j] is { } limit && answeredTimes.Count > 0
                    ? answeredTimes.Count(t => t < limit) / (double)answeredTimes.Count
                    : null;

                var options = question.Options.Select(o =>
                {
                    var chose = complete.Select(c => c.Responses[j].AnswerId == o.Id ? 1.0 : 0.0).ToList();
                    return new OptionRow(o.Id, o.Text, o.IsKey,
                        n > 0 ? chose.Average() : 0,
                        upper.Count > 0 ? upper.Average(i => chose[i]) : null,
                        lower.Count > 0 ? lower.Average(i => chose[i]) : null,
                        ItemStatistics.Pearson(chose, rest));
                }).ToList();

                items.Add(new ItemRow(question.Id, j + 1, question.Text, n, p, rpb, d, omit, median, rapidRate, options,
                    Flags(n, p, rpb, options, rapidRate)));
            }

            // Matrix rows (student × question) with Guttman errors against the class's order of difficulty
            var easiestFirst = Enumerable.Range(0, k).OrderByDescending(j => items[j].P).ThenBy(j => j).ToList();
            var rows = complete.Select(c => new AttemptRow(
                c.Attempt.SubmissionId, c.Attempt.UserId, c.Scores.Sum(),
                Enumerable.Range(0, k).Select(j => c.Responses[j].Skipped ? (int?)null : c.Scores[j]).ToList(),
                ItemStatistics.GuttmanErrorRate(easiestFirst.Select(j => c.Scores[j]).ToList()),
                c.Rte, c.Rapid, c.Timed)).ToList();

            var percents = totals.Select(t => k > 0 ? t * 100.0 / k : 0).ToList();
            var kr20 = ItemStatistics.Kr20(complete.Select(c => c.Scores).ToList());
            var sd = n > 0 ? ItemStatistics.StandardDeviation(percents) : (double?)null;
            return new Result(n, excluded, k,
                n > 0 ? percents.Average() : null,
                n > 0 ? ItemStatistics.Median(percents) : null,
                sd, kr20, sd is { } s ? ItemStatistics.Sem(s, kr20) : null,
                items, rows, lowEffortShare);
        }

        // Codes the page explains in plain words (Views/QuizMonitor, wwwroot/js/admin-reports.js)
        private static List<string> Flags(int n, double p, double? rpb, IReadOnlyList<OptionRow> options, double? rapidRate)
        {
            var flags = new List<string>();
            if (n < MinAttemptsForStatistics) { flags.Add("few"); return flags; }
            if (p < HardBelow) flags.Add("hard");
            if (p > EasyAbove) flags.Add("easy");
            if (rpb is { } r)
            {
                if (r <= 0) flags.Add("negative");
                else if (r < GoodDiscrimination) flags.Add("weak");
            }
            var key = options.Where(o => o.IsKey).ToList();
            var distractors = options.Where(o => !o.IsKey).ToList();
            if (key.Count > 0 && distractors.Count > 0)
            {
                var keyShare = key.Sum(o => o.Share);
                var topDistractor = distractors.MaxBy(o => o.Share)!;
                if (topDistractor.Share > keyShare || (rpb is <= 0 && topDistractor.PointBiserial is > 0))
                    flags.Add("miskey");
                if (distractors.Any(o => o.PointBiserial is > 0.05) && !flags.Contains("miskey"))
                    flags.Add("strong-distractor");
                if (n >= 20 && distractors.Any(o => o.Share < DeadDistractorShare))
                    flags.Add("dead-distractor");
            }
            if (rapidRate is > 0.20) flags.Add("rapid");
            return flags;
        }
    }
}
