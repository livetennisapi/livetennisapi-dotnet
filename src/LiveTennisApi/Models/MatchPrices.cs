using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>The envelope of <c>/matches/{id}/prices</c>.</summary>
    public sealed record MatchPricesMeta : LiveTennisModel
    {
        /// <summary>The match id the ticks belong to.</summary>
        [JsonPropertyName("match_id")]
        public int? MatchId { get; init; }

        /// <summary>Ticks on this page.</summary>
        [JsonPropertyName("count")]
        public int? Count { get; init; }

        /// <summary>
        /// The window was clipped at <see cref="Limit"/> — older ticks exist.
        /// There is <b>no offset</b> on this endpoint; raise the limit or narrow
        /// the minutes window instead.
        /// </summary>
        [JsonPropertyName("has_more")]
        public bool? HasMore { get; init; }

        /// <summary>The limit that was applied (max 500).</summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; init; }

        /// <summary>The lookback window in minutes, or <c>null</c> when unbounded.</summary>
        [JsonPropertyName("minutes")]
        public int? Minutes { get; init; }
    }

    /// <summary>
    /// Bare price ticks of a match's mapped match-winner market, newest first
    /// (no market wrapper). <b>PRO tier and above.</b>
    /// </summary>
    public sealed record MatchPrices : LiveTennisModel
    {
        /// <summary>The ticks, newest first. Never <c>null</c> (empty when absent).</summary>
        [JsonPropertyName("data")]
        public IReadOnlyList<Price> Data { get; init; } = new List<Price>();

        /// <summary>The envelope, if the server sent one.</summary>
        [JsonPropertyName("meta")]
        public MatchPricesMeta? Meta { get; init; }
    }
}
