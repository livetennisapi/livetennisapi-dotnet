using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>The calling key's quota limits.</summary>
    public sealed record UsageLimits : LiveTennisModel
    {
        /// <summary>Requests per minute, or <c>null</c> when unlimited/unknown.</summary>
        [JsonPropertyName("per_minute")]
        public int? PerMinute { get; init; }

        /// <summary>Requests per day, or <c>null</c> when the channel is daily-cap exempt.</summary>
        [JsonPropertyName("per_day")]
        public int? PerDay { get; init; }
    }

    /// <summary>Today's usage for the calling key, current to the second.</summary>
    public sealed record UsageToday : LiveTennisModel
    {
        /// <summary>Calls made today.</summary>
        [JsonPropertyName("calls")]
        public int? Calls { get; init; }

        /// <summary>Errors today.</summary>
        [JsonPropertyName("errors")]
        public int? Errors { get; init; }

        /// <summary>Calls remaining in the daily window, or <c>null</c> when no daily cap applies.</summary>
        [JsonPropertyName("remaining_day")]
        public int? RemainingDay { get; init; }
    }

    /// <summary>One day of usage history.</summary>
    public sealed record UsageDay : LiveTennisModel
    {
        /// <summary>The day (<c>YYYY-MM-DD</c>).</summary>
        [JsonPropertyName("day")]
        public string? Day { get; init; }

        /// <summary>Calls that day.</summary>
        [JsonPropertyName("calls")]
        public int? Calls { get; init; }

        /// <summary>Errors that day.</summary>
        [JsonPropertyName("errors")]
        public int? Errors { get; init; }
    }

    /// <summary>
    /// Your own usage vs quota (any tier; the call itself is quota-exempt).
    /// </summary>
    /// <remarks>
    /// Durable daily usage for the calling key: tier, limits, today's calls
    /// (current to the second) and a 30-day history. The <b>per-minute</b>
    /// window lives on the <c>X-RateLimit-*</c> headers of every response, not
    /// here — and the daily reset instant is only carried on the daily-429 body
    /// (<see cref="LiveTennisApi.RateLimitedException.ResetsAt"/>), not on this
    /// object.
    /// </remarks>
    public sealed record Usage : LiveTennisModel
    {
        /// <summary>Opaque reference to your own key.</summary>
        [JsonPropertyName("principal")]
        public string? Principal { get; init; }

        /// <summary>Effective tier: <c>free</c>, <c>basic</c>, <c>pro</c>, or <c>ultra</c>.</summary>
        [JsonPropertyName("tier")]
        public string? Tier { get; init; }

        /// <summary>Subscription tier; equals <see cref="Tier"/> unless a temporary grant is active.</summary>
        [JsonPropertyName("base_tier")]
        public string? BaseTier { get; init; }

        /// <summary>When a temporary tier grant reverts (UTC ISO string), else <c>null</c>.</summary>
        [JsonPropertyName("tier_expires_at")]
        public string? TierExpiresAt { get; init; }

        /// <summary>The key's channel (e.g. direct vs marketplace).</summary>
        [JsonPropertyName("channel")]
        public string? Channel { get; init; }

        /// <summary>The quota limits in force.</summary>
        [JsonPropertyName("limits")]
        public UsageLimits? Limits { get; init; }

        /// <summary>Today's usage, current to the second.</summary>
        [JsonPropertyName("today")]
        public UsageToday? Today { get; init; }

        /// <summary>The last 30 days, oldest first.</summary>
        [JsonPropertyName("history")]
        public IReadOnlyList<UsageDay>? History { get; init; }

        /// <summary>When the summary was produced (UTC ISO string).</summary>
        [JsonPropertyName("as_of")]
        public string? AsOf { get; init; }
    }
}
