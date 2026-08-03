# LiveTennisApi — .NET client for the Live Tennis API

Official .NET client for the [Live Tennis API](https://livetennisapi.com) —
real-time tennis scores, players, fixtures and (on paid tiers) match events,
market prices and model analysis for ATP, WTA, Challenger, ITF and juniors.

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

Get a free key (no card, 1000 requests/day) at
<https://livetennisapi.com/subscribe/free>.

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
| `ListMatchesAsync(status, tour, limit, offset)` | `/matches` | FREE |
| `GetMatchAsync(matchId)` | `/matches/{id}` | FREE |
| `GetMatchScoreAsync(matchId)` | `/matches/{id}/score` | FREE |
| `SearchPlayersAsync(search, limit, offset)` | `/players` | FREE |
| `GetPlayerAsync(playerId)` | `/players/{id}` | FREE |
| `ListFixturesAsync(tour, limit, offset)` | `/fixtures` | FREE |
| `ListMatchEventsAsync(matchId, limit, offset)` | `/matches/{id}/events` | PRO |
| `ListMarketsAsync(matchId)` | `/markets` | PRO |
| `GetMarketPricesAsync(matchId, limit)` | `/markets/{id}/prices` | PRO |
| `ListCompletedMatchesAsync(limit, offset)` | `/history/matches` | BASIC |
| `GetMatchAnalysisAsync(matchId)` | `/matches/{id}/analysis` | ULTRA |

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

### Players and doubles

- `Player.Tour` is the record's **own** granular tour string (for example
  `juniors_boys`), and a doubles team reports it **UPPERCASE** (`ATP`). It is an
  opaque string — do **not** confuse it with the `Tour` request filter enum.
- On a doubles team, `DataCompleteness.Known` and `.Of` are **`null`** (per-player
  biography does not apply — distinct from `0`), and `.Note` explains why.

### The `tour` filter

`ListMatchesAsync` and `ListFixturesAsync` accept a `Tour` enum —
`Atp`, `Wta`, `Challenger`, `Itf`, `Juniors`. An unrecognised value is a `400`
(`BadRequestException`), never a silent pass-through.

> **Note:** `/fixtures` (`ListFixturesAsync`) currently also returns some already
> finished matches (`Status == "finished"`). This is a known upstream quirk; the
> client passes it through unfiltered.

## Errors

Everything derives from `LiveTennisApiException` (carrying `StatusCode`, `Code`,
`Body`, `Headers`, `RequestUri`). The common cases are distinguishable by type:

```csharp
try
{
    var analysis = await client.GetMatchAnalysisAsync(matchId);
}
catch (UpgradeRequiredException ex)      // 403 — tier too low
{
    Console.WriteLine(ex.RequiredTier);  // e.g. "ULTRA"
}
catch (RateLimitedException ex)          // 429
{
    await Task.Delay(TimeSpan.FromSeconds(ex.RetryAfterSeconds ?? 60));
}
catch (UnauthorizedException)            // 401 — bad/missing key
{
}
```

| Exception | When |
|---|---|
| `BadRequestException` | 400 |
| `UnauthorizedException` | 401 |
| `UpgradeRequiredException` | 403 (adds `RequiredTier`) |
| `NotFoundException` | 404 |
| `RateLimitedException` | 429 (adds `RetryAfterSeconds`) |
| `ServerException` | 5xx |
| `ApiConnectionException` / `ApiTimeoutException` | no response / timeout |

Transient failures (`429`, `5xx`) are retried automatically (default 2 attempts),
honouring `Retry-After`.

## License

MIT — see [LICENSE](LICENSE).

## Affiliate program

Know developers who need tennis data? The [affiliate program](https://affiliates.livetennisapi.com/program) pays 51% recurring commission for the life of every referred subscription — 30-day cookie, and the people you refer get 10% off.
