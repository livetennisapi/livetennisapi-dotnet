using System;
using System.Linq;
using System.Threading.Tasks;
using LiveTennisApi;
using LiveTennisApi.Models;

// A tiny live smoke test. Reads the key from the LIVETENNISAPI_KEY environment
// variable — never hardcode a credential.
//
//   LIVETENNISAPI_KEY=twjp_... dotnet run --project samples/LiveTennisApi.LiveSmoke
//
// It proves the models deserialize against real, live JSON: it prints a match
// with string points and per-set games, hunts for a doubles match (UPPERCASE
// opaque tour, null data_completeness) and for a match whose score.server is
// null.

var key = Environment.GetEnvironmentVariable("LIVETENNISAPI_KEY");
if (string.IsNullOrWhiteSpace(key))
{
    Console.Error.WriteLine("Set LIVETENNISAPI_KEY to run the live smoke test.");
    return 1;
}

using var client = new LiveTennisApiClient(key!);

var health = await client.HealthAsync();
Console.WriteLine($"health: status={health?.Status} version={health?.Version}");

Console.WriteLine();
Console.WriteLine("== live matches (first 5) ==");
var live = await client.ListMatchesAsync(MatchStatus.Live, limit: 5);
Console.WriteLine($"meta.count={live.Meta?.Count} returned={live.Data.Count}");
foreach (var m in live.Data)
{
    var s = m.Score;
    var points = s?.Points is null ? "-" : string.Join(",", s.Points);
    var games = s?.Games is null ? "-" : string.Join(" | ", s.Games.Select(g => "[" + string.Join(",", g) + "]"));
    Console.WriteLine(
        $"  #{m.Id} {(m.IsDoubles == true ? "DOUBLES" : "singles")} {Name(m)} | " +
        $"server={FmtInt(s?.Server)} points=[{points}] games={games}");
}

Console.WriteLine();
Console.WriteLine("== a DOUBLES match (opaque UPPERCASE tour + null data_completeness) ==");
var doubles = await FindAsync(client, m => m.IsDoubles == true);
if (doubles is null)
{
    Console.WriteLine("  (no doubles match found in the sampled pages right now)");
}
else
{
    var team = doubles.Players?.P1;
    var dc = team?.DataCompleteness;
    Console.WriteLine($"  #{doubles.Id} {Name(doubles)}");
    Console.WriteLine($"  p1.name={team?.Name}");
    Console.WriteLine($"  p1.tour={Quote(team?.Tour)}  is_doubles_team={team?.IsDoublesTeam}");
    Console.WriteLine($"  p1.data_completeness: known={FmtInt(dc?.Known)} of={FmtInt(dc?.Of)} note={Quote(dc?.Note)}");
}

Console.WriteLine();
Console.WriteLine("== a match with score.server == null ==");
var nullServer = await FindAsync(
    client,
    m => m.Score is not null && m.Score.Server is null,
    MatchStatus.Live, MatchStatus.Completed, MatchStatus.Upcoming);
if (nullServer is null)
{
    Console.WriteLine("  (no null-server match found in the sampled pages right now)");
}
else
{
    Console.WriteLine($"  #{nullServer.Id} status={nullServer.Status} winner={FmtInt(nullServer.Winner)} " +
                      $"server={FmtInt(nullServer.Score?.Server)} sets=[{Join(nullServer.Score?.Sets)}]");
}

Console.WriteLine();
Console.WriteLine("== an UPCOMING match (score == null) ==");
var upcoming = await client.ListMatchesAsync(MatchStatus.Upcoming, limit: 3);
foreach (var m in upcoming.Data.Take(3))
{
    Console.WriteLine($"  #{m.Id} {Name(m)} score-is-null={m.Score is null}");
}

Console.WriteLine();
Console.WriteLine("== 403 handling: request ULTRA analysis on a free key ==");
try
{
    await client.GetMatchAnalysisAsync(live.Data.FirstOrDefault()?.Id ?? 1);
    Console.WriteLine("  (unexpectedly succeeded — key may be ULTRA)");
}
catch (UpgradeRequiredException ex)
{
    Console.WriteLine($"  caught UpgradeRequiredException: status={ex.StatusCode} code={ex.Code} requiredTier={ex.RequiredTier}");
}

Console.WriteLine();
Console.WriteLine("smoke OK");
return 0;

static string Name(Match m) => $"{m.Players?.P1?.Name} vs {m.Players?.P2?.Name}";
static string FmtInt(int? v) => v?.ToString() ?? "null";
static string Quote(string? v) => v is null ? "null" : "\"" + v + "\"";
static string Join(System.Collections.Generic.IReadOnlyList<int>? v) => v is null ? "" : string.Join(",", v);

static async Task<Match?> FindAsync(LiveTennisApiClient client, Func<Match, bool> predicate, params MatchStatus[] statuses)
{
    var order = statuses.Length > 0 ? statuses : new[] { MatchStatus.Live, MatchStatus.Upcoming, MatchStatus.Completed };
    foreach (var status in order)
    {
        var page = await client.ListMatchesAsync(status, limit: 50);
        var hit = page.Data.FirstOrDefault(predicate);
        if (hit is not null)
        {
            return hit;
        }
    }

    return null;
}
