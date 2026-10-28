using SpeakingClub.Services.Reports;
using static SpeakingClub.Services.Reports.ItemAnalysis;

namespace SpeakingClub.Tests;

// A made-up class of 40 students (ability = index 0…39) with problems planted in the questions,
// the same kinds the seeded test data uses: a too easy question, a dead distractor and a wrong answer key.
public class ItemAnalysisTests
{
    private static QuestionInput Question(int id, int key, params int[] distractors) =>
        new(id, "Q" + id, new[] { new OptionInput(key, "key", true) }.Concat(distractors.Select(d => new OptionInput(d, "d" + d, false))).ToList());

    private static readonly List<QuestionInput> Quiz = new()
    {
        Question(1, 11, 12, 13),   // ordinary: right from ability 20 up
        Question(2, 21, 22, 23),   // far too easy, option 23 never chosen
        Question(3, 31, 32, 33),   // wrong key: the strong students choose 32
        Question(4, 41, 42, 43),   // every tenth student skips it
        // Four clean questions (right from ability 8, 16, 24, 32) so the total score follows ability
        Question(5, 51, 52, 53), Question(6, 61, 62, 63), Question(7, 71, 72, 73), Question(8, 81, 82, 83)
    };

    private static ResponseInput Chose(int answerId, int key, int seconds = 20) => new(answerId, answerId == key, false, seconds);
    private static readonly ResponseInput Skip = new(null, null, true, 0);

    private static AttemptInput Student(int i, bool timed = false, int seconds = 20)
    {
        var responses = new Dictionary<int, ResponseInput>
        {
            [1] = Chose(i >= 20 ? 11 : i % 2 == 0 ? 12 : 13, 11, seconds),
            [2] = Chose(i == 0 ? 22 : 21, 21, seconds),
            [3] = Chose(i < 15 ? 31 : 32, 31, seconds),
            [4] = i % 10 == 0 ? Skip : Chose(i >= 10 ? 41 : 42, 41, seconds)
        };
        for (var q = 5; q <= 8; q++)
            responses[q] = Chose(i >= 8 * (q - 4) ? q * 10 + 1 : q * 10 + 2 + i % 2, q * 10 + 1, seconds);
        return new(1000 + i, "u" + i, timed, responses);
    }

    private static List<AttemptInput> Class(int n = 40) => Enumerable.Range(0, n).Select(i => Student(i)).ToList();

    [Fact]
    public void Difficulty_and_skips_are_counted_per_question()
    {
        var result = Analyze(Quiz, Class());
        Assert.Equal(40, result.Attempts);
        Assert.Equal(0, result.Excluded);
        Assert.Equal(8, result.Questions);
        Assert.Equal(0.5, result.Items[0].P, 10);
        Assert.Equal(39 / 40.0, result.Items[1].P, 10);
        Assert.Equal(15 / 40.0, result.Items[2].P, 10);
        Assert.Equal(0.1, result.Items[3].OmitRate, 10);
        Assert.Equal(40, result.Rows.Count);
    }

    [Fact]
    public void Planted_problems_are_flagged()
    {
        var items = Analyze(Quiz, Class()).Items;
        Assert.Contains("easy", items[1].Flags);
        Assert.Contains("dead-distractor", items[1].Flags);   // option 23: 0 % of 40
        Assert.Contains("miskey", items[2].Flags);            // a distractor is chosen more often than the key
        Assert.Contains("negative", items[2].Flags);          // the key attracts the weaker students
        Assert.DoesNotContain("miskey", items[0].Flags);
        Assert.True(items[0].PointBiserial > ItemAnalysis.GoodDiscrimination);
    }

    [Fact]
    public void Distractor_shares_add_up_with_the_key()
    {
        var q1 = Analyze(Quiz, Class()).Items[0];
        Assert.Equal(1.0, q1.Options.Sum(o => o.Share), 10);
        Assert.Equal(0.5, q1.Options.Single(o => o.IsKey).Share, 10);
        Assert.Equal(1.0, q1.Options.Single(o => o.IsKey).ShareTop!.Value, 10);      // the top 27 % all chose the key
        Assert.Equal(0.0, q1.Options.Single(o => o.IsKey).ShareBottom!.Value, 10);
    }

    [Fact]
    public void Few_attempts_only_say_so()
    {
        var items = Analyze(Quiz, Class(5)).Items;
        Assert.All(items, item => Assert.Equal(new[] { "few" }, item.Flags));
    }

    [Fact]
    public void Attempts_missing_a_question_or_with_an_unknown_answer_are_left_out()
    {
        var attempts = Class();
        attempts.Add(new AttemptInput(2000, "late", false, new Dictionary<int, ResponseInput> { [1] = Chose(11, 11) }));   // before questions 2–4 existed
        var unknown = Student(25);
        attempts.Add(unknown with
        {
            SubmissionId = 2001,
            Responses = new Dictionary<int, ResponseInput>(unknown.Responses) { [3] = new(null, null, false, 20) }   // its option was deleted
        });
        var result = Analyze(Quiz, attempts);
        Assert.Equal(40, result.Attempts);
        Assert.Equal(2, result.Excluded);
    }

    [Fact]
    public void Rapid_guessing_attempts_can_be_filtered_out()
    {
        // 4 of 20 attempts answer everything in 1 s: below the 3 s threshold → effort 0
        var attempts = Enumerable.Range(0, 20).Select(i => Student(i * 2, timed: true, seconds: i < 4 ? 1 : 20)).ToList();
        var all = Analyze(Quiz, attempts);
        Assert.Equal(20, all.Attempts);
        Assert.Equal(0.2, all.LowEffortShare!.Value, 10);
        Assert.Equal(0.0, all.Rows.First(r => r.SubmissionId == 1000).Rte!.Value, 10);
        Assert.Equal(1.0, all.Rows.First(r => r.SubmissionId == 1000 + 38).Rte!.Value, 10);

        var filtered = Analyze(Quiz, attempts, excludeLowEffort: true);
        Assert.Equal(16, filtered.Attempts);
    }

    [Fact]
    public void Reliability_and_measurement_error_are_reported_in_percent_points()
    {
        var result = Analyze(Quiz, Class());
        Assert.NotNull(result.Kr20);
        Assert.NotNull(result.SdPercent);
        Assert.Equal(result.SdPercent!.Value * Math.Sqrt(1 - Math.Clamp(result.Kr20!.Value, 0, 1)), result.SemPercent!.Value, 10);
    }

    [Fact]
    public void Nothing_to_analyse_gives_empty_figures()
    {
        var result = Analyze(Quiz, new List<AttemptInput>());
        Assert.Equal(0, result.Attempts);
        Assert.Null(result.MeanPercent);
        Assert.Null(result.Kr20);
        Assert.Null(result.LowEffortShare);
    }
}
