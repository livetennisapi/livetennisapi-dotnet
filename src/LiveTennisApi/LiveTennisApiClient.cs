using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LiveTennisApi.Internal;
using LiveTennisApi.Models;

namespace LiveTennisApi
{
    /// <summary>
    /// A client for the Live Tennis API — real-time scores, players, fixtures and
    /// (on paid tiers) events, market prices and model analysis.
    /// </summary>
    /// <remarks>
    /// <para>Create one directly with your key:</para>
    /// <code>
    /// using var client = new LiveTennisApiClient("twjp_...");
    /// var live = await client.ListMatchesAsync(MatchStatus.Live);
    /// </code>
    /// <para>
    /// Or hand it an <see cref="HttpClient"/> from
    /// <c>IHttpClientFactory</c>, in which case the client will <b>not</b> dispose
    /// it:
    /// </para>
    /// <code>
    /// var client = new LiveTennisApiClient(httpClientFromFactory, "twjp_...");
    /// </code>
    /// <para>
    /// The type is safe to reuse and to call concurrently. Only the FREE surface
    /// (matches, scores, players, fixtures) works on a free key; higher endpoints
    /// throw <see cref="UpgradeRequiredException"/>.
    /// </para>
    /// </remarks>
    public sealed class LiveTennisApiClient : IDisposable
    {
        private const int MaxLimit = 200;

        private static readonly IReadOnlyList<(string Marker, string Tier)> TierRequirements =
            new (string, string)[]
            {
                ("/analysis", "ULTRA"),
                ("/events", "PRO"),
                ("/markets", "PRO"),
                ("/history", "BASIC"),
            };

        private static readonly string ClientVersion =
            typeof(LiveTennisApiClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(LiveTennisApiClient).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        private readonly HttpClient _http;
        private readonly bool _ownsHttpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly AuthHeader _authHeader;
        private readonly int _maxRetries;
        private readonly string _userAgent;
        private bool _disposed;

        /// <summary>
        /// Creates a client that owns and configures its own <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="apiKey">Your <c>twjp_</c> API key.</param>
        /// <param name="options">Optional configuration; defaults are used when <c>null</c>.</param>
        /// <exception cref="ArgumentException"><paramref name="apiKey"/> is null or blank.</exception>
        public LiveTennisApiClient(string apiKey, LiveTennisApiClientOptions? options = null)
            : this(CreateOwnedHttpClient(options), apiKey, options, ownsHttpClient: true)
        {
        }

        /// <summary>
        /// Creates a client over a caller-supplied <see cref="HttpClient"/> — the
        /// <c>IHttpClientFactory</c>-friendly path. The supplied client is
        /// <b>not</b> disposed by this instance, and its own timeout applies.
        /// </summary>
        /// <param name="httpClient">The HTTP client to send requests with.</param>
        /// <param name="apiKey">Your <c>twjp_</c> API key.</param>
        /// <param name="options">Optional configuration; defaults are used when <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="apiKey"/> is null or blank.</exception>
        public LiveTennisApiClient(HttpClient httpClient, string apiKey, LiveTennisApiClientOptions? options = null)
            : this(httpClient ?? throw new ArgumentNullException(nameof(httpClient)), apiKey, options, ownsHttpClient: false)
        {
        }

        private LiveTennisApiClient(HttpClient httpClient, string apiKey, LiveTennisApiClientOptions? options, bool ownsHttpClient)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("An API key is required.", nameof(apiKey));
            }

            options ??= new LiveTennisApiClientOptions();
            _http = httpClient;
            _ownsHttpClient = ownsHttpClient;
            _apiKey = apiKey.Trim();
            _baseUrl = (options.BaseUrl ?? LiveTennisApiClientOptions.DefaultBaseUrl).TrimEnd('/');
            _authHeader = options.AuthHeader;
            _maxRetries = Math.Max(0, options.MaxRetries);
            _userAgent = string.IsNullOrWhiteSpace(options.UserAgent)
                ? "livetennisapi-dotnet/" + ClientVersion
                : options.UserAgent!;
        }

        private static HttpClient CreateOwnedHttpClient(LiveTennisApiClientOptions? options)
        {
            var client = new HttpClient();
            var timeout = options?.Timeout ?? TimeSpan.FromSeconds(30);
            if (timeout > TimeSpan.Zero)
            {
                client.Timeout = timeout;
            }

            return client;
        }

        // -- endpoints ----------------------------------------------------------

        /// <summary>Liveness probe. Needs no authentication.</summary>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The service health.</returns>
        public Task<HealthStatus?> HealthAsync(CancellationToken cancellationToken = default) =>
            GetAsync<HealthStatus>("/health", null, cancellationToken);

        /// <summary>Lists matches by lifecycle status, optionally restricted to one tour.</summary>
        /// <param name="status">Lifecycle status. Defaults to <see cref="MatchStatus.Live"/>.</param>
        /// <param name="tour">Optional tour filter. Omit for all tours.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of matches, each with its latest score.</returns>
        public Task<Page<Match>> ListMatchesAsync(
            MatchStatus status = MatchStatus.Live,
            Tour? tour = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<Match>(
                "/matches",
                new[]
                {
                    Param("status", status.ToQueryValue()),
                    Param("tour", tour?.ToQueryValue()),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        /// <summary>Full match detail. Embeds <c>market</c> at PRO and <c>analysis</c> at ULTRA.</summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The match, or <c>null</c> if the body was empty.</returns>
        public Task<Match?> GetMatchAsync(int matchId, CancellationToken cancellationToken = default) =>
            GetAsync<Match>("/matches/" + matchId, null, cancellationToken);

        /// <summary>Current score only — the lowest-latency read available.</summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The current score, or <c>null</c> if the body was empty.</returns>
        public Task<Score?> GetMatchScoreAsync(int matchId, CancellationToken cancellationToken = default) =>
            GetAsync<Score>("/matches/" + matchId + "/score", null, cancellationToken);

        /// <summary>Match events, newest first. <b>PRO.</b></summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of events.</returns>
        public Task<Page<MatchEvent>> ListMatchEventsAsync(
            int matchId,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<MatchEvent>(
                "/matches/" + matchId + "/events",
                new[] { Param("limit", limit), Param("offset", offset) },
                cancellationToken);

        /// <summary>Model analysis for a match. <b>ULTRA.</b></summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The analysis, or <c>null</c> if the body was empty.</returns>
        public Task<Analysis?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default) =>
            GetAsync<Analysis>("/matches/" + matchId + "/analysis", null, cancellationToken);

        /// <summary>Searches players by name. Ranked players come first.</summary>
        /// <param name="search">Optional name query. Omit to list ranked players.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of players (no <c>stats</c> object on the list).</returns>
        public Task<Page<Player>> SearchPlayersAsync(
            string? search = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<Player>(
                "/players",
                new[]
                {
                    Param("search", search),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        /// <summary>One player's bio, ranking and cached stats.</summary>
        /// <param name="playerId">The player id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The player, or <c>null</c> if the body was empty.</returns>
        public Task<Player?> GetPlayerAsync(int playerId, CancellationToken cancellationToken = default) =>
            GetAsync<Player>("/players/" + playerId, null, cancellationToken);

        /// <summary>Match-winner market(s) for a match. <b>PRO.</b></summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of markets.</returns>
        public Task<Page<Market>> ListMarketsAsync(int matchId, CancellationToken cancellationToken = default) =>
            GetPageAsync<Market>(
                "/markets",
                new[] { Param("match_id", matchId) },
                cancellationToken);

        /// <summary>Market with recent price ticks per side, newest first. <b>PRO.</b></summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="limit">Number of price ticks, 1–200. Defaults to 50.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The market with its <c>prices</c>, or <c>null</c> if the body was empty.</returns>
        public Task<Market?> GetMarketPricesAsync(int matchId, int limit = 50, CancellationToken cancellationToken = default) =>
            GetAsync<Market>(
                "/markets/" + matchId + "/prices",
                new[] { Param("limit", limit) },
                cancellationToken);

        /// <summary>Completed matches, newest first, with a derived <c>winner</c>. <b>BASIC.</b></summary>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of completed matches.</returns>
        public Task<Page<Match>> ListCompletedMatchesAsync(
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<Match>(
                "/history/matches",
                new[] { Param("limit", limit), Param("offset", offset) },
                cancellationToken);

        /// <summary>Upcoming scheduled fixtures, earliest first.</summary>
        /// <remarks>Note: this endpoint currently also returns some finished matches; they are passed through unfiltered.</remarks>
        /// <param name="tour">Optional tour filter. Omit for all tours.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of fixtures (names only — players not yet resolved to ids).</returns>
        public Task<Page<Fixture>> ListFixturesAsync(
            Tour? tour = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<Fixture>(
                "/fixtures",
                new[]
                {
                    Param("tour", tour?.ToQueryValue()),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        // -- transport ----------------------------------------------------------

        private static KeyValuePair<string, string?> Param(string key, string? value) =>
            new KeyValuePair<string, string?>(key, value);

        private static KeyValuePair<string, string?> Param(string key, int value) =>
            new KeyValuePair<string, string?>(key, value.ToString(CultureInfo.InvariantCulture));

        private async Task<Page<T>> GetPageAsync<T>(
            string path,
            IEnumerable<KeyValuePair<string, string?>> query,
            CancellationToken cancellationToken)
        {
            var page = await GetAsync<Page<T>>(path, query, cancellationToken).ConfigureAwait(false);
            return page ?? new Page<T>();
        }

        private async Task<T?> GetAsync<T>(
            string path,
            IEnumerable<KeyValuePair<string, string?>>? query,
            CancellationToken cancellationToken)
            where T : class
        {
            ThrowIfDisposed();
            var url = BuildUrl(path, query);

            for (var attempt = 0; ; attempt++)
            {
                HttpResponseMessage response;
                try
                {
                    using var request = BuildRequest(url);
                    response = await _http
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException ex)
                {
                    // Cancellation not requested by the caller ⇒ the HttpClient timeout fired.
                    if (attempt >= _maxRetries)
                    {
                        throw new ApiTimeoutException("Request to " + url + " timed out.", url, ex);
                    }

                    await DelayAsync(Backoff(attempt, null), cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    if (attempt >= _maxRetries)
                    {
                        throw new ApiConnectionException("Could not reach " + url + ": " + ex.Message, url, ex);
                    }

                    await DelayAsync(Backoff(attempt, null), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                using (response)
                {
                    var status = (int)response.StatusCode;

                    if (ShouldRetry(status) && attempt < _maxRetries)
                    {
                        await DelayAsync(Backoff(attempt, RetryAfterSeconds(response)), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw await BuildExceptionAsync(response, path, url).ConfigureAwait(false);
                    }

                    return await DeserializeAsync<T>(response, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private HttpRequestMessage BuildRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            if (_authHeader == AuthHeader.Bearer)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("X-API-Key", _apiKey);
            }

            return request;
        }

        private string BuildUrl(string path, IEnumerable<KeyValuePair<string, string?>>? query)
        {
            var builder = new StringBuilder(_baseUrl);
            builder.Append(path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path);

            if (query != null)
            {
                var first = true;
                foreach (var pair in query)
                {
                    if (pair.Value is null)
                    {
                        continue;
                    }

                    builder.Append(first ? '?' : '&');
                    first = false;
                    builder.Append(Uri.EscapeDataString(pair.Key));
                    builder.Append('=');
                    builder.Append(Uri.EscapeDataString(pair.Value));
                }
            }

            return builder.ToString();
        }

        private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
            where T : class
        {
#if NET8_0_OR_GREATER
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            try
            {
                return await JsonSerializer
                    .DeserializeAsync<T>(stream, LiveTennisJson.Options, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new LiveTennisApiException(
                    "Failed to deserialize the response body: " + ex.Message,
                    (int)response.StatusCode,
                    innerException: ex);
            }
        }

        private async Task<LiveTennisApiException> BuildExceptionAsync(HttpResponseMessage response, string path, string url)
        {
            string? body = null;
            try
            {
                body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                // The body is best-effort context; a failure to read it must not
                // mask the status error.
            }

            var code = ExtractErrorCode(body);
            var headers = CollectHeaders(response);
            var reason = string.IsNullOrEmpty(response.ReasonPhrase) ? "request failed" : response.ReasonPhrase!;
            var message = code ?? reason;
            var requiredTier = RequiredTierFor(path);
            var retryAfter = RetryAfterSeconds(response);

            return ExceptionFactory.ForStatus(response.StatusCode, message, code, url, body, headers, requiredTier, retryAfter);
        }

        private static string? ExtractErrorCode(string? body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(body!);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("error", out var error) &&
                    error.ValueKind == JsonValueKind.String)
                {
                    var value = error.GetString();
                    return string.IsNullOrEmpty(value) ? null : value;
                }
            }
            catch (JsonException)
            {
                // Non-JSON body — no structured code to extract.
            }

            return null;
        }

        private static IReadOnlyDictionary<string, string> CollectHeaders(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }

            return headers;
        }

        private static string? RequiredTierFor(string path)
        {
            foreach (var (marker, tier) in TierRequirements)
            {
                if (path.IndexOf(marker, StringComparison.Ordinal) >= 0)
                {
                    return tier;
                }
            }

            return null;
        }

        private static double? RetryAfterSeconds(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter is null)
            {
                return null;
            }

            if (retryAfter.Delta.HasValue)
            {
                return retryAfter.Delta.Value.TotalSeconds;
            }

            if (retryAfter.Date.HasValue)
            {
                var seconds = (retryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
                return seconds > 0 ? seconds : 0;
            }

            return null;
        }

        /// <summary>Retry only what retrying can fix: <c>429</c> and <c>5xx</c>.</summary>
        private static bool ShouldRetry(int status) => status == 429 || status >= 500;

        private static TimeSpan Backoff(int attempt, double? retryAfterSeconds)
        {
            if (retryAfterSeconds.HasValue)
            {
                return TimeSpan.FromSeconds(Math.Min(retryAfterSeconds.Value, 60));
            }

            // Exponential with full jitter, capped, so concurrent clients don't
            // retry in lockstep.
            var baseMs = Math.Min(500d * Math.Pow(2, attempt), 10_000);
            var jitter = RandomShared.NextDouble() * 250;
            return TimeSpan.FromMilliseconds(Math.Min(baseMs + jitter, 10_000));
        }

        private static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            delay <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(delay, cancellationToken);

#if NET8_0_OR_GREATER
        private static Random RandomShared => Random.Shared;
#else
        [ThreadStatic]
        private static Random? _random;

        private static Random RandomShared => _random ??= new Random();
#endif

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LiveTennisApiClient));
            }
        }

        /// <summary>
        /// Disposes the underlying <see cref="HttpClient"/> when this client owns
        /// it. When an <see cref="HttpClient"/> was supplied by the caller, it is
        /// left untouched.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_ownsHttpClient)
            {
                _http.Dispose();
            }
        }
    }
}
