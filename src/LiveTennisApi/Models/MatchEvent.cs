using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>A match event. <b>PRO tier and above.</b></summary>
    public sealed record MatchEvent : LiveTennisModel
    {
        /// <summary>Event type: <c>break</c>, <c>set_won</c>, <c>game_won</c>, or <c>momentum_run</c>.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        /// <summary>The player the event relates to (<c>1</c> or <c>2</c>), or <c>null</c>.</summary>
        [JsonPropertyName("player")]
        public int? Player { get; init; }

        /// <summary>When the event occurred, ISO 8601 UTC string, or <c>null</c>.</summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; init; }
    }
}
