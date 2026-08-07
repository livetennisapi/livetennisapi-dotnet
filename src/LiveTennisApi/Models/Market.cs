using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>One price tick for a market side.</summary>
    public sealed record Price : LiveTennisModel
    {
        /// <summary>Which outcome: <c>1</c> = player 1's, <c>2</c> = player 2's, or <c>null</c>.</summary>
        [JsonPropertyName("side")]
        public int? Side { get; init; }

        /// <summary>Best bid, or <c>null</c>.</summary>
        [JsonPropertyName("bid")]
        public double? Bid { get; init; }

        /// <summary>Best ask, or <c>null</c>.</summary>
        [JsonPropertyName("ask")]
        public double? Ask { get; init; }

        /// <summary>Mid price, or <c>null</c>.</summary>
        [JsonPropertyName("mid")]
        public double? Mid { get; init; }

        /// <summary>Bid/ask spread, or <c>null</c>.</summary>
        [JsonPropertyName("spread")]
        public double? Spread { get; init; }

        /// <summary>Feed category, e.g. <c>prediction_market</c>, or <c>null</c>.</summary>
        [JsonPropertyName("price_source")]
        public string? PriceSource { get; init; }

        /// <summary>
        /// <c>true</c> = bid/ask were estimated from the mid (not a live order
        /// book); <c>false</c> = real top-of-book; <c>null</c> = unknown (older
        /// ticks). Tagged so a synthesised quote is never mistaken for a live
        /// book.
        /// </summary>
        [JsonPropertyName("synthetic")]
        public bool? Synthetic { get; init; }

        /// <summary>Tick time as an ISO 8601 UTC string, or <c>null</c>.</summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; init; }
    }

    /// <summary>A match-winner market. <b>PRO tier and above.</b></summary>
    public sealed record Market : LiveTennisModel
    {
        /// <summary>Market id.</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>The market question, or <c>null</c>.</summary>
        [JsonPropertyName("question")]
        public string? Question { get; init; }

        /// <summary>Market status: <c>active</c>, <c>resolved</c>, <c>closed</c>, or <c>null</c>.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        /// <summary>Traded volume, or <c>null</c>.</summary>
        [JsonPropertyName("volume")]
        public double? Volume { get; init; }

        /// <summary>Available liquidity, or <c>null</c>.</summary>
        [JsonPropertyName("liquidity")]
        public double? Liquidity { get; init; }

        /// <summary>Market end time as an ISO 8601 UTC string, or <c>null</c>.</summary>
        [JsonPropertyName("end_date")]
        public string? EndDate { get; init; }

        /// <summary>
        /// Recent price ticks per side, newest first. Populated by the prices
        /// endpoint and the match-detail embed; otherwise empty.
        /// </summary>
        [JsonPropertyName("prices")]
        public IReadOnlyList<Price>? Prices { get; init; }
    }
}
