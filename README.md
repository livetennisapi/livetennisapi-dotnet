# LiveTennisApi — .NET client for the Live Tennis API

Official .NET client for the [Live Tennis API](https://livetennisapi.com) —
real-time tennis scores, players, fixtures, deep match history and (on paid
tiers) head-to-head, the 1968–2022 results archive, rankings, in-play
statistics, rally/charting shot data, match events, market prices, webhooks and
model analysis for **ATP, WTA, Challenger, ITF and juniors**.

[![ci](https://github.com/livetennisapi/livetennisapi-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/livetennisapi/livetennisapi-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/LiveTennisApi.svg)](https://www.nuget.org/packages/LiveTennisApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

- **Multi-targeted**: `netstandard2.0` (Framework, Xamarin, Unity, older Core)
  and `net8.0`.
- **`HttpClient`- and `IHttpClientFactory`-friendly**, fully `async` with
  `CancellationToken` support.
- **Typed models** with correct nullability, and a **typed exception hierarchy**.
- Tolerant by design: unknown JSON fields are preserved, never rejected —
  additive changes ship within `v1`.

## Install

```bash
dotnet add package LiveTennisApi
```

## Quickstart

```csharp
using LiveTennisApi;
using LiveTennisApi.Models;

using var client = new LiveTennisApiClient("twjp_your_key");

var live = await client.ListMatchesAsync(MatchStatus.Live);
foreach (var m in live.Data)
{
    Console.WriteLine($"{m.Tournament}: {m.Players?.P1?.Name} vs {m.Players?.P2?.Name}");
}
```

Get a free key (no card, 100 requests/day) at
<https://livetennisapi.com/subscribe/free>. On a free key, poll no faster than
every ~15 minutes; an always-on dashboard belongs on BASIC.

### With `IHttpClientFactory`

Hand the client an `HttpClient` and it will **not** dispose it — the factory
owns its lifetime:

```csharp
services.AddHttpClient();

// elsewhere:
var http = httpClientFactory.CreateClient();
var client = new LiveTennisApiClient(http, "twjp_your_key");
```

### Options

```csharp
var client = new LiveTennisApiClient("twjp_your_key", new LiveTennisApiClientOptions
{
    AuthHeader = AuthHeader.Bearer, // or AuthHeader.ApiKey for X-API-Key
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 2,                 // 429 and 5xx only
});
```

## Endpoints

| Method | Endpoint | Tier |
|---|---|---|
| `HealthAsync` | `/health` | none |
| `ListMatchesAsync(status, tour, …, players, from, to, country)` | `/matches` | FREE¹ |
| `GetMatchAsync(matchId)` | `/matches/{id}` | FREE |
| `GetMatchScoreAsync(matchId)` | `/matches/{id}/score` | FREE |
| `SearchPlayersAsync(search, limit, offset)` | `/players` | FREE |
| `GetPlayerAsync(playerId)` | `/players/{id}` | FREE |
| `ListFixturesAsync(tour, limit, offset)` | `/fixtures` | FREE |
| `ListTournamentsAsync(search, tour, …)` | `/tournaments` | FREE |
| `GetTournamentAsync(tournamentId)` | `/tournaments/{id}` | FREE |
| `GetUsageAsync()` | `/usage` | any (quota-exempt) |
| `ListCompletedMatchesAsync(…, tour, players, from, to, country)` | `/history/matches` | BASIC² |
| `GetMatchTapeAsync(matchId, sequence)` | `/history/matches/{id}` | BASIC² |
| `GetHeadToHeadAsync(p1, p2)` | `/h2h` | BASIC² |
| `ListArchiveMatchesAsync(tour, name, from, to, round, level, …)` | `/history/archive/matches` | BASIC² |
| `GetArchiveMatchAsync(archiveId)` | `/history/archive/matches/{id}` | BASIC² |
| `ListArchivePlayersAsync(name, tour, …)` | `/history/archive/players` | BASIC² |
| `GetArchiveCareerAsync(name)` | `/history/archive/career` | BASIC² |
| `ListMatchEventsAsync(matchId, limit, offset)` | `/matches/{id}/events` | PRO |
| `ListMarketsAsync(matchId)` | `/markets` | PRO |
| `GetMarketPricesAsync(matchId, limit)` | `/markets/{id}/prices` | PRO |
| `ListMatchPricesAsync(matchId, limit, minutes)` | `/matches/{id}/prices` | PRO |
| `ListRankingsAsync(system, asOf, …)` | `/rankings` (listing mode) | PRO |
| `ListHistoryPackagesAsync(kind, year)` | `/history/packages` | PRO³ |
| `GetHistoryPackageAsync(period, kind)` | `/history/packages/{period}` | PRO³ |
| `GetMatchAnalysisAsync(matchId)` | `/matches/{id}/analysis` | ULTRA |
| `GetMatchStatisticsAsync(matchId)` | `/matches/{id}/statistics` | ULTRA |
| `GetPlayerRankingsAsync(playerIds, systems, asOf, …)` | `/rankings` (per-player as-of) | ULTRA |
| `ListRallyMatchesAsync(player, from, to, surface, gender, …)` | `/rally/matches` | ULTRA |
| `GetRallyMatchAsync(rallyMatchId, …)` | `/rally/matches/{id}` | ULTRA |
| `GetMatchRallyAsync(matchId, …)` | `/history/matches/{id}/rally` | ULTRA |
| `GetChartingPlayerAsync(name, gender)` | `/charting/players` | ULTRA |
| `GetChartingMatchAsync(chartingMatchId)` | `/charting/matches/{id}` | ULTRA |
| `GetWsTokenAsync()` | `/ws-token` | ULTRA |
| `CreateWebhookAsync(url, events)` | `POST /webhooks` | ULTRA⁴ |
| `ListWebhooksAsync()` | `/webhooks` | ULTRA⁴ |
| `DeleteWebhookAsync(webhookId)` | `DELETE /webhooks/{id}` | ULTRA⁴ |

¹ `status=completed` needs BASIC+ (or any History plan).
² Or any History plan — History grants work on a FREE core key.
³ `kind=rankings` and `year=` listings need ULTRA / History Business / a 1-year
package.
⁴ Direct keys only — a marketplace (RapidAPI) key gets `403 direct_key_required`.

This covers **every path of the public v1 OpenAPI spec**. Deliberate
exclusions: the undocumented gateway aliases (unstable, do not use); the
server's HTML views and static assets (browser surfaces, not API); and the
package **file downloads** — `GetHistoryPackageAsync` returns the manifest
(filenames, sizes, SHA-256); stream the file itself with
`GET /history/packages/{period}?format=jsonl|csv` using your own HTTP client,
since a multi-GB download does not belong behind a JSON deserializer.

## Quotas

| Tier | Requests/min | Requests/day | Price |
|---|---|---|---|
| FREE | 30 | 100 | $0 |
| BASIC | 60 | 1,000 | $9.99/mo |
| PRO | 300 | 10,000 | $29.99/mo |
| ULTRA | 600 | 500,000 | $99.99/mo |

Every response carries `X-RateLimit-Limit` / `X-RateLimit-Remaining` /
`X-RateLimit-Reset` headers, and 429s carry `Retry-After`. The daily window
resets at the absolute instant given in the daily-429 body's `resets_at`
(surfaced as `RateLimitedException.ResetsAt`) — it is derived from the
account's local midnight, not from any fixed UTC time, so always read the
instant rather than assuming one.

## Auth

`Authorization: Bearer twjp_...` is the preferred scheme (the default);
`X-API-Key: twjp_...` also works via `AuthHeader.ApiKey`. Only `/health` is
unauthenticated.

## Reading a score — gotchas the models encode

```csharp
var score = (await client.GetMatchScoreAsync(matchId));
```

- **`Points` are strings** — `"0"`, `"15"`, `"30"`, `"40"`, `"AD"`, not integers.
- **`Games` is player-major**: `[games_p1, games_p2]`, and *each side is a
  per-set list*. `[[6,3,2],[4,6,1]]` reads 6-4, 3-6, 2-1. Use
  `score.GamesForSet(setIndex)` instead of indexing by hand.
- **`Server` is nullable** (`int?`) — `null` between points and on a finished
  match.
- **`Match.Score` is nullable** — an upcoming match has no score yet.
- **`WinProbabilityP1` / `Danger`** are populated on the ULTRA tier only.

### Match metadata

- `Match.Tour` is the **filter vocabulary** (`atp`, `wta`, `challenger`, `itf`,
  `juniors`) and is safe to group on; `null` means the feed never stated a tour
  (exhibitions, team and mixed events). `Match.TournamentId` is a stable
  tournament identity; `Match.RoundCode` is the normalized round.
- On completed matches that ended early, `Match.EventStatus` says how
  (`Retired`, `Walk Over`, …) and `Match.Withdrew` says which player
  retired/conceded (`1`/`2`).

### The tape (point-by-point history)

`GetMatchTapeAsync(matchId, TapeSequence.Clean)` returns one row per distinct
score state — and only clean rows carry `PointWinner` (who won the point;
`null` on gaps and the first row, never guessed). The response also carries
per-set tiebreak final scores (`Tiebreaks`) from observed states only. A null
row `Timestamp` marks a reconstructed row. **Check `Meta.Coverage` /
`Meta.PointSource` before backtesting** — tapes are not guaranteed to cover the
whole match. Works on live matches too.

### Players and doubles

- `Player.Tour` is the record's **own** granular tour string (for example
  `juniors_boys`), and a doubles team reports it **UPPERCASE** (`ATP`). It is an
  opaque string — do **not** confuse it with the `Tour` request filter enum or
  with `Match.Tour`.
- On a doubles team, `DataCompleteness.Known` and `.Of` are **`null`** (per-player
  biography does not apply — distinct from `0`), and `.Note` explains why.

### Filters

- `Tour` enum: `Atp`, `Wta`, `Challenger`, `Itf`, `Juniors`. An unrecognised
  wire value is a `400` (`BadRequestException`), never a silent pass-through.
- `players:` accepts up to **50 ids** (either participant matches; the client
  rejects more before the wire). `from:`/`to:` take `YYYY-MM-DD` or ISO 8601
  datetimes. `country:` takes the lowercase 3-letter code the `Player.Country`
  field returns (IOC-style, e.g. `ned`, `sui` — not ISO-3166).
- Archive and h2h endpoints key on **names** (archive people have no roster
  ids); a fragment matching more than one player is a `400 ambiguous_name`
  listing the candidates.

> **Note:** `/fixtures` (`ListFixturesAsync`) currently also returns some already
> finished matches (`Status == "finished"`). This is a known upstream quirk; the
> client passes it through unfiltered.

### Webhooks (ULTRA, direct keys only)

The API POSTs the same frames the WebSocket sends to your HTTPS endpoint on
every live score commit:

```csharp
var hook = await client.CreateWebhookAsync(
    "https://example.com/hooks/tennis",
    new[] { WebhookEvent.Score, WebhookEvent.BreakPoint });

// hook.Secret is shown EXACTLY ONCE, on this response — store it now.
// ListWebhooksAsync() never returns it again.
```

- **Max 3 webhooks per key** — a fourth registration throws
  `ConflictException` (`409 webhook_limit`); delete one first.
- The registration POST is **never retried automatically**, so a transient
  failure cannot register the same webhook twice.
- Verify deliveries against the stored signing secret.

## Errors

Everything derives from `LiveTennisApiException` (carrying `StatusCode`, `Code`,
`Body`, `Headers`, `RequestUri`). The common cases are distinguishable by type:

```csharp
try
{
    var analysis = await client.GetMatchAnalysisAsync(matchId);
}
catch (AbuseThrottledException ex)       // 429 abuse_throttled — 24h block
{
    // Fix the retry loop; blocked until ex.RetryAt (from retry_at_epoch).
}
catch (RateLimitedException ex)          // 429
{
    if (ex.ResetsAt is not null)
    {
        // Daily quota — resets at the absolute instant from the body.
        var resumeAt = ex.ResetsAtTime;
    }
    else
    {
        await Task.Delay(TimeSpan.FromSeconds(ex.RetryAfterSeconds ?? 60));
    }
}
catch (UpgradeRequiredException ex)      // 403 — tier too low
{
    Console.WriteLine(ex.RequiredTier);  // e.g. "ULTRA"
}
catch (UnauthorizedException)            // 401 — bad/missing key
{
}
```

| Exception | When |
|---|---|
| `BadRequestException` | 400 (`bad_tour`, `bad_date`, `ambiguous_name`, …) |
| `UnauthorizedException` | 401 |
| `UpgradeRequiredException` | 403 (adds `RequiredTier`) |
| `NotFoundException` | 404 (check `Code` — `not_charted` ≠ `not_found`) |
| `ConflictException` | 409 (e.g. `webhook_limit`) |
| `RateLimitedException` | 429 (adds `RetryAfterSeconds`, `ResetsAt` on the daily window) |
| `AbuseThrottledException` | 429 `abuse_throttled` (adds `RetryAtEpoch`/`RetryAt`) |
| `ServerException` | 5xx |
| `ApiConnectionException` / `ApiTimeoutException` | no response / timeout |

Transient failures (`429`, `5xx`) are retried automatically (default 2 attempts),
honouring `Retry-After` — except a 429 whose `Retry-After` exceeds 60 s (a
daily/abuse block), which is surfaced immediately rather than retried against.
POST requests (webhook registration) are never retried; GET and DELETE are.

## Links

- Docs: <https://docs.livetennisapi.com>
- Free API key: <https://livetennisapi.com/subscribe/free>
- Discord: <https://discord.gg/f8WUZHgDm6>
- GitHub org: <https://github.com/livetennisapi>

## License

MIT — see [LICENSE](LICENSE).

## Affiliate program

Know developers who need tennis data? The [affiliate program](https://affiliates.livetennisapi.com/program) pays 51% recurring commission for the life of every referred subscription — 30-day cookie, and the people you refer get 10% off.
