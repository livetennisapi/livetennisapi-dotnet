using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// One ranking record in force at the requested instant.
    /// </summary>
    /// <remarks>
    /// <see cref="System"/> is always explicit and systems are never collapsed
    /// into a single "rank" — they are not comparable. ATP/WTA and the ITF
    /// circuits populate <see cref="Rank"/>+<see cref="Points"/>; UTR populates
    /// <see cref="Rating"/> and leaves rank/points <c>null</c>, because UTR is a
    /// rating and has no rank.
    /// </remarks>
    public sealed record RankingRecord : LiveTennisModel
    {
        /// <summary>
        /// Our player id. On listing rows this may be <c>null</c> for players
        /// outside our roster — the table has no silent holes.
        /// </summary>
        [JsonPropertyName("player_id")]
        public int? PlayerId { get; init; }

        /// <summary>
        /// The name as the ranking publisher printed it — present on listing
        /// rows (where <see cref="PlayerId"/> may be null), absent on per-player
        /// records.
        /// </summary>
        [JsonPropertyName("player_name")]
        public string? PlayerName { get; init; }

        /// <summary>
        /// The ranking system: <c>atp</c>, <c>wta</c>, <c>itf_jt</c>,
        /// <c>itf_mt</c>, <c>itf_wt</c>, or <c>utr</c>.
        /// </summary>
        [JsonPropertyName("system")]
        public string? System { get; init; }

        /// <summary>The player's tour, or <c>null</c>.</summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>The rank. <c>null</c> for UTR.</summary>
        [JsonPropertyName("rank")]
        public int? Rank { get; init; }

        /// <summary>Ranking points. <c>null</c> for UTR.</summary>
        [JsonPropertyName("points")]
        public int? Points { get; init; }

        /// <summary>
        /// The rank at the immediately preceding snapshot week (ATP/WTA only;
        /// <c>null</c> when no prior week is held, and always <c>null</c> for
        /// ITF/UTR).
        /// </summary>
        [JsonPropertyName("previous_rank")]
        public int? PreviousRank { get; init; }

        /// <summary>
        /// The circuit's own signed weekly movement (ITF systems only;
        /// <c>null</c> elsewhere).
        /// </summary>
        [JsonPropertyName("rank_movement")]
        public int? RankMovement { get; init; }

        /// <summary>UTR rating; <c>null</c> for every other system.</summary>
        [JsonPropertyName("rating")]
        public double? Rating { get; init; }

        /// <summary>
        /// The publication week this record took effect (<c>YYYY-MM-DD</c>).
        /// Records ingested live rather than from the official weekly
        /// publication are bucketed to the observed week.
        /// </summary>
        [JsonPropertyName("effective_date")]
        public string? EffectiveDate { get; init; }

        /// <summary>When the record was observed (UTC ISO string), or <c>null</c>.</summary>
        [JsonPropertyName("observed_at")]
        public string? ObservedAt { get; init; }
    }

    /// <summary>
    /// What resolved against what was asked. Read it before trusting an empty
    /// result — ITF and UTR history begins 2026-07-29 and cannot be
    /// reconstructed earlier, so a request before that date correctly returns
    /// nothing for those systems.
    /// </summary>
    public sealed record RankingCoverage : LiveTennisModel
    {
        /// <summary>The as-of date that was applied, or <c>null</c>.</summary>
        [JsonPropertyName("as_of")]
        public string? AsOf { get; init; }

        /// <summary>Player ids requested.</summary>
        [JsonPropertyName("players_requested")]
        public int? PlayersRequested { get; init; }

        /// <summary>Player ids that resolved.</summary>
        [JsonPropertyName("players_resolved")]
        public int? PlayersResolved { get; init; }

        /// <summary>Systems requested.</summary>
        [JsonPropertyName("systems_requested")]
        public IReadOnlyList<string>? SystemsRequested { get; init; }

        /// <summary>Systems that resolved.</summary>
        [JsonPropertyName("systems_resolved")]
        public IReadOnlyList<string>? SystemsResolved { get; init; }

        /// <summary>Earliest effective date held, per requested system (value may be <c>null</c>).</summary>
        [JsonPropertyName("oldest_available")]
        public IReadOnlyDictionary<string, string?>? OldestAvailable { get; init; }
    }

    /// <summary>The pagination + coverage envelope of <c>/rankings</c>.</summary>
    public sealed record RankingListMeta : LiveTennisModel
    {
        /// <summary>The page size that was applied.</summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; init; }

        /// <summary>The offset that was applied.</summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; init; }

        /// <summary>The number of items on this page.</summary>
        [JsonPropertyName("count")]
        public int? Count { get; init; }

        /// <summary>Size of the whole filtered set, or <c>null</c>.</summary>
        [JsonPropertyName("total")]
        public int? Total { get; init; }

        /// <summary>Whether more results exist beyond this page.</summary>
        [JsonPropertyName("has_more")]
        public bool? HasMore { get; init; }

        /// <summary>What resolved against what was asked.</summary>
        [JsonPropertyName("coverage")]
        public RankingCoverage? Coverage { get; init; }
    }

    /// <summary>One page of ranking records with the rankings-specific meta.</summary>
    public sealed record RankingsResult : LiveTennisModel
    {
        /// <summary>The ranking records. Never <c>null</c> (empty when absent).</summary>
        [JsonPropertyName("data")]
        public IReadOnlyList<RankingRecord> Data { get; init; } = new List<RankingRecord>();

        /// <summary>The pagination + coverage envelope, if the server sent one.</summary>
        [JsonPropertyName("meta")]
        public RankingListMeta? Meta { get; init; }
    }
}
