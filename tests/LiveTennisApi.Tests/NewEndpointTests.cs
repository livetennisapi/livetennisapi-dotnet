using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LiveTennisApi.Models;
using Xunit;

namespace LiveTennisApi.Tests
{
    /// <summary>Coverage for the surface added in 1.1.0.</summary>
    public class NewEndpointTests
    {
        // --- Match model: tour, tournament_id, round_code, withdrew, tape ------

        [Fact]
        public async Task Matches_NewFields_Deserialize()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("matches_fields.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListCompletedMatchesAsync();

            var retired = page.Data[0];
            Assert.Equal("atp", retired.Tour);                 // filter vocabulary, groupable
            Assert.Equal("cincinnati-atp", retired.TournamentId);
            Assert.Equal("R16", retired.RoundCode);
            Assert.Equal("Retired", retired.EventStatus);
            Assert.Equal(1, retired.Winner);
            Assert.Equal(2, retired.Withdrew);                 // the withdrawer is the loser
            Assert.Equal("from_start", retired.Tape!.Coverage);
            Assert.Equal(141, retired.Tape.Rows);

            var exhibition = page.Data[1];
            Assert.Null(exhibition.Tour);                      // exhibitions carry no tour — a real state
            Assert.Null(exhibition.TournamentId);
            Assert.Equal("Q", exhibition.RoundCode);           // unnumbered qualifying round
            Assert.Null(exhibition.Withdrew);
            Assert.Equal("reconstructed", exhibition.Tape!.Coverage);

            // Meta gained total/has_more; total stays null where uncountable.
            Assert.Null(page.Meta!.Total);
            Assert.False(page.Meta.HasMore);
        }

        // --- New list filters --------------------------------------------------

        [Fact]
        public async Task ListMatches_PlayerFromToCountry_ReachTheWire()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListMatchesAsync(
                MatchStatus.Completed,
                players: new[] { 810, 12 },
                from: "2026-08-01",
                to: "2026-08-07",
                country: "sui");

            var query = handler.Requests[0].Query;
            Assert.Contains("player=810", query);
            Assert.Contains("player=12", query);   // repeatable, one param per id
            Assert.Contains("from=2026-08-01", query);
            Assert.Contains("to=2026-08-07", query);
            Assert.Contains("country=sui", query);
        }

        [Fact]
        public async Task ListMatches_MoreThan50Players_ThrowsBeforeTheWire()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ListMatchesAsync(players: Enumerable.Range(1, 51)));
            Assert.Equal(0, handler.CallCount); // rejected client-side, per the API's cap
        }

        [Fact]
        public async Task CompletedMatches_TourFilter_ReachesTheWire()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListCompletedMatchesAsync(tour: Tour.Juniors, from: "2026-07-01");

            var query = handler.Requests[0].Query;
            Assert.Contains("tour=juniors", query);
            Assert.Contains("from=2026-07-01", query);
            Assert.StartsWith("/api/public/v1/history/matches", handler.Requests[0].Uri.AbsolutePath);
        }

        // --- /h2h --------------------------------------------------------------

        [Fact]
        public async Task H2H_Deserializes_BothEras()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("h2h.json")));
            using var client = TestSupport.ClientOver(handler);

            var h2h = await client.GetHeadToHeadAsync("djokovic", "nadal");

            Assert.Contains("p1=djokovic", handler.Requests[0].Query);
            Assert.Contains("p2=nadal", handler.Requests[0].Query);

            Assert.Equal("Novak Djokovic", h2h!.Players!.P1!.Name);
            Assert.Equal(31, h2h.Totals!.P1Wins);
            Assert.Equal(1, h2h.Totals.Undecided);   // counted apart from the wins
            Assert.Equal(20, h2h.BySurface!["clay"].P2);

            var current = h2h.Meetings![0];
            Assert.Equal("current", current.Era);
            Assert.Equal(18234, current.MatchId);     // current rows carry our match id
            Assert.Equal("R32", current.RoundCode);
            Assert.Null(current.ArchiveMatchId);

            var archive = h2h.Meetings[1];
            Assert.Equal("archive", archive.Era);
            Assert.Equal(90211, archive.ArchiveMatchId);
            Assert.Equal("6-2 4-6 6-2 7-6(4)", archive.Score);

            var walkover = h2h.Meetings[2];
            Assert.Equal("walkover", walkover.Outcome); // excludable via outcome
            Assert.Null(walkover.Winner);
        }

        [Fact]
        public async Task H2H_AmbiguousName_Is400WithCode()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.BadRequest, TestSupport.Fixture("error_400_ambiguous.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.GetHeadToHeadAsync("williams", "graf"));
            Assert.Equal("ambiguous_name", ex.Code);
            Assert.Contains("Serena Williams", ex.Body); // candidate list preserved on the body
        }

        // --- /history/archive/* ------------------------------------------------

        [Fact]
        public async Task ArchiveMatches_Deserialize_WithRanksAtTheTime()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("archive_matches.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListArchiveMatchesAsync(
                ArchiveTour.Atp, name: "stich", round: "F", level: "G");

            var query = handler.Requests[0].Query;
            Assert.Contains("tour=atp", query);
            Assert.Contains("name=stich", query);
            Assert.Contains("round=F", query);
            Assert.Contains("level=G", query);

            var final = page.Data[0];
            Assert.Equal("Michael Stich", final.Winner!.Name); // winner is a stored column
            Assert.Equal(6, final.Winner.Rank);                // rank AT THE TIME
            Assert.Equal(101414, final.Loser!.PlayerId);       // corpus person id, not roster
            Assert.Equal("completed", final.Outcome);
            Assert.Null(final.Stats);                          // list rows carry no stats

            Assert.Equal(1485752, page.Meta!.Total);
            Assert.True(page.Meta.HasMore);
        }

        [Fact]
        public async Task ArchiveCareer_Deserializes_WithHonestServeCoverage()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("archive_career.json")));
            using var client = TestSupport.ClientOver(handler);

            var career = await client.GetArchiveCareerAsync("sampras");

            Assert.Equal("Pete Sampras", career!.Player!.Name);
            Assert.Equal(64, career.Record!.Titles);
            Assert.Equal(101, career.Record.BySurface!["grass"].Wins);
            Assert.Equal(203, career.Record.ByLevel!["G"].Wins);
            Assert.Equal(1993, career.ByYear![1].Year);
            Assert.Equal(908, career.Serve!.MatchesWithStats); // serve stats exist from 1991 only
            Assert.Equal(9.8, career.Serve.AcesPerMatch);
        }

        // --- /rally/* ----------------------------------------------------------

        [Fact]
        public async Task RallyDetail_Deserializes_PointsAndShots()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("rally_match_detail.json")));
            using var client = TestSupport.ClientOver(handler);

            var detail = await client.GetRallyMatchAsync(11646, limit: 2);

            Assert.Contains("limit=2", handler.Requests[0].Query);
            Assert.Equal(11646, detail!.RallyMatchId);
            Assert.Equal(15012, detail.MatchId);          // linked to our id space here
            Assert.Equal(334, detail.Points);
            Assert.Equal(331, detail.PointsParsed);       // the per-match quality number
            Assert.Equal(334, detail.Meta!.Total);        // full point count, not page size

            var first = detail.Rally![0];
            Assert.True(first.Parsed);
            Assert.Equal("4;214,f18,b2,f1*", first.Raw);  // charter's string, verbatim
            Assert.Equal(4, first.RallyLength);
            Assert.Equal(4, first.Shots!.Count);
            Assert.Equal("serve", first.Shots[0].Stroke);
            Assert.Equal("forehand", first.Shots[1].Wing);

            var ace = detail.Rally[1];
            Assert.True(ace.IsAce);
            Assert.Equal(1, ace.RallyLength);             // an ace is 1
        }

        [Fact]
        public async Task MatchRally_ByOurId_UsesHistoryPath()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("rally_match_detail.json")));
            using var client = TestSupport.ClientOver(handler);

            await client.GetMatchRallyAsync(15012);

            Assert.StartsWith("/api/public/v1/history/matches/15012/rally", handler.Requests[0].Uri.AbsolutePath);
        }

        [Fact]
        public async Task RallyList_GenderFilter_IsSingleLetter()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListRallyMatchesAsync(player: "alcaraz", gender: Gender.Women);

            Assert.Contains("gender=W", handler.Requests[0].Query);
        }

        // --- /charting/* -------------------------------------------------------

        [Fact]
        public async Task ChartingPlayer_GenderFilter_IsWord()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Ok("{\"player\":{\"name\":\"Iga Swiatek\"},\"matches_charted\":120,\"coverage\":\"curated\",\"families\":{}}"));
            using var client = TestSupport.ClientOver(handler);

            var aggregate = await client.GetChartingPlayerAsync("swiatek", Gender.Women);

            Assert.Contains("name=swiatek", handler.Requests[0].Query);
            Assert.Contains("gender=women", handler.Requests[0].Query); // words here, letters on /rally
            Assert.Equal(120, aggregate!.MatchesCharted);
        }

        // --- /matches/{id}/statistics -------------------------------------------

        [Fact]
        public async Task Statistics_TwoFamilies_StayUnmerged()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("match_statistics.json")));
            using var client = TestSupport.ClientOver(handler);

            var stats = await client.GetMatchStatisticsAsync(22336);

            Assert.Equal("live", stats!.Coverage);
            Assert.Equal(1, stats.TiebreakGamesExcluded); // tiebreaks never enter the derived family

            var p1 = stats.Players!.P1!;
            Assert.Equal(7, p1.Measured!.Aces);           // measured only — no derived ace count exists
            Assert.Equal(88, p1.HoldPct);
            // Same quantity, two computations — a cross-check, not a duplication.
            Assert.Equal(35, p1.ServicePointsWon);
            Assert.Equal(38, p1.Measured.ServicePointsWon);

            var p2 = stats.Players.P2!;
            Assert.Null(p2.HoldPct);                      // null, never 0, when nothing was played
            Assert.Null(p2.Measured!.FirstServesIn);      // absent measured key reads as null (ITF tier-2 gap)

            // Per-family freshness on different clocks.
            Assert.Equal(4, stats.Freshness!.Derived!.AgeSeconds);
            Assert.Equal(35, stats.Freshness.Measured!.AgeSeconds);
            Assert.Null(stats.Freshness.MeasuredDivergence);
            Assert.Equal(17, stats.Freshness.Derived.Describes!.TotalGames);
        }

        [Fact]
        public async Task Statistics_403_InfersUltra()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(() => client.GetMatchStatisticsAsync(1));
            Assert.Equal("ULTRA", ex.RequiredTier);
        }

        // --- /rankings ----------------------------------------------------------

        [Fact]
        public async Task RankingsListing_Deserializes_WithPreviousRank()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("rankings_listing.json")));
            using var client = TestSupport.ClientOver(handler);

            var result = await client.ListRankingsAsync(RankingSystem.Atp, asOf: "2026-08-07");

            var query = handler.Requests[0].Query;
            Assert.Contains("system=atp", query);
            Assert.Contains("as_of=2026-08-07", query);
            Assert.DoesNotContain("player=", query); // no player ⇒ listing mode

            Assert.Equal(1, result.Data[0].Rank);
            Assert.Equal(1, result.Data[0].PreviousRank);
            Assert.Equal(3, result.Data[1].PreviousRank);
            Assert.Null(result.Data[1].PlayerId);              // unrostered row — no silent holes
            Assert.Equal("Somebody Unrostered", result.Data[1].PlayerName);
            Assert.Equal("2023-01-02", result.Meta!.Coverage!.OldestAvailable!["atp"]);
        }

        [Fact]
        public async Task RankingsListing_403_SaysPro()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(() => client.ListRankingsAsync(RankingSystem.Wta));
            Assert.Equal("PRO", ex.RequiredTier); // listing mode is PRO
        }

        [Fact]
        public async Task PlayerRankings_BuildsRepeatableParams_AndSaysUltraOn403()
        {
            var handler = new StubHttpMessageHandler(
                _ => TestSupport.Ok(TestSupport.Fixture("rankings_listing.json")),
                _ => TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            await client.GetPlayerRankingsAsync(
                new[] { 412, 763 },
                systems: new[] { RankingSystem.Atp, RankingSystem.Utr },
                asOf: "2026-01-01");

            var query = handler.Requests[0].Query;
            Assert.Contains("player=412", query);
            Assert.Contains("player=763", query);
            Assert.Contains("system=atp", query);
            Assert.Contains("system=utr", query);
            Assert.Contains("as_of=2026-01-01", query);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(
                () => client.GetPlayerRankingsAsync(new[] { 412 }));
            Assert.Equal("ULTRA", ex.RequiredTier); // per-player mode is ULTRA
        }

        [Fact]
        public async Task PlayerRankings_ValidatesIdCount()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.GetPlayerRankingsAsync(new int[0]));
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.GetPlayerRankingsAsync(Enumerable.Range(1, 51)));
            Assert.Equal(0, handler.CallCount);
        }

        // --- /ws-token ----------------------------------------------------------

        [Fact]
        public async Task WsToken_CarriesUrlAndSlateAllChannel()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("ws_token.json")));
            using var client = TestSupport.ClientOver(handler);

            var token = await client.GetWsTokenAsync();

            Assert.False(string.IsNullOrEmpty(token!.Token));
            Assert.Equal(300, token.ExpiresIn);
            Assert.Equal("wss://api.livetennisapi.com/connection/websocket", token.WsUrl);
            Assert.Equal("match:{match_id}", token.Channels!.Match);
            Assert.Equal("slate:all", token.Channels.Slate);
        }

        // --- /history/packages ---------------------------------------------------

        [Fact]
        public async Task HistoryPackages_KindAndYear_ReachTheWire()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("history_packages.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListHistoryPackagesAsync(HistoryPackageKind.Rankings, year: "2026");

            var query = handler.Requests[0].Query;
            Assert.Contains("kind=rankings", query);
            Assert.Contains("year=2026", query);

            Assert.Null(page.Data[0].Kind);               // tape packages carry no kind
            Assert.Equal("rankings", page.Data[1].Kind);
            Assert.Equal(2, page.Data[0].Files!.Count);
            Assert.False(string.IsNullOrEmpty(page.Data[0].Files![0].Sha256));
        }

        [Fact]
        public async Task HistoryPackages_DefaultTapeKind_IsOmitted()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListHistoryPackagesAsync();

            // The default keeps a tape-only client's traffic unchanged.
            Assert.DoesNotContain("kind=", handler.Requests[0].Query);
        }

        // --- per-match tape -------------------------------------------------------

        [Fact]
        public async Task Tape_Clean_PointWinnerAndTiebreaks()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("tape_clean.json")));
            using var client = TestSupport.ClientOver(handler);

            var tape = await client.GetMatchTapeAsync(15012, TapeSequence.Clean);

            Assert.Contains("sequence=clean", handler.Requests[0].Query);

            Assert.Equal("atp", tape!.Match!.Tour);
            Assert.Equal(1, tape.Tape![0].PointWinner);   // attributable transition
            Assert.Equal(2, tape.Tape[1].PointWinner);
            Assert.Null(tape.Tape[1].Timestamp);          // the reconstructed-row marker
            Assert.Null(tape.Tape[2].PointWinner);        // gap — never guessed

            // Per-set tiebreak finals, aligned to the sets; null = no breaker or unobserved close.
            Assert.Null(tape.Tiebreaks![0]);
            Assert.Equal(8, tape.Tiebreaks[1]!.P1);
            Assert.Equal(6, tape.Tiebreaks[1]!.P2);

            Assert.Equal("clean", tape.Meta!.Sequence);
            Assert.Equal("mixed", tape.Meta.PointSource);
            Assert.Equal(5, tape.Meta.RawRows);
            Assert.Equal(3, tape.Meta.Rows);              // after the clean collapse
            Assert.Single(tape.Profiles!);
        }

        // --- 429 shapes ------------------------------------------------------------

        [Fact]
        public async Task Daily429_SurfacesResetsAt()
        {
            var options = new LiveTennisApiClientOptions { MaxRetries = 0 };
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.TooManyRequests, TestSupport.Fixture("error_429_daily.json")));
            using var client = TestSupport.ClientOver(handler, options);

            var ex = await Assert.ThrowsAsync<RateLimitedException>(() => client.ListMatchesAsync());

            Assert.Equal("rate_limited", ex.Code);
            Assert.IsNotType<AbuseThrottledException>(ex);
            // The absolute instant from the body — local-midnight-derived, not a fixed UTC time.
            Assert.Equal("2026-08-07T21:00:00Z", ex.ResetsAt);
            Assert.Equal(new DateTimeOffset(2026, 8, 7, 21, 0, 0, TimeSpan.Zero), ex.ResetsAtTime);
        }

        [Fact]
        public async Task Abuse429_IsTypedWithRetryAtEpoch()
        {
            var options = new LiveTennisApiClientOptions { MaxRetries = 0 };
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.TooManyRequests, TestSupport.Fixture("error_429_abuse.json")));
            using var client = TestSupport.ClientOver(handler, options);

            var ex = await Assert.ThrowsAsync<AbuseThrottledException>(() => client.ListMatchesAsync());

            Assert.Equal("abuse_throttled", ex.Code);
            Assert.Equal(1786568400L, ex.RetryAtEpoch);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1786568400), ex.RetryAt);
            Assert.IsAssignableFrom<RateLimitedException>(ex); // catchable as the base 429 type
        }

        [Fact]
        public async Task LongRetryAfter429_IsNotRetried()
        {
            // A daily/abuse block advertises a huge Retry-After; burning retries
            // against it is what earns an abuse block, so the client must not.
            var options = new LiveTennisApiClientOptions { MaxRetries = 2 };
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.TooManyRequests, "{\"error\":\"rate_limited\"}", ("Retry-After", "86400")));
            using var client = TestSupport.ClientOver(handler, options);

            await Assert.ThrowsAsync<RateLimitedException>(() => client.ListMatchesAsync());
            Assert.Equal(1, handler.CallCount); // surfaced immediately, no retry burned
        }
    }
}
