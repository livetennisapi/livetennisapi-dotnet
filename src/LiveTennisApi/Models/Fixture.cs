using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>A scheduled fixture. Players are names only — not yet resolved to ids.</summary>
    /// <remarks>
    /// Note: the <c>/fixtures</c> endpoint currently also returns some already
    /// finished matches (<see cref="Status"/> = <c>finished</c>). This is a known
    /// upstream quirk; the client passes it through unfiltered.
    /// </remarks>
    public sealed record Fixture : LiveTennisModel
    {
        /// <summary>Fixture id.</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>Scheduled date as an ISO 8601 date string (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("event_date")]
        public string? EventDate { get; init; }

        /// <summary>
        /// Scheduled start (UTC), ISO 8601 string. <c>null</c> until the order of
        /// play assigns a time — a date-only fixture is a real state.
        /// </summary>
        [JsonPropertyName("start_time")]
        public string? StartTime { get; init; }

        /// <summary>
        /// Player 1's roster id, when the participant resolved to our roster
        /// (exact-key resolution, never a name match). <c>null</c> otherwise —
        /// names are always present regardless.
        /// </summary>
        [JsonPropertyName("player1_id")]
        public int? Player1Id { get; init; }

        /// <summary>Player 2's roster id, or <c>null</c>. See <see cref="Player1Id"/>.</summary>
        [JsonPropertyName("player2_id")]
        public int? Player2Id { get; init; }

        /// <summary>
        /// The record's own granular tour string (for example <c>juniors_boys</c>),
        /// an opaque value — not the <see cref="LiveTennisApi.Tour"/> filter enum.
        /// </summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>Tournament name, or <c>null</c>.</summary>
        [JsonPropertyName("tournament")]
        public string? Tournament { get; init; }

        /// <summary>Round, or <c>null</c>.</summary>
        [JsonPropertyName("round")]
        public string? Round { get; init; }

        /// <summary>Normalized round (same vocabulary as <see cref="Match.RoundCode"/>), or <c>null</c>.</summary>
        [JsonPropertyName("round_code")]
        public string? RoundCode { get; init; }

        /// <summary>Surface, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Player 1 name, or <c>null</c>.</summary>
        [JsonPropertyName("player1_name")]
        public string? Player1Name { get; init; }

        /// <summary>Player 2 name, or <c>null</c>.</summary>
        [JsonPropertyName("player2_name")]
        public string? Player2Name { get; init; }

        /// <summary>Fixture status, or <c>null</c>. May be <c>finished</c> (see the type remarks).</summary>
        [JsonPropertyName("status")]
        public string? Status { get; init; }
    }
}
