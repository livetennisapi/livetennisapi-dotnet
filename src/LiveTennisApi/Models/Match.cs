using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>The two sides of a match.</summary>
    public sealed record MatchPlayers : LiveTennisModel
    {
        /// <summary>Player (or doubles team) 1.</summary>
        [JsonPropertyName("p1")]
        public Player? P1 { get; init; }

        /// <summary>Player (or doubles team) 2.</summary>
        [JsonPropertyName("p2")]
        public Player? P2 { get; init; }
    }

    /// <summary>A tennis match with its latest score.</summary>
    /// <remarks>
    /// <see cref="Score"/> is <b>nullable</b> — an upcoming match has no score yet.
    /// <see cref="Market"/> is present from PRO, <see cref="Analysis"/> from ULTRA;
    /// both are <b>absent</b> (deserialize to <c>null</c>) below those tiers, so
    /// treat <c>null</c> as "not entitled / not available", never as "no market
    /// exists".
    /// </remarks>
    public sealed record Match : LiveTennisModel
    {
        /// <summary>Stable match id.</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>Tournament name.</summary>
        [JsonPropertyName("tournament")]
        public string? Tournament { get; init; }

        /// <summary>Surface: <c>hard</c>, <c>clay</c>, <c>grass</c>, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Whether the match is indoors.</summary>
        [JsonPropertyName("indoor")]
        public bool? Indoor { get; init; }

        /// <summary>Match format: <c>BO3</c>, <c>BO5</c>, or <c>null</c>.</summary>
        [JsonPropertyName("format")]
        public string? Format { get; init; }

        /// <summary>Round, or <c>null</c>.</summary>
        [JsonPropertyName("round")]
        public string? Round { get; init; }

        /// <summary>Lifecycle status: <c>upcoming</c>, <c>live</c>, <c>completed</c>, or <c>cancelled</c>.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        /// <summary>A finer-grained status string, or <c>null</c>.</summary>
        [JsonPropertyName("event_status")]
        public string? EventStatus { get; init; }

        /// <summary>Whether this is a doubles match.</summary>
        [JsonPropertyName("is_doubles")]
        public bool? IsDoubles { get; init; }

        /// <summary>Scheduled start as an ISO 8601 UTC string, or <c>null</c>.</summary>
        [JsonPropertyName("scheduled_time")]
        public string? ScheduledTime { get; init; }

        /// <summary>The two sides of the match.</summary>
        [JsonPropertyName("players")]
        public MatchPlayers? Players { get; init; }

        /// <summary>The latest score, or <c>null</c> for an upcoming match.</summary>
        [JsonPropertyName("score")]
        public Score? Score { get; init; }

        /// <summary>Winner (<c>1</c> or <c>2</c>) on completed matches, else <c>null</c>. May also be <c>null</c> on a completed match with no derivable winner.</summary>
        [JsonPropertyName("winner")]
        public int? Winner { get; init; }

        /// <summary>Embedded market. <b>PRO+ only</b>; <c>null</c> below that tier.</summary>
        [JsonPropertyName("market")]
        public Market? Market { get; init; }

        /// <summary>Embedded analysis. <b>ULTRA only</b>; <c>null</c> below that tier.</summary>
        [JsonPropertyName("analysis")]
        public Analysis? Analysis { get; init; }
    }
}
