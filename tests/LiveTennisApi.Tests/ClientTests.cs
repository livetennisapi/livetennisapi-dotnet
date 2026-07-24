using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LiveTennisApi.Models;
using Xunit;

namespace LiveTennisApi.Tests
{
    public class ClientTests
    {
        // --- REQUIRED: an upcoming match deserializes with a null Score ---------

        [Fact]
        public async Task ListMatches_Upcoming_ScoreIsNull_DoesNotThrow()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("matches_upcoming.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListMatchesAsync(MatchStatus.Upcoming);

            Assert.NotEmpty(page.Data);
            Assert.All(page.Data, m => Assert.Equal("upcoming", m.Status));
            // The whole score object is null on an upcoming match — this must not throw.
            Assert.All(page.Data, m => Assert.Null(m.Score));
            // And the status filter reached the wire.
            Assert.Contains("status=upcoming", handler.Requests[0].Query);
        }

        // --- REQUIRED: a 403 becomes UpgradeRequiredException -------------------

        [Fact]
        public async Task Analysis_403_ThrowsUpgradeRequired_WithTierAndCode()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(() => client.GetMatchAnalysisAsync(1));

            Assert.Equal(403, ex.StatusCode);
            Assert.Equal("upgrade_required", ex.Code);
            Assert.Equal("ULTRA", ex.RequiredTier); // inferred from the /analysis path
            Assert.IsAssignableFrom<LiveTennisApiException>(ex);
        }

        // --- live score: string points, player-major multi-set games -----------

        [Fact]
        public async Task LiveScore_PointsAreStrings_GamesArePlayerMajor()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("matches_live.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListMatchesAsync(MatchStatus.Live);
            var score = page.Data[0].Score;

            Assert.NotNull(score);
            Assert.NotNull(score!.Points);
            Assert.All(score.Points!, p => Assert.IsType<string>(p)); // points are strings
            Assert.NotNull(score.Games);
            Assert.Equal(2, score.Games!.Count);                       // [p1, p2]
            // Games sub-arrays are per-set and grow together.
            Assert.Equal(score.Games[0].Count, score.Games[1].Count);
            var set0 = score.GamesForSet(0);
            Assert.Equal(score.Games[0][0], set0.P1);
            Assert.Equal(score.Games[1][0], set0.P2);
            Assert.Equal((null, null), score.GamesForSet(99));         // out of range guarded
        }

        // --- doubles: null known/of + note, opaque UPPERCASE tour --------------

        [Fact]
        public async Task Doubles_DataCompletenessKnownOfNull_WithNote_TourOpaque()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("matches_doubles.json")));
            using var client = TestSupport.ClientOver(handler);

            var match = (await client.ListMatchesAsync()).Data[0];
            Assert.True(match.IsDoubles);

            var team = match.Players!.P1!;
            Assert.True(team.IsDoublesTeam);
            Assert.Equal("ATP", team.Tour);            // UPPERCASE, opaque — not the filter enum
            var dc = team.DataCompleteness!;
            Assert.Null(dc.Known);                     // null, distinct from 0
            Assert.Null(dc.Of);
            Assert.False(string.IsNullOrEmpty(dc.Note));
        }

        // --- completed: null server inside a present score; winner mapping -----

        [Fact]
        public async Task Completed_ServerCanBeNull_InsidePresentScore()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("matches_completed.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListCompletedMatchesAsync();

            var finished = Assert.Single(page.Data, m => m.Winner == 1);
            Assert.NotNull(finished.Score);       // score object present
            Assert.Null(finished.Score!.Server);  // but server is null on a finished match
        }

        // --- single player: integer completeness + stats, lowercase tour -------

        [Fact]
        public async Task SinglePlayer_DataCompletenessIntegers_StatsPresent()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("player_single.json")));
            using var client = TestSupport.ClientOver(handler);

            var player = await client.GetPlayerAsync(2317);

            Assert.NotNull(player);
            Assert.Equal("atp", player!.Tour);        // lowercase for an individual
            Assert.Equal(3, player.DataCompleteness!.Known);
            Assert.Equal(5, player.DataCompleteness.Of);
            Assert.NotNull(player.Stats);
        }

        // --- error mapping -----------------------------------------------------

        [Fact]
        public async Task Unauthorized_401_Throws()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Unauthorized, TestSupport.Fixture("error_401_unauth.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => client.ListMatchesAsync());
            Assert.Equal(401, ex.StatusCode);
            Assert.Equal("unauthorized", ex.Code);
        }

        [Fact]
        public async Task BadTour_400_ThrowsBadRequest_WithCode()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.BadRequest, TestSupport.Fixture("error_400_bad_tour.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => client.ListMatchesAsync());
            Assert.Equal(400, ex.StatusCode);
            Assert.Equal("bad_tour", ex.Code);
        }

        [Fact]
        public async Task RateLimited_429_ParsesRetryAfter()
        {
            var options = new LiveTennisApiClientOptions { MaxRetries = 0 };
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.TooManyRequests, "{\"error\":\"rate_limited\"}", ("Retry-After", "1")));
            using var client = TestSupport.ClientOver(handler, options);

            var ex = await Assert.ThrowsAsync<RateLimitedException>(() => client.ListMatchesAsync());
            Assert.Equal(429, ex.StatusCode);
            Assert.Equal(1.0, ex.RetryAfterSeconds);
        }

        // --- retry policy: transient 429 then success --------------------------

        [Fact]
        public async Task Retry_429ThenSuccess_Recovers()
        {
            var options = new LiveTennisApiClientOptions { MaxRetries = 2 };
            var handler = new StubHttpMessageHandler(
                _ => TestSupport.Json(HttpStatusCode.TooManyRequests, "{\"error\":\"rate_limited\"}", ("Retry-After", "0")),
                _ => TestSupport.Ok(TestSupport.Fixture("matches_live.json")));
            using var client = TestSupport.ClientOver(handler, options);

            var page = await client.ListMatchesAsync();

            Assert.NotEmpty(page.Data);
            Assert.Equal(2, handler.CallCount); // one 429, one 200
        }

        [Fact]
        public async Task ConnectionFailure_ThrowsApiConnectionException()
        {
            var options = new LiveTennisApiClientOptions { MaxRetries = 0 };
            var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
            using var client = TestSupport.ClientOver(handler, options);

            await Assert.ThrowsAsync<ApiConnectionException>(() => client.ListMatchesAsync());
        }

        // --- auth header selection --------------------------------------------

        [Fact]
        public async Task BearerAuth_IsDefault()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("health.json")));
            using var client = TestSupport.ClientOver(handler);

            await client.HealthAsync();

            var headers = handler.Requests[0].Headers;
            Assert.Equal("Bearer " + TestSupport.TestKey, headers["Authorization"]);
            Assert.False(headers.ContainsKey("X-API-Key"));
        }

        [Fact]
        public async Task ApiKeyAuth_UsesXApiKeyHeader()
        {
            var options = new LiveTennisApiClientOptions { AuthHeader = AuthHeader.ApiKey };
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("health.json")));
            using var client = TestSupport.ClientOver(handler, options);

            await client.HealthAsync();

            var headers = handler.Requests[0].Headers;
            Assert.Equal(TestSupport.TestKey, headers["X-API-Key"]);
            Assert.False(headers.ContainsKey("Authorization"));
        }

        // --- query construction ------------------------------------------------

        [Fact]
        public async Task ListMatches_BuildsQuery_WithLowercaseTourFilter()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListMatchesAsync(MatchStatus.Upcoming, Tour.Wta, limit: 10, offset: 5);

            var query = handler.Requests[0].Query;
            Assert.Contains("status=upcoming", query);
            Assert.Contains("tour=wta", query);   // enum -> lowercase wire value
            Assert.Contains("limit=10", query);
            Assert.Contains("offset=5", query);
        }

        [Fact]
        public async Task ListMarkets_UsesMatchIdQueryParam()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListMarketsAsync(4242);

            Assert.Contains("match_id=4242", handler.Requests[0].Query);
            Assert.StartsWith("/api/public/v1/markets", handler.Requests[0].Uri.AbsolutePath);
        }

        [Fact]
        public async Task OmittedTour_IsNotSent()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"data\":[]}"));
            using var client = TestSupport.ClientOver(handler);

            await client.ListMatchesAsync(); // no tour

            Assert.DoesNotContain("tour=", handler.Requests[0].Query);
        }

        // --- forward compatibility: unknown fields survive ---------------------

        [Fact]
        public async Task UnknownField_IsCapturedInAdditionalProperties()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Ok("{\"status\":\"ok\",\"version\":\"v1\",\"brand_new_field\":123}"));
            using var client = TestSupport.ClientOver(handler);

            var health = await client.HealthAsync();

            Assert.NotNull(health);
            Assert.Equal("ok", health!.Status);
            Assert.NotNull(health.AdditionalProperties);
            Assert.True(health.AdditionalProperties!.ContainsKey("brand_new_field"));
        }

        // --- health ------------------------------------------------------------

        [Fact]
        public async Task Health_Deserializes()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("health.json")));
            using var client = TestSupport.ClientOver(handler);

            var health = await client.HealthAsync();

            Assert.Equal("ok", health!.Status);
            Assert.Equal("v1", health.Version);
        }

        // --- constructor guards ------------------------------------------------

        [Fact]
        public void Constructor_RequiresApiKey()
        {
            Assert.Throws<System.ArgumentException>(() => new LiveTennisApiClient("  "));
        }

        [Fact]
        public void Constructor_WithHttpClient_RejectsNullClient()
        {
            Assert.Throws<System.ArgumentNullException>(() => new LiveTennisApiClient(null!, "key"));
        }
    }
}
