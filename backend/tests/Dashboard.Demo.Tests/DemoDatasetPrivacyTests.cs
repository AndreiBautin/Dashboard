using System.Text;
using System.Text.RegularExpressions;

namespace Dashboard.Demo.Tests;

/// <summary>
/// The deployed demo is public. These tests are the barrier that keeps
/// personal data out of it.
///
/// They are written against the *seeded store* rather than against the source
/// file, on purpose: reading the fixture's own declarations would only prove
/// that the literals in one file look clean, whereas walking the store proves
/// that whatever actually reaches the browser is clean — including anything a
/// future change might compute, concatenate, or derive on the way in.
///
/// The specific values this scans for are the ones that were genuinely
/// present in this repository's history before it was made public. A
/// regression here means real data has found its way back into the fixture.
/// </summary>
public class DemoDatasetPrivacyTests
{
    // Must match the clock the application services read. They call
    // DateTime.UtcNow directly rather than taking an injected clock, so a
    // hardcoded date here disagrees with them either side of UTC midnight.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string SeededContent()
    {
        var store = new DemoStore();
        DemoSeeder.FillIfEmpty(store, Today);

        var builder = new StringBuilder();
        foreach (var friend in DemoDataset.Friends)
        {
            builder.AppendLine(friend.Name);
            builder.AppendLine(friend.Notes ?? string.Empty);
        }

        foreach (var category in DemoDataset.Categories)
        {
            builder.AppendLine(category.Name);
            foreach (var metric in category.Metrics)
            {
                builder.AppendLine(metric.Name);
                builder.AppendLine(metric.Unit);
                foreach (var value in metric.Values)
                {
                    builder.AppendLine(value?.ToString() ?? string.Empty);
                }
            }
        }

        return builder.ToString();
    }

    [Theory]
    // Email addresses, phone numbers, and URLs are the three shapes that most
    // often smuggle a real identity into a fixture that otherwise looks fine.
    [InlineData(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "an email address")]
    [InlineData(@"\+?\d{1,2}[-. ]?\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}", "a phone number")]
    [InlineData(@"https?://", "a URL")]
    [InlineData(@"(?i)\b(password|secret|api[_-]?key|token|bearer)\b", "a credential-shaped word")]
    [InlineData(@"\b\d{3}-\d{2}-\d{4}\b", "a national ID number")]
    public void SeededContent_ContainsNothingThatLooksPersonal(string pattern, string description)
    {
        var matches = Regex.Matches(SeededContent(), pattern);

        Assert.True(
            matches.Count == 0,
            $"The demo fixture contains what looks like {description}: " +
            string.Join(", ", matches.Select(match => $"\"{match.Value}\"")));
    }

    [Theory]
    // The actual figures that were committed to this repository's history
    // before it was published. If any of these ever reappears, the scrub has
    // been undone.
    [InlineData("236200")]
    [InlineData("236_200")]
    [InlineData("31193")]
    [InlineData("31_193")]
    [InlineData("74095")]
    [InlineData("74_095")]
    public void SeededContent_DoesNotContainThePreviouslyCommittedRealFigures(string figure)
    {
        Assert.DoesNotContain(figure, SeededContent(), StringComparison.Ordinal);
    }

    [Fact]
    public void SeededFriendNames_AreNotTheRealOnesThatWereCommitted()
    {
        string[] previouslyCommitted = ["Alex", "Saul", "Steve", "Tyler"];
        var seededNames = DemoDataset.Friends.Select(friend => friend.Name).ToList();

        foreach (var name in previouslyCommitted)
        {
            Assert.DoesNotContain(name, seededNames);
        }
    }
}
