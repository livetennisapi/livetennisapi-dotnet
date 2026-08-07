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
    /// A client for the Live Tennis API — real-time scores, players, fixtures
    /// and (on paid tiers) history and tapes, head-to-head, the 1968–2022
    /// results archive, rankings, in-play statistics, rally/charting data,
    /// events, market prices and model analysis.
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
        private const int MaxPlayerFilters = 50;

        // Order matters: the first matching marker wins, so the more specific
        // paths (e.g. /rally, /history/packages) sit above the general /history.
        private static readonly IReadOnlyList<(string Marker, string Tier)> TierRequirements =
            new (string, string)[]
            {
                ("/analysis", "ULTRA"),
                ("/statistics", "ULTRA"),
                ("/rally", "ULTRA"),
                ("/charting", "ULTRA"),
                ("/ws-token", "ULTRA"),
                ("/webhooks", "ULTRA"),
                ("/events", "PRO"),
                ("/markets", "PRO"),
                ("/prices", "PRO"),
                ("/history/packages", "PRO"),
                ("/history", "BASIC"),
                ("/h2h", "BASIC"),
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

        /// <summary>Lists matches by lifecycle status, with optional filters.</summary>
        /// <param name="status">Lifecycle status. Defaults to <see cref="MatchStatus.Live"/>.</param>
        /// <param name="tour">Optional tour filter. Omit for all tours.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="players">
        /// Optional player ids, max 50 — matches where any of them is either
        /// participant (deduplicated union). An unknown id returns an empty
        /// list, not an error.
        /// </param>
        /// <param name="from">Earliest play date: <c>YYYY-MM-DD</c> or ISO 8601 UTC datetime. A bare date is a UTC day boundary.</param>
        /// <param name="to">Latest play date (a bare date includes the whole UTC day); must not precede <paramref name="from"/>.</param>
        /// <param name="country">
        /// Lowercase 3-letter country code (IOC-style, e.g. <c>ned</c>,
        /// <c>sui</c> — the vocabulary <see cref="Player.Country"/> returns, not
        /// ISO-3166) — matches where either participant has that country.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of matches, each with its latest score.</returns>
        /// <exception cref="ArgumentException">More than 50 player ids were supplied.</exception>
        public Task<Page<Match>> ListMatchesAsync(
            MatchStatus status = MatchStatus.Live,
            Tour? tour = null,
            int limit = 50,
            int offset = 0,
            IEnumerable<int>? players = null,
            string? from = null,
            string? to = null,
            string? country = null,
            CancellationToken cancellationToken = default)
        {
            var query = new List<KeyValuePair<string, string?>>
            {
                Param("status", status.ToQueryValue()),
                Param("tour", tour?.ToQueryValue()),
            };
            AddPlayerParams(query, players);
            query.Add(Param("from", from));
            query.Add(Param("to", to));
            query.Add(Param("country", country));
            query.Add(Param("limit", limit));
            query.Add(Param("offset", offset));
            return GetPageAsync<Match>("/matches", query, cancellationToken);
        }

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

        /// <summary>
        /// Completed matches, newest first, with a derived <c>winner</c> and each
        /// match's tape coverage (<see cref="Match.Tape"/>). <b>BASIC, or any
        /// History plan.</b>
        /// </summary>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="tour">Optional tour filter. Omit for all tours.</param>
        /// <param name="players">Optional player ids, max 50 — matches where any of them is either participant.</param>
        /// <param name="from">Earliest play date: <c>YYYY-MM-DD</c> or ISO 8601 UTC datetime.</param>
        /// <param name="to">Latest play date (a bare date includes the whole UTC day).</param>
        /// <param name="country">Lowercase 3-letter country code (IOC-style) — either participant.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of completed matches.</returns>
        /// <exception cref="ArgumentException">More than 50 player ids were supplied.</exception>
        public Task<Page<Match>> ListCompletedMatchesAsync(
            int limit = 50,
            int offset = 0,
            Tour? tour = null,
            IEnumerable<int>? players = null,
            string? from = null,
            string? to = null,
            string? country = null,
            CancellationToken cancellationToken = default)
        {
            var query = new List<KeyValuePair<string, string?>>
            {
                Param("tour", tour?.ToQueryValue()),
            };
            AddPlayerParams(query, players);
            query.Add(Param("from", from));
            query.Add(Param("to", to));
            query.Add(Param("country", country));
            query.Add(Param("limit", limit));
            query.Add(Param("offset", offset));
            return GetPageAsync<Match>("/history/matches", query, cancellationToken);
        }

        /// <summary>
        /// The per-match tape: the point-by-point score sequence + per-point
        /// model probabilities. <b>BASIC, or any History plan.</b> Works on a
        /// <b>live</b> match, not only a completed one.
        /// </summary>
        /// <param name="matchId">The match id (the same id space as <c>/matches</c>).</param>
        /// <param name="sequence">
        /// <see cref="TapeSequence.Raw"/> (default) is every committed row —
        /// deliberately non-monotonic. <see cref="TapeSequence.Clean"/> is one
        /// row per distinct score state, and is the only mode whose rows carry
        /// <see cref="HistoryTapeRow.PointWinner"/>.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>Match header + chronological tape + model profiles + coverage meta, or <c>null</c> if the body was empty.</returns>
        public Task<HistoryTape?> GetMatchTapeAsync(
            int matchId,
            TapeSequence sequence = TapeSequence.Raw,
            CancellationToken cancellationToken = default) =>
            GetAsync<HistoryTape>(
                "/history/matches/" + matchId,
                new[] { Param("sequence", sequence.ToQueryValue()) },
                cancellationToken);

        /// <summary>
        /// The head-to-head record between two players across the results
        /// archive (1968–2022) and our own completed matches (2023→).
        /// <b>BASIC, or any History plan.</b>
        /// </summary>
        /// <param name="p1">First player name (fragment, min 3 chars).</param>
        /// <param name="p2">Second player name (fragment, min 3 chars).</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The record; empty totals when no player matches the names.</returns>
        /// <remarks>
        /// A fragment matching more than one player is refused with a <c>400</c>
        /// <c>ambiguous_name</c> (<see cref="BadRequestException"/>) listing the
        /// candidates in the body.
        /// </remarks>
        public Task<HeadToHead?> GetHeadToHeadAsync(string p1, string p2, CancellationToken cancellationToken = default) =>
            GetAsync<HeadToHead>(
                "/h2h",
                new[] { Param("p1", p1), Param("p2", p2) },
                cancellationToken);

        /// <summary>
        /// Deep historical results, 1968–2022: winner/loser-shaped records with
        /// final score, round, seeds, and the players' ranks at the time.
        /// <b>BASIC, or any History plan.</b> A separate id space from
        /// <c>/matches</c>.
        /// </summary>
        /// <param name="tour">Optional archive tour filter (ATP/WTA only).</param>
        /// <param name="name">Case-insensitive substring match on either player's name (min 3 chars).</param>
        /// <param name="from">Earliest tournament start date (<c>YYYY-MM-DD</c>).</param>
        /// <param name="to">Latest tournament start date (<c>YYYY-MM-DD</c>).</param>
        /// <param name="round">Round code: <c>F</c>, <c>SF</c>, <c>QF</c>, <c>R16</c>, <c>R32</c>, <c>R64</c>, <c>R128</c>, <c>RR</c>, <c>BR</c>, <c>Q1</c>–<c>Q4</c>, <c>ER</c>.</param>
        /// <param name="level">Source tier code: <c>G</c>, <c>M</c>, <c>A</c>, <c>F</c>, <c>D</c>, <c>C</c>, <c>O</c>, or a futures category code.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of archive results, newest tournament first.</returns>
        public Task<Page<ArchiveMatch>> ListArchiveMatchesAsync(
            ArchiveTour? tour = null,
            string? name = null,
            string? from = null,
            string? to = null,
            string? round = null,
            string? level = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<ArchiveMatch>(
                "/history/archive/matches",
                new[]
                {
                    Param("tour", tour?.ToQueryValue()),
                    Param("name", name),
                    Param("from", from),
                    Param("to", to),
                    Param("round", round),
                    Param("level", level),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        /// <summary>
        /// One archive result, with serve statistics where the era recorded them
        /// (<c>stats</c> is <c>null</c> for most pre-1991 rows — never
        /// synthesised). <b>BASIC, or any History plan.</b>
        /// </summary>
        /// <param name="archiveId">The archive record id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The archive record, or <c>null</c> if the body was empty.</returns>
        public Task<ArchiveMatch?> GetArchiveMatchAsync(int archiveId, CancellationToken cancellationToken = default) =>
            GetAsync<ArchiveMatch>("/history/archive/matches/" + archiveId, null, cancellationToken);

        /// <summary>
        /// Archive player bios — hand, date of birth, country, height,
        /// career-high. <b>BASIC, or any History plan.</b> Own id space; never a
        /// roster id.
        /// </summary>
        /// <param name="name">Case-insensitive substring filter (min 3 chars).</param>
        /// <param name="tour">Optional archive tour filter (ATP/WTA only).</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of archive people, ordered by name.</returns>
        public Task<Page<ArchivePlayerBio>> ListArchivePlayersAsync(
            string? name = null,
            ArchiveTour? tour = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<ArchivePlayerBio>(
                "/history/archive/players",
                new[]
                {
                    Param("name", name),
                    Param("tour", tour?.ToQueryValue()),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        /// <summary>
        /// One player's whole archive career (1968–2022): W-L record, titles and
        /// the summed serve-stat block — sums and ratios of sums only, nothing
        /// modelled. <b>BASIC, or any History plan.</b>
        /// </summary>
        /// <param name="name">Player name (fragment, min 3 chars — must resolve to one person).</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The career aggregate, or <c>null</c> if the body was empty.</returns>
        /// <remarks>Ambiguous fragments are refused with a <c>400</c> <c>ambiguous_name</c> listing candidates.</remarks>
        public Task<ArchiveCareer?> GetArchiveCareerAsync(string name, CancellationToken cancellationToken = default) =>
            GetAsync<ArchiveCareer>(
                "/history/archive/career",
                new[] { Param("name", name) },
                cancellationToken);

        /// <summary>
        /// In-play statistics for one match — aces, double faults, serve split,
        /// hold/break %, break points, service and return points, in two
        /// deliberately unmerged families (derived vs measured). <b>ULTRA.</b>
        /// </summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// The statistics with their own coverage and <c>as_of</c>, or
        /// <c>null</c> if the body was empty. When nothing is held the endpoint
        /// returns <c>200</c> with null <see cref="MatchStatistics.Players"/>,
        /// not <c>404</c>.
        /// </returns>
        public Task<MatchStatistics?> GetMatchStatisticsAsync(int matchId, CancellationToken cancellationToken = default) =>
            GetAsync<MatchStatistics>("/matches/" + matchId + "/statistics", null, cancellationToken);

        /// <summary>
        /// The full published ranking table in rank order for one system — the
        /// newest week at or before <paramref name="asOf"/>. <b>PRO.</b>
        /// </summary>
        /// <param name="system">
        /// The system to list. <see cref="RankingSystem.Utr"/> has no listing (a
        /// rating, not a ranking).
        /// </param>
        /// <param name="asOf">Optional as-of date (<c>YYYY-MM-DD</c>). Omit for the latest week.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The table in rank order; rows carry <c>player_name</c> as published and a null <c>player_id</c> for players outside the roster.</returns>
        public async Task<RankingsResult> ListRankingsAsync(
            RankingSystem system,
            string? asOf = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<RankingsResult>(
                "/rankings",
                new[]
                {
                    Param("system", system.ToQueryValue()),
                    Param("as_of", asOf),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken,
                requiredTier: "PRO").ConfigureAwait(false);
            return result ?? new RankingsResult();
        }

        /// <summary>
        /// Per-player point-in-time ranking records: per system, the newest
        /// record effective on or before <paramref name="asOf"/> — never one
        /// dated after it. <b>ULTRA.</b>
        /// </summary>
        /// <param name="playerIds">Player ids, 1–50.</param>
        /// <param name="systems">Optional systems to restrict to. Omit for all.</param>
        /// <param name="asOf">Optional as-of date (<c>YYYY-MM-DD</c>). Omit for the latest known record.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>Ranking records in force at <paramref name="asOf"/>, with coverage meta.</returns>
        /// <exception cref="ArgumentException">No player ids, or more than 50, were supplied.</exception>
        /// <remarks>
        /// Every other ranking field in this API is the player's <b>current</b>
        /// value joined at read time; this endpoint is the point-in-time answer.
        /// Check <c>Meta.Coverage.OldestAvailable</c> before trusting an empty
        /// result — ITF and UTR history begins 2026-07-29.
        /// </remarks>
        public async Task<RankingsResult> GetPlayerRankingsAsync(
            IEnumerable<int> playerIds,
            IEnumerable<RankingSystem>? systems = null,
            string? asOf = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            if (playerIds is null)
            {
                throw new ArgumentNullException(nameof(playerIds));
            }

            var query = new List<KeyValuePair<string, string?>>();
            var count = 0;
            foreach (var id in playerIds)
            {
                count++;
                if (count > MaxPlayerFilters)
                {
                    throw new ArgumentException("At most " + MaxPlayerFilters + " player ids are accepted.", nameof(playerIds));
                }

                query.Add(Param("player", id));
            }

            if (count == 0)
            {
                throw new ArgumentException("At least one player id is required — for the rank-ordered listing use ListRankingsAsync.", nameof(playerIds));
            }

            if (systems != null)
            {
                foreach (var system in systems)
                {
                    query.Add(Param("system", system.ToQueryValue()));
                }
            }

            query.Add(Param("as_of", asOf));
            query.Add(Param("limit", limit));
            query.Add(Param("offset", offset));

            var result = await GetAsync<RankingsResult>("/rankings", query, cancellationToken, requiredTier: "ULTRA")
                .ConfigureAwait(false);
            return result ?? new RankingsResult();
        }

        /// <summary>
        /// Charted matches with shot-by-shot data, newest first. <b>ULTRA.</b>
        /// Rally construction is the layer below the tape: the tape says what
        /// the score became after each point, this says how the point was
        /// played. Own id space — most charted matches predate our collection.
        /// </summary>
        /// <param name="player">Optional substring match on either player name.</param>
        /// <param name="from">Earliest match date (<c>YYYY-MM-DD</c>).</param>
        /// <param name="to">Latest match date (<c>YYYY-MM-DD</c>).</param>
        /// <param name="surface">Optional surface filter.</param>
        /// <param name="gender">Optional gender filter.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of charted matches — the authoritative coverage list.</returns>
        public Task<Page<RallyMatch>> ListRallyMatchesAsync(
            string? player = null,
            string? from = null,
            string? to = null,
            string? surface = null,
            Gender? gender = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<RallyMatch>(
                "/rally/matches",
                new[]
                {
                    Param("player", player),
                    Param("from", from),
                    Param("to", to),
                    Param("surface", surface),
                    Param("gender", gender?.ToRallyQueryValue()),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        /// <summary>
        /// Rally construction for one charted match — its points in play order.
        /// <b>ULTRA.</b>
        /// </summary>
        /// <param name="rallyMatchId">The rally match id (this product's own id space).</param>
        /// <param name="limit">Points per page, 1–200. Defaults to 50.</param>
        /// <param name="offset">Point offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The charted match with its points; <c>Meta.Total</c> is the full point count.</returns>
        public Task<RallyMatchDetail?> GetRallyMatchAsync(
            int rallyMatchId,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetAsync<RallyMatchDetail>(
                "/rally/matches/" + rallyMatchId,
                new[] { Param("limit", limit), Param("offset", offset) },
                cancellationToken);

        /// <summary>
        /// Rally construction addressed by <b>our</b> match id, resolved through
        /// the optional link. <b>ULTRA.</b>
        /// </summary>
        /// <param name="matchId">Our match id (the same id space as <c>/matches</c>).</param>
        /// <param name="limit">Points per page, 1–200. Defaults to 50.</param>
        /// <param name="offset">Point offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The charted match with its points.</returns>
        /// <remarks>
        /// Answers <c>404</c> with code <c>not_charted</c> when we hold the match
        /// but nobody charted it — deliberately distinct from "no such match"
        /// (<c>not_found</c>), because most matches are not charted. Check
        /// <see cref="LiveTennisApiException.Code"/> on the
        /// <see cref="NotFoundException"/> to tell them apart.
        /// </remarks>
        public Task<RallyMatchDetail?> GetMatchRallyAsync(
            int matchId,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetAsync<RallyMatchDetail>(
                "/history/matches/" + matchId + "/rally",
                new[] { Param("limit", limit), Param("offset", offset) },
                cancellationToken);

        /// <summary>
        /// Career shot-level charting aggregate for one player, from the Match
        /// Charting Project. <b>ULTRA.</b> Coverage is curated (11,646 charted
        /// matches, concentrated on the majors), not full-slate.
        /// </summary>
        /// <param name="name">Player name (min 3 chars — must resolve to one charted person).</param>
        /// <param name="gender">Optional disambiguator when the fragment matches players of both tours.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The aggregate, or <c>null</c> if the body was empty.</returns>
        public Task<ChartingPlayerAggregate?> GetChartingPlayerAsync(
            string name,
            Gender? gender = null,
            CancellationToken cancellationToken = default) =>
            GetAsync<ChartingPlayerAggregate>(
                "/charting/players",
                new[]
                {
                    Param("name", name),
                    Param("gender", gender?.ToChartingQueryValue()),
                },
                cancellationToken);

        /// <summary>
        /// One charted match — every Match Charting Project stat family for both
        /// players, with the per-set split exactly as charted. <b>ULTRA.</b>
        /// </summary>
        /// <param name="chartingMatchId">The charting match id (own id space, 1960–2026).</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The charted match, or <c>null</c> if the body was empty.</returns>
        public Task<ChartingMatch?> GetChartingMatchAsync(int chartingMatchId, CancellationToken cancellationToken = default) =>
            GetAsync<ChartingMatch>("/charting/matches/" + chartingMatchId, null, cancellationToken);

        /// <summary>
        /// Mints a short-lived connection token for the high-fan-out push
        /// WebSocket feed. <b>ULTRA.</b> Mint a fresh token on reconnect.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// The token with the push URL and channel vocabulary
        /// (<c>match:{match_id}</c> per-match streams, <c>slate:all</c> for
        /// every live score frame), or <c>null</c> if the body was empty.
        /// </returns>
        public Task<WsToken?> GetWsTokenAsync(CancellationToken cancellationToken = default) =>
            GetAsync<WsToken>("/ws-token", null, cancellationToken);

        /// <summary>
        /// Lists pre-built monthly bulk packages, newest period first.
        /// <b>PRO, or a package subscription</b>
        /// (<see cref="HistoryPackageKind.Rankings"/> and year listings need
        /// ULTRA / History Business / a 1-year package).
        /// </summary>
        /// <param name="kind">Package family. Defaults to <see cref="HistoryPackageKind.Tape"/>.</param>
        /// <param name="year">Optional year (<c>YYYY</c>) — lists every published month of that year.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The ready packages. Treat this listing as the authoritative set of months that exist.</returns>
        public Task<Page<HistoryPackage>> ListHistoryPackagesAsync(
            HistoryPackageKind kind = HistoryPackageKind.Tape,
            string? year = null,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<HistoryPackage>(
                "/history/packages",
                new[]
                {
                    Param("kind", kind == HistoryPackageKind.Tape ? null : kind.ToQueryValue()),
                    Param("year", year),
                },
                cancellationToken);

        /// <summary>
        /// One monthly package's manifest (file list, sizes, SHA-256 digests).
        /// <b>PRO, or a package subscription.</b> Download the files themselves
        /// with <c>?format=jsonl|csv</c> outside this client.
        /// </summary>
        /// <param name="period">The month, <c>YYYY-MM</c>.</param>
        /// <param name="kind">Package family; <see cref="HistoryPackageKind.Rankings"/> requires ULTRA.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The manifest, or <c>null</c> if the body was empty.</returns>
        public Task<HistoryPackage?> GetHistoryPackageAsync(
            string period,
            HistoryPackageKind kind = HistoryPackageKind.Tape,
            CancellationToken cancellationToken = default) =>
            GetAsync<HistoryPackage>(
                "/history/packages/" + Uri.EscapeDataString(period ?? throw new ArgumentNullException(nameof(period))),
                new[] { Param("kind", kind == HistoryPackageKind.Tape ? null : kind.ToQueryValue()) },
                cancellationToken);

        /// <summary>
        /// The tournament catalogue — the id space
        /// <see cref="Match.TournamentId"/> joins, one row per tournament ×
        /// event type, stable across seasons, in name order.
        /// </summary>
        /// <param name="search">Optional case-insensitive substring match on the tournament name.</param>
        /// <param name="tour">Optional tour filter. Omit for all tours.</param>
        /// <param name="limit">Page size, 1–200. Defaults to 50.</param>
        /// <param name="offset">Page offset. Defaults to 0.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A page of tournaments.</returns>
        public Task<Page<Tournament>> ListTournamentsAsync(
            string? search = null,
            Tour? tour = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default) =>
            GetPageAsync<Tournament>(
                "/tournaments",
                new[]
                {
                    Param("search", search),
                    Param("tour", tour?.ToQueryValue()),
                    Param("limit", limit),
                    Param("offset", offset),
                },
                cancellationToken);

        /// <summary>One tournament by its stable id.</summary>
        /// <param name="tournamentId">The <c>tournament_id</c> carried on match objects.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The tournament, or <c>null</c> if the body was empty.</returns>
        public Task<Tournament?> GetTournamentAsync(string tournamentId, CancellationToken cancellationToken = default) =>
            GetAsync<Tournament>(
                "/tournaments/" + Uri.EscapeDataString(tournamentId ?? throw new ArgumentNullException(nameof(tournamentId))),
                null,
                cancellationToken);

        /// <summary>
        /// Your own usage vs quota — tier, limits, today's calls (current to the
        /// second) and a 30-day history. Works on every tier and the call itself
        /// is quota-exempt.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The usage summary, or <c>null</c> if the body was empty.</returns>
        /// <remarks>
        /// The per-minute window is on the <c>X-RateLimit-*</c> headers of every
        /// response, not here — and the daily reset instant is only carried on
        /// the daily-429 body (<see cref="RateLimitedException.ResetsAt"/>).
        /// </remarks>
        public Task<Usage?> GetUsageAsync(CancellationToken cancellationToken = default) =>
            GetAsync<Usage>("/usage", null, cancellationToken);

        /// <summary>
        /// Bare price ticks of the match's mapped match-winner market, newest
        /// first — no market wrapper. <b>PRO.</b>
        /// </summary>
        /// <param name="matchId">The match id.</param>
        /// <param name="limit">Ticks to return, 1–500. Defaults to 100.</param>
        /// <param name="minutes">Optional lookback window in minutes, 1–1440. Omit for unbounded.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// The ticks. <c>Meta.HasMore</c> means the window was clipped at the
        /// limit — there is no offset on this endpoint; raise the limit or
        /// narrow the minutes window. <c>404</c> when the match has no mapped
        /// market.
        /// </returns>
        public async Task<MatchPrices> ListMatchPricesAsync(
            int matchId,
            int limit = 100,
            int? minutes = null,
            CancellationToken cancellationToken = default)
        {
            var query = new List<KeyValuePair<string, string?>> { Param("limit", limit) };
            if (minutes.HasValue)
            {
                query.Add(Param("minutes", minutes.Value));
            }

            var result = await GetAsync<MatchPrices>(
                "/matches/" + matchId + "/prices", query, cancellationToken).ConfigureAwait(false);
            return result ?? new MatchPrices();
        }

        /// <summary>
        /// Registers an outbound webhook: the API POSTs the same frames the
        /// WebSocket sends to your HTTPS endpoint on every live score commit.
        /// <b>ULTRA, direct keys only.</b>
        /// </summary>
        /// <param name="url">The destination URL — HTTPS only, publicly routable.</param>
        /// <param name="events">The events to subscribe to. Omit for the default (<c>score</c>).</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// The created webhook. <b><see cref="Webhook.Secret"/> is present only
        /// on this response — it is shown exactly once</b>; store it before
        /// letting the object go.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
        /// <remarks>
        /// Up to <b>3 webhooks per key</b>: a fourth registration is a
        /// <c>409</c> <see cref="ConflictException"/> with code
        /// <c>webhook_limit</c> — delete one first. On a marketplace
        /// (RapidAPI) key the endpoint answers <c>403</c> with code
        /// <c>direct_key_required</c>. This POST is never retried
        /// automatically, so a transient failure cannot register the webhook
        /// twice.
        /// </remarks>
        public Task<Webhook?> CreateWebhookAsync(
            string url,
            IEnumerable<WebhookEvent>? events = null,
            CancellationToken cancellationToken = default)
        {
            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var payload = new Dictionary<string, object> { ["url"] = url };
            if (events != null)
            {
                var names = new List<string>();
                foreach (var webhookEvent in events)
                {
                    names.Add(webhookEvent.ToWireValue());
                }

                payload["events"] = names;
            }

            var body = JsonSerializer.Serialize(payload, LiveTennisJson.Options);
            return SendAsync<Webhook>(HttpMethod.Post, "/webhooks", null, body, cancellationToken);
        }

        /// <summary>
        /// Lists your webhooks. <b>ULTRA, direct keys only.</b> The signing
        /// secret is <b>never</b> included here — it is shown only once, on the
        /// registration response.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>Your webhooks (max 3).</returns>
        public Task<Page<Webhook>> ListWebhooksAsync(CancellationToken cancellationToken = default) =>
            GetPageAsync<Webhook>("/webhooks", new KeyValuePair<string, string?>[0], cancellationToken);

        /// <summary>Removes one of your webhooks. <b>ULTRA, direct keys only.</b></summary>
        /// <param name="webhookId">The webhook id.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deletion receipt (the deleted id), or <c>null</c> if the body was empty.</returns>
        public Task<WebhookDeleted?> DeleteWebhookAsync(int webhookId, CancellationToken cancellationToken = default) =>
            SendAsync<WebhookDeleted>(HttpMethod.Delete, "/webhooks/" + webhookId, null, null, cancellationToken);

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

        /// <summary>Appends repeatable <c>player=</c> parameters, enforcing the API's cap of 50 ids.</summary>
        private static void AddPlayerParams(List<KeyValuePair<string, string?>> query, IEnumerable<int>? players)
        {
            if (players is null)
            {
                return;
            }

            var count = 0;
            foreach (var id in players)
            {
                count++;
                if (count > MaxPlayerFilters)
                {
                    throw new ArgumentException("At most " + MaxPlayerFilters + " player ids are accepted.", nameof(players));
                }

                query.Add(Param("player", id));
            }
        }

        private async Task<Page<T>> GetPageAsync<T>(
            string path,
            IEnumerable<KeyValuePair<string, string?>> query,
            CancellationToken cancellationToken)
        {
            var page = await GetAsync<Page<T>>(path, query, cancellationToken).ConfigureAwait(false);
            return page ?? new Page<T>();
        }

        private Task<T?> GetAsync<T>(
            string path,
            IEnumerable<KeyValuePair<string, string?>>? query,
            CancellationToken cancellationToken,
            string? requiredTier = null)
            where T : class =>
            SendAsync<T>(HttpMethod.Get, path, query, jsonBody: null, cancellationToken, requiredTier);

        private async Task<T?> SendAsync<T>(
            HttpMethod method,
            string path,
            IEnumerable<KeyValuePair<string, string?>>? query,
            string? jsonBody,
            CancellationToken cancellationToken,
            string? requiredTier = null)
            where T : class
        {
            ThrowIfDisposed();
            var url = BuildUrl(path, query);

            // POST is not idempotent here (a retried webhook registration could
            // register twice when the first attempt succeeded server-side after
            // the response was lost), so it gets exactly one attempt. GET and
            // DELETE are safe to retry.
            var maxRetries = method == HttpMethod.Post ? 0 : _maxRetries;

            for (var attempt = 0; ; attempt++)
            {
                HttpResponseMessage response;
                try
                {
                    using var request = BuildRequest(method, url, jsonBody);
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
                    if (attempt >= maxRetries)
                    {
                        throw new ApiTimeoutException("Request to " + url + " timed out.", url, ex);
                    }

                    await DelayAsync(Backoff(attempt, null), cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    if (attempt >= maxRetries)
                    {
                        throw new ApiConnectionException("Could not reach " + url + ": " + ex.Message, url, ex);
                    }

                    await DelayAsync(Backoff(attempt, null), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                using (response)
                {
                    var status = (int)response.StatusCode;
                    var retryAfter = RetryAfterSeconds(response);

                    // Retrying can only fix the per-minute window. A long
                    // Retry-After marks a daily-quota or abuse block: burning
                    // retries against it is exactly the behaviour that earns an
                    // abuse block, so surface it immediately instead.
                    var retriableNow = ShouldRetry(status) &&
                        !(status == 429 && retryAfter.HasValue && retryAfter.Value > 60);

                    if (retriableNow && attempt < maxRetries)
                    {
                        await DelayAsync(Backoff(attempt, retryAfter), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw await BuildExceptionAsync(response, path, url, requiredTier).ConfigureAwait(false);
                    }

                    return await DeserializeAsync<T>(response, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string? jsonBody)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            if (jsonBody != null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

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

        private async Task<LiveTennisApiException> BuildExceptionAsync(
            HttpResponseMessage response,
            string path,
            string url,
            string? requiredTierOverride)
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

            var error = ExtractErrorBody(body);
            var headers = CollectHeaders(response);
            var reason = string.IsNullOrEmpty(response.ReasonPhrase) ? "request failed" : response.ReasonPhrase!;
            var message = error.Code ?? reason;
            var requiredTier = requiredTierOverride ?? RequiredTierFor(path);
            var retryAfter = RetryAfterSeconds(response);

            return ExceptionFactory.ForStatus(
                response.StatusCode, message, error.Code, url, body, headers, requiredTier, retryAfter,
                error.ResetsAt, error.RetryAtEpoch);
        }

        /// <summary>The machine-readable pieces of an error body.</summary>
        private readonly struct ErrorBody
        {
            public ErrorBody(string? code, string? resetsAt, long? retryAtEpoch)
            {
                Code = code;
                ResetsAt = resetsAt;
                RetryAtEpoch = retryAtEpoch;
            }

            public string? Code { get; }

            public string? ResetsAt { get; }

            public long? RetryAtEpoch { get; }
        }

        private static ErrorBody ExtractErrorBody(string? body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return default;
            }

            try
            {
                using var doc = JsonDocument.Parse(body!);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return default;
                }

                string? code = null;
                if (doc.RootElement.TryGetProperty("error", out var error) &&
                    error.ValueKind == JsonValueKind.String)
                {
                    var value = error.GetString();
                    code = string.IsNullOrEmpty(value) ? null : value;
                }

                // Daily-window 429s carry an absolute reset instant.
                string? resetsAt = null;
                if (doc.RootElement.TryGetProperty("resets_at", out var resets) &&
                    resets.ValueKind == JsonValueKind.String)
                {
                    resetsAt = resets.GetString();
                }

                // Abuse-throttle 429s carry the block's end as Unix seconds.
                long? retryAtEpoch = null;
                if (doc.RootElement.TryGetProperty("retry_at_epoch", out var retryAt) &&
                    retryAt.ValueKind == JsonValueKind.Number &&
                    retryAt.TryGetInt64(out var epoch))
                {
                    retryAtEpoch = epoch;
                }

                return new ErrorBody(code, resetsAt, retryAtEpoch);
            }
            catch (JsonException)
            {
                // Non-JSON body — no structured fields to extract.
                return default;
            }
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
