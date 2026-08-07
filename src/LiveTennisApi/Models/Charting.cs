using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// Career shot-level charting aggregate for one player, from the Match
    /// Charting Project. <b>ULTRA only.</b>
    /// </summary>
    /// <remarks>
    /// The deepest serve/return profile held: serve placement (deuce/ad ×
    /// wide/body/T), return depth and outcomes, net and serve-and-volley
    /// conversion, clutch break/game/set-point serving and returning, winners and
    /// unforced errors by wing, and rally-length and shot-direction tendencies —
    /// summed over the player's charted matches. Every field is a raw <b>sum</b>
    /// over the player's Total rows, and <see cref="MatchesCharted"/> states the
    /// sample. Coverage is curated — 11,646 charted matches across both tours
    /// back to the 1960s, concentrated on the majors, <b>not</b> full-slate
    /// coverage. An ambiguous name fragment is refused with candidates
    /// (<c>ambiguous_name</c>); disambiguate with the gender filter.
    /// </remarks>
    public sealed record ChartingPlayerAggregate : LiveTennisModel
    {
        /// <summary>The resolved player header (raw JSON).</summary>
        [JsonPropertyName("player")]
        public JsonElement? Player { get; init; }

        /// <summary>The number of charted matches the sums cover.</summary>
        [JsonPropertyName("matches_charted")]
        public int? MatchesCharted { get; init; }

        /// <summary>A human-readable coverage note.</summary>
        [JsonPropertyName("coverage")]
        public string? Coverage { get; init; }

        /// <summary>
        /// Per-family summed numeric columns (raw JSON) — family names key
        /// objects of summed counters.
        /// </summary>
        [JsonPropertyName("families")]
        public JsonElement? Families { get; init; }
    }

    /// <summary>
    /// One charted match, every Match Charting Project stat family for both
    /// players, with the per-set split (row/set 1, 2, Total) exactly as charted.
    /// <b>ULTRA only.</b> <see cref="ChartingMatchId"/> is this product's own id
    /// space (1960–2026, mostly matches with no counterpart in the live table).
    /// </summary>
    public sealed record ChartingMatch : LiveTennisModel
    {
        /// <summary>This product's own charted-match id.</summary>
        [JsonPropertyName("charting_match_id")]
        public int? ChartingMatchId { get; init; }

        /// <summary>The Match Charting Project's own id.</summary>
        [JsonPropertyName("mcp_id")]
        public string? McpId { get; init; }

        /// <summary>Gender: <c>M</c> or <c>W</c>.</summary>
        [JsonPropertyName("gender")]
        public string? Gender { get; init; }

        /// <summary>The two players (raw JSON).</summary>
        [JsonPropertyName("players")]
        public JsonElement? Players { get; init; }

        /// <summary>Every stat family for both players (raw JSON).</summary>
        [JsonPropertyName("families")]
        public JsonElement? Families { get; init; }
    }
}
