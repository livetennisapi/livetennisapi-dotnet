using System.Net;
using System.Threading.Tasks;
using LiveTennisApi.Models;
using Xunit;

namespace LiveTennisApi.Tests
{
    /// <summary>GET /players/{id}/stats and the two profile fields added 2026-10-22.</summary>
    public class PlayerStatsTests
    {
        [Fact]
        public async Task PlayerStats_FiltersReachTheWire()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("player_stats.json")));
            using var client = TestSupport.ClientOver(handler);

            await client.GetPlayerStatsAsync(810, from: "2025-01-01", to: "2025-12-31", surface: "clay", level: "atp", last: 5);

            var request = handler.Requests[0];
            Assert.Equal("/api/public/v1/players/810/stats", request.Uri.AbsolutePath);
            Assert.Equal("?from=2025-01-01&to=2025-12-31&surface=clay&level=atp&last=5", request.Query);
        }

        [Fact]
        public async Task PlayerStats_NoFilters_SendsNoQuery()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"player\":{\"id\":1}}"));
            using var client = TestSupport.ClientOver(handler);

            var stats = await client.GetPlayerStatsAsync(1);

            Assert.Equal(string.Empty, handler.Requests[0].Query);
            Assert.Null(stats!.ServeReturn);   // absent below ULTRA stays null
            Assert.Null(stats.Records);
        }

        [Fact]
        public async Task PlayerStats_Deserializes()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("player_stats.json")));
            using var client = TestSupport.ClientOver(handler);

            var stats = await client.GetPlayerStatsAsync(810);

            Assert.Equal(810, stats!.Player!.Id);
            Assert.Equal(2, stats.Filters!.Last);
            var records = stats.Records!;
            Assert.Equal(28, records.Overall!.Wins);
            Assert.Equal(73.3, records.BySurface!["hard"].WinPct);
            Assert.Null(records.BySurface["grass"].WinPct);       // empty sample is null, never 0
            Assert.Equal(1, records.Sets!.Sample!.Excluded!["walkover"]);
            Assert.Equal(9, records.Sets.Tiebreaks!.Won);
            Assert.Equal(22, records.Sets.AfterWinningFirstSet!.Won);
            Assert.Equal(18, records.Sets.Comebacks!.MatchesLostASet);
            Assert.Equal(1, records.Withdrawals!.Walkover!.Received);
            Assert.Equal(1, records.Excluded!.Abandoned);
            Assert.Equal(2, records.Recent!.Count);
            Assert.Equal(new int?[] { 2, 1 }, records.Recent[0].Score!.Sets);
            Assert.Equal(7, records.Recent[0].Score!.Games![0][2]);
            Assert.Null(records.Recent[1].Score);
            Assert.Equal("retired", records.Recent[1].Outcome);

            var sr = stats.ServeReturn!;
            Assert.Equal(40, sr.Matches);
            Assert.Equal(35, sr.MatchesWithStats);
            Assert.Equal(5, sr.MatchesIncomplete);
            Assert.Equal(6.0, sr.Serve!.Aces!.PerMatch);
            Assert.Null(sr.Serve.DoubleFaults!.Total);
            Assert.Equal(0, sr.Serve.DoubleFaults.Matches);
            Assert.Equal(1500, sr.Serve.FirstServeIn!.Numerator);
            Assert.Null(sr.Return!.BreakPointsConverted!.Pct);
        }

        [Fact]
        public async Task PlayerStats_NullGameAndIncompleteMatches_Decode()
        {
            // A null game count is unknown, not 0, and must keep its position in the set.
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(
                "{\"player\":{\"id\":1},\"records\":{\"recent\":[{\"match_id\":7,\"won\":true,\"outcome\":\"completed\"," +
                "\"score\":{\"sets\":[2,0],\"games\":[[6,null],[4,0]]}}]}," +
                "\"serve_return\":{\"matches\":1,\"matches_with_stats\":0,\"matches_incomplete\":1}}"));
            using var client = TestSupport.ClientOver(handler);

            var stats = await client.GetPlayerStatsAsync(1);

            var games = stats!.Records!.Recent![0].Score!.Games!;
            Assert.Equal(2, games.Count);
            Assert.Equal(2, games[0].Count);
            Assert.Equal(6, games[0][0]);
            Assert.Null(games[0][1]);
            Assert.Equal(2, games[1].Count);
            Assert.Equal(new int?[] { 4, 0 }, games[1]);

            var sr = stats.ServeReturn!;
            Assert.Equal(1, sr.Matches);
            Assert.Equal(0, sr.MatchesWithStats);
            Assert.Equal(1, sr.MatchesIncomplete);
            Assert.Null(sr.Serve);
        }

        [Fact]
        public async Task PlayerStats_403_IsPro()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(() => client.GetPlayerStatsAsync(1));
            Assert.Equal("PRO", ex.RequiredTier);
        }

        [Fact]
        public async Task Player_HeightAndDoublesRanking()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(
                "{\"id\":1,\"height_cm\":188,\"doubles_ranking\":{\"rank\":42,\"points\":1200,\"observed_on\":\"2026-10-19\"}}"));
            using var client = TestSupport.ClientOver(handler);

            var player = await client.GetPlayerAsync(1);

            Assert.Equal(188, player!.HeightCm);
            Assert.Equal(42, player.DoublesRanking!.Rank);
            Assert.Equal(1200, player.DoublesRanking.Points);
            Assert.Equal("2026-10-19", player.DoublesRanking.ObservedOn);
        }

        [Fact]
        public async Task Player_FreeKey_NullFieldsStayNull()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"id\":1,\"height_cm\":null,\"doubles_ranking\":null}"));
            using var client = TestSupport.ClientOver(handler);

            var player = await client.GetPlayerAsync(1);

            Assert.Null(player!.HeightCm);
            Assert.Null(player.DoublesRanking);
        }
    }
}
