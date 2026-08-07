using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LiveTennisApi.Models;
using Xunit;

namespace LiveTennisApi.Tests
{
    /// <summary>Coverage for the surface added in 1.2.0 (full API parity).</summary>
    public class ParityEndpointTests
    {
        // --- /tournaments -------------------------------------------------------

        [Fact]
        public async Task Tournaments_Deserialize_CategoryNullWhereUncurated()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("tournaments.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListTournamentsAsync(search: "wim", tour: Tour.Atp);

            var query = handler.Requests[0].Query;
            Assert.Contains("search=wim", query);
            Assert.Contains("tour=atp", query);

            var slam = page.Data[0];
            Assert.Equal("wimbledon-atp", slam.Id);           // the id Match.TournamentId joins
            Assert.Equal("grand_slam", slam.Category);
            Assert.Equal("GB", slam.Country);                 // ISO-3166 alpha-2, not the player vocab

            var itf = page.Data[1];
            Assert.Null(itf.Category);                        // uncurated — never guessed from the name
            Assert.Null(itf.City);
        }

        [Fact]
        public async Task Tournament_ById_EscapesTheStringId()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Ok("{\"id\":\"wimbledon-atp\",\"name\":\"Wimbledon\"}"));
            using var client = TestSupport.ClientOver(handler);

            var tournament = await client.GetTournamentAsync("wimbledon-atp");

            Assert.Equal("/api/public/v1/tournaments/wimbledon-atp", handler.Requests[0].Uri.AbsolutePath);
            Assert.Equal("Wimbledon", tournament!.Name);
        }

        // --- /usage -------------------------------------------------------------

        [Fact]
        public async Task Usage_Deserializes_WithGrantAndHistory()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("usage.json")));
            using var client = TestSupport.ClientOver(handler);

            var usage = await client.GetUsageAsync();

            Assert.Equal("pro", usage!.Tier);
            Assert.Equal("basic", usage.BaseTier);            // temporary grant active
            Assert.NotNull(usage.TierExpiresAt);
            Assert.Equal(300, usage.Limits!.PerMinute);
            Assert.Equal(10000, usage.Limits.PerDay);
            Assert.Equal(8769, usage.Today!.RemainingDay);
            Assert.Equal(2, usage.History!.Count);
            Assert.Equal("2026-08-05", usage.History[0].Day); // oldest first
        }

        // --- /matches/{id}/prices ----------------------------------------------

        [Fact]
        public async Task MatchPrices_Deserialize_WithSyntheticTag()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("match_prices.json")));
            using var client = TestSupport.ClientOver(handler);

            var prices = await client.ListMatchPricesAsync(22336, limit: 3, minutes: 30);

            var query = handler.Requests[0].Query;
            Assert.Contains("limit=3", query);
            Assert.Contains("minutes=30", query);
            Assert.StartsWith("/api/public/v1/matches/22336/prices", handler.Requests[0].Uri.AbsolutePath);

            Assert.False(prices.Data[0].Synthetic);           // real top-of-book
            Assert.True(prices.Data[1].Synthetic);            // estimated from mid — never mistake for a book
            Assert.Null(prices.Data[2].Synthetic);            // older tick, unknown
            Assert.Equal("prediction_market", prices.Data[0].PriceSource);

            Assert.Equal(22336, prices.Meta!.MatchId);
            Assert.True(prices.Meta.HasMore);                 // clipped — no offset here
            Assert.Equal(30, prices.Meta.Minutes);
        }

        [Fact]
        public async Task MatchPrices_403_SaysPro()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(() => client.ListMatchPricesAsync(1));
            Assert.Equal("PRO", ex.RequiredTier);
        }

        // --- /webhooks ----------------------------------------------------------

        [Fact]
        public async Task CreateWebhook_PostsJson_AndSurfacesTheOneTimeSecret()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Created, TestSupport.Fixture("webhook_created.json")));
            using var client = TestSupport.ClientOver(handler);

            var webhook = await client.CreateWebhookAsync(
                "https://example.com/hooks/tennis",
                new[] { WebhookEvent.Score, WebhookEvent.BreakPoint });

            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/public/v1/webhooks", request.Uri.AbsolutePath);
            Assert.Contains("\"url\":\"https://example.com/hooks/tennis\"", request.Body);
            Assert.Contains("\"break_point\"", request.Body); // enum -> wire value

            Assert.Equal(71, webhook!.Id);
            // The secret exists ONLY on this response — the list never carries it.
            Assert.Equal("whsec_a1b2c3d4e5f60718293a4b5c6d7e8f90", webhook.Secret);
            Assert.False(string.IsNullOrEmpty(webhook.SecretNote));
        }

        [Fact]
        public async Task CreateWebhook_OmittedEvents_AreNotSent()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Created, TestSupport.Fixture("webhook_created.json")));
            using var client = TestSupport.ClientOver(handler);

            await client.CreateWebhookAsync("https://example.com/hooks/tennis");

            // Server default (score) applies; the client does not second-guess it.
            Assert.DoesNotContain("events", handler.Requests[0].Body);
        }

        [Fact]
        public async Task ListWebhooks_NeverContainsASecret()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok(TestSupport.Fixture("webhooks_list.json")));
            using var client = TestSupport.ClientOver(handler);

            var page = await client.ListWebhooksAsync();

            Assert.Equal(2, page.Data.Count);
            Assert.All(page.Data, w => Assert.Null(w.Secret));
            Assert.Equal(12, page.Data[1].ConsecutiveFailures);
            Assert.Equal("connect timeout", page.Data[1].LastError);
        }

        [Fact]
        public async Task DeleteWebhook_UsesDelete_AndReturnsReceipt()
        {
            var handler = new StubHttpMessageHandler(_ => TestSupport.Ok("{\"deleted\":71}"));
            using var client = TestSupport.ClientOver(handler);

            var receipt = await client.DeleteWebhookAsync(71);

            Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
            Assert.Equal("/api/public/v1/webhooks/71", handler.Requests[0].Uri.AbsolutePath);
            Assert.Equal(71, receipt!.Deleted);
        }

        [Fact]
        public async Task FourthWebhook_Is409Conflict_WithLimitCode()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Conflict, TestSupport.Fixture("error_409_webhook_limit.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<ConflictException>(
                () => client.CreateWebhookAsync("https://example.com/hooks/fourth"));
            Assert.Equal(409, ex.StatusCode);
            Assert.Equal("webhook_limit", ex.Code);           // delete one first
        }

        [Fact]
        public async Task Webhooks_403_SaysUltra()
        {
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.Forbidden, TestSupport.Fixture("error_403_upgrade.json")));
            using var client = TestSupport.ClientOver(handler);

            var ex = await Assert.ThrowsAsync<UpgradeRequiredException>(() => client.ListWebhooksAsync());
            Assert.Equal("ULTRA", ex.RequiredTier);
        }

        // --- POST retry policy ---------------------------------------------------

        [Fact]
        public async Task CreateWebhook_IsNeverRetried_EvenOn5xx()
        {
            // A retried registration could register twice when the first attempt
            // succeeded server-side after the response was lost.
            var options = new LiveTennisApiClientOptions { MaxRetries = 2 };
            var handler = new StubHttpMessageHandler(_ =>
                TestSupport.Json(HttpStatusCode.InternalServerError, "{\"error\":\"server_error\"}"));
            using var client = TestSupport.ClientOver(handler, options);

            await Assert.ThrowsAsync<ServerException>(
                () => client.CreateWebhookAsync("https://example.com/hooks/tennis"));
            Assert.Equal(1, handler.CallCount);               // exactly one attempt
        }

        [Fact]
        public async Task DeleteWebhook_IsRetried_ItIsIdempotent()
        {
            var options = new LiveTennisApiClientOptions { MaxRetries = 2 };
            var handler = new StubHttpMessageHandler(
                _ => TestSupport.Json(HttpStatusCode.InternalServerError, "{\"error\":\"server_error\"}"),
                _ => TestSupport.Ok("{\"deleted\":71}"));
            using var client = TestSupport.ClientOver(handler, options);

            var receipt = await client.DeleteWebhookAsync(71);

            Assert.Equal(71, receipt!.Deleted);
            Assert.Equal(2, handler.CallCount);               // one 500, one 200
        }
    }
}
