using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>One resolved side of a head-to-head request.</summary>
    public sealed record HeadToHeadPlayer : LiveTennisModel
    {
        /// <summary>The resolved full name.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    /// <summary>The two resolved players of a head-to-head record.</summary>
    public sealed record HeadToHeadPlayers : LiveTennisModel
    {
        /// <summary>The player resolved from the <c>p1</c> fragment.</summary>
        [JsonPropertyName("p1")]
        public HeadToHeadPlayer? P1 { get; init; }

        /// <summary>The player resolved from the <c>p2</c> fragment.</summary>
        [JsonPropertyName("p2")]
        public HeadToHeadPlayer? P2 { get; init; }
    }

    /// <summary>Win totals over the pairing.</summary>
    public sealed record HeadToHeadTotals : LiveTennisModel
    {
        /// <summary>Meetings won by p1 (of this h2h).</summary>
        [JsonPropertyName("p1_wins")]
        public int? P1Wins { get; init; }

        /// <summary>Meetings won by p2 (of this h2h).</summary>
        [JsonPropertyName("p2_wins")]
        public int? P2Wins { get; init; }

        /// <summary>Meetings with a known winner.</summary>
        [JsonPropertyName("meetings")]
        public int? Meetings { get; init; }

        /// <summary>Meetings with no derivable winner — never counted in the wins.</summary>
        [JsonPropertyName("undecided")]
        public int? Undecided { get; init; }
    }

    /// <summary>The per-surface win split of a head-to-head.</summary>
    public sealed record HeadToHeadSurfaceSplit : LiveTennisModel
    {
        /// <summary>Wins for p1 on this surface.</summary>
        [JsonPropertyName("p1")]
        public int? P1 { get; init; }

        /// <summary>Wins for p2 on this surface.</summary>
        [JsonPropertyName("p2")]
        public int? P2 { get; init; }
    }

    /// <summary>One meeting between the two players, newest first.</summary>
    public sealed record HeadToHeadMeeting : LiveTennisModel
    {
        /// <summary>
        /// Which half of the product served this row: <c>archive</c> (1968–2022
        /// results archive) or <c>current</c> (our own completed matches, 2023→).
        /// </summary>
        [JsonPropertyName("era")]
        public string? Era { get; init; }

        /// <summary>Match (or tournament-start) date, ISO 8601 date string, or <c>null</c>.</summary>
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        /// <summary>Tournament name, or <c>null</c>.</summary>
        [JsonPropertyName("tournament")]
        public string? Tournament { get; init; }

        /// <summary>Source tier code (archive rows only), or <c>null</c>.</summary>
        [JsonPropertyName("level")]
        public string? Level { get; init; }

        /// <summary>Round, or <c>null</c>.</summary>
        [JsonPropertyName("round")]
        public string? Round { get; init; }

        /// <summary>Normalized round code (current rows only), or <c>null</c>.</summary>
        [JsonPropertyName("round_code")]
        public string? RoundCode { get; init; }

        /// <summary>Surface, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>The final score as published (archive rows), or <c>null</c>.</summary>
        [JsonPropertyName("score")]
        public string? Score { get; init; }

        /// <summary>
        /// How the meeting ended (<c>completed</c>, <c>retired</c>,
        /// <c>walkover</c>, …), so walkovers and retirements can be excluded.
        /// </summary>
        [JsonPropertyName("outcome")]
        public string? Outcome { get; init; }

        /// <summary>
        /// The winner, <c>1</c> or <c>2</c> <b>of this h2h</b> (p1/p2 as
        /// requested), or <c>null</c> when underivable.
        /// </summary>
        [JsonPropertyName("winner")]
        public int? Winner { get; init; }

        /// <summary>The archive match id (archive rows only) — joins the archive detail endpoint.</summary>
        [JsonPropertyName("archive_match_id")]
        public int? ArchiveMatchId { get; init; }

        /// <summary>Our match id (current rows only) — read the score from the match endpoints.</summary>
        [JsonPropertyName("match_id")]
        public int? MatchId { get; init; }
    }

    /// <summary>
    /// The head-to-head record between two players, assembled from both halves
    /// of the product: the results archive (1968–2022), where the winner is a
    /// stored column, and our own completed matches (2023→), where the winner is
    /// derived from the final recorded state. <b>BASIC tier and above.</b>
    /// </summary>
    /// <remarks>
    /// Names are the keys — archive people have no roster ids. A fragment
    /// matching more than one player is refused with a <c>400</c>
    /// <c>ambiguous_name</c> listing the candidates, because two people summed
    /// into one record is a wrong answer, not a convenience.
    /// </remarks>
    public sealed record HeadToHead : LiveTennisModel
    {
        /// <summary>The resolved names, or <c>null</c> when no player matches the fragments.</summary>
        [JsonPropertyName("players")]
        public HeadToHeadPlayers? Players { get; init; }

        /// <summary>Win totals. Totals count meetings with a known winner.</summary>
        [JsonPropertyName("totals")]
        public HeadToHeadTotals? Totals { get; init; }

        /// <summary>Per-surface win split; keys are surface names plus <c>unknown</c>.</summary>
        [JsonPropertyName("by_surface")]
        public IReadOnlyDictionary<string, HeadToHeadSurfaceSplit>? BySurface { get; init; }

        /// <summary>The meetings, newest first, capped at 200.</summary>
        [JsonPropertyName("meetings")]
        public IReadOnlyList<HeadToHeadMeeting>? Meetings { get; init; }

        /// <summary>
        /// Per-player serve/return/break-point aggregates over the pairing.
        /// <b>ULTRA only</b>; absent below that tier. Kept as raw JSON — the
        /// block carries <c>archive_serve</c> (from 1991) and <c>current</c>
        /// (2023+) families, each with <c>meetings_with_stats</c>.
        /// </summary>
        [JsonPropertyName("stats")]
        public JsonElement? Stats { get; init; }
    }
}
