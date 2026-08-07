# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-08-07

Full API parity: every path of the public v1 OpenAPI spec now has a typed
method (deliberate exclusions documented in the README).

### Added

- **Tournament catalogue**: `ListTournamentsAsync(search, tour)` and
  `GetTournamentAsync(id)` (`/tournaments`, FREE) — the id space
  `Match.TournamentId` joins, with curated city/country (ISO-3166 alpha-2) and
  `category` (null where the catalogues don't agree, never guessed).
- **Usage**: `GetUsageAsync()` (`/usage`, any tier, quota-exempt) — tier,
  temporary-grant fields, limits, today's calls and the 30-day history.
- **Bare price ticks**: `ListMatchPricesAsync(matchId, limit, minutes)`
  (`/matches/{id}/prices`, PRO) — no market wrapper, limit up to 500,
  minutes-bounded lookback, `Meta.HasMore` (no offset on this endpoint).
- **Webhooks** (`/webhooks`, ULTRA, direct keys only): `CreateWebhookAsync`
  (the `secret` is returned exactly once, on the 201 only),
  `ListWebhooksAsync` (never includes the secret), `DeleteWebhookAsync`.
  Max 3 per key — a fourth registration is a `409` `webhook_limit`.
- **Price model**: `PriceSource` and `Synthetic` (true = bid/ask estimated
  from mid; never mistake a synthesised quote for a live book).
- **Errors**: `ConflictException` (409, e.g. `webhook_limit`).

### Changed

- The transport now supports POST/DELETE. POST requests are **never retried**
  (a retried webhook registration could register twice when the first attempt
  succeeded server-side after the response was lost); GET and DELETE keep the
  existing retry policy.

## [1.1.0] - 2026-08-07

### Added

- **Head-to-head**: `GetHeadToHeadAsync(p1, p2)` (`/h2h`, BASIC) — totals,
  per-surface split and the meetings list across the 1968–2022 results archive
  and our own completed matches, with typed `HeadToHead*` models.
- **Results archive (1968–2022)**: `ListArchiveMatchesAsync`,
  `GetArchiveMatchAsync`, `ListArchivePlayersAsync`, `GetArchiveCareerAsync`
  (`/history/archive/matches|players|career`, BASIC) — winner/loser-shaped
  results with ranks at the time, archive player bios, and career aggregates
  with the summed serve block.
- **Per-match tape**: `GetMatchTapeAsync(matchId, sequence)`
  (`/history/matches/{id}`, BASIC) with `TapeSequence.Raw|Clean`; clean rows
  carry the new `point_winner`, and the response includes per-set tiebreak
  final scores and full coverage meta.
- **In-play statistics**: `GetMatchStatisticsAsync`
  (`/matches/{id}/statistics`, ULTRA) — fully typed derived + measured
  families, per-family freshness/coverage and divergence reporting.
- **Rankings**: `ListRankingsAsync(system, asOf)` (listing mode, PRO) and
  `GetPlayerRankingsAsync(playerIds, systems, asOf)` (per-player as-of records,
  ULTRA), with `previous_rank`, UTR ratings and coverage meta
  (`oldest_available`).
- **Rally / shot-by-shot**: `ListRallyMatchesAsync`, `GetRallyMatchAsync`,
  `GetMatchRallyAsync` (`/rally/matches`, `/rally/matches/{id}`,
  `/history/matches/{id}/rally`, ULTRA) — charted points with parsed shots,
  verbatim charter notation and per-match parse quality.
- **Charting aggregates**: `GetChartingPlayerAsync`, `GetChartingMatchAsync`
  (`/charting/players`, `/charting/matches/{id}`, ULTRA).
- **Push feed token**: `GetWsTokenAsync` (`/ws-token`, ULTRA) — typed
  `WsToken` with `WsUrl` and the channel vocabulary (`match:{match_id}`,
  `slate:all`).
- **Bulk packages**: `ListHistoryPackagesAsync(kind, year)` and
  `GetHistoryPackageAsync(period, kind)` (`/history/packages`, PRO) with the
  `rankings` package family.
- **Match model**: `Tour` (filter vocabulary, groupable), `TournamentId`,
  `RoundCode`, `Withdrew`, and the `Tape` coverage summary on
  `/history/matches` rows. **Fixture model**: `StartTime`, `Player1Id`,
  `Player2Id`, `RoundCode`. **ListMeta**: `Total` and `HasMore`.
- **List filters**: `players` (≤ 50 ids, validated client-side), `from`/`to`,
  `country` on `ListMatchesAsync`; plus `tour` on
  `ListCompletedMatchesAsync`.
- **Errors**: `AbuseThrottledException` (429 `abuse_throttled`) with
  `RetryAtEpoch`/`RetryAt`; daily-window 429s surface the absolute
  `ResetsAt`/`ResetsAtTime` from the body. `UpgradeRequiredException` now
  reports the correct tier for all new endpoints, including the two `/rankings`
  modes (PRO listing vs ULTRA per-player).

### Changed

- A 429 whose `Retry-After` exceeds 60 seconds (daily quota or abuse block) is
  no longer retried — it is surfaced immediately, because retrying against a
  long block is exactly the behaviour that earns an abuse throttle.
- `RateLimitedException` is no longer sealed (it is the base of
  `AbuseThrottledException`).

### Documentation

- README rewritten to the current API surface: full endpoint/tier table, quota
  table (2026-08-06 grid: FREE 100/day, BASIC 1,000/day, PRO 10,000/day, ULTRA
  500,000/day), auth section and links.
- LICENSE copyright year stated (2026).

## [1.0.0] - 2026-07-24

### Added

- Initial release: full FREE-tier surface (matches, scores, players, fixtures)
  plus PRO/ULTRA endpoints (events, markets, analysis), typed models with a
  `JsonExtensionData` forward-compatibility net, a typed exception hierarchy,
  retry with jittered backoff, and `netstandard2.0`/`net8.0` targets.

[1.2.0]: https://github.com/livetennisapi/livetennisapi-dotnet/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/livetennisapi/livetennisapi-dotnet/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/livetennisapi/livetennisapi-dotnet/releases/tag/v1.0.0
