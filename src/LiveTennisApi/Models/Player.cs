using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// How much biographical detail is known for a player, so a consumer can tell
    /// "not in the feed" from "not yet fetched" without probing.
    /// </summary>
    /// <remarks>
    /// <see cref="Known"/> and <see cref="Of"/> are <b>nullable</b>: on a doubles
    /// team they are <c>null</c> (per-player biography does not apply), which is
    /// distinct from <c>0</c> (the fields apply and none are known). When the
    /// object is not applicable, <see cref="Note"/> explains why.
    /// </remarks>
    public sealed record DataCompleteness : LiveTennisModel
    {
        /// <summary>Fields populated, of <see cref="Of"/>. <c>null</c> on a doubles team.</summary>
        [JsonPropertyName("known")]
        public int? Known { get; init; }

        /// <summary>Fields considered. <c>null</c> on a doubles team.</summary>
        [JsonPropertyName("of")]
        public int? Of { get; init; }

        /// <summary>Names of the unpopulated fields, e.g. <c>["backhand","hand"]</c>.</summary>
        [JsonPropertyName("missing")]
        public IReadOnlyList<string>? Missing { get; init; }

        /// <summary>Present only when the object is not applicable (e.g. a doubles team).</summary>
        [JsonPropertyName("note")]
        public string? Note { get; init; }
    }

    /// <summary>Cached statistics, populated by the single-player endpoint only.</summary>
    public sealed record PlayerStats : LiveTennisModel
    {
        /// <summary>Rating figures, if present. Shape is tier- and player-dependent.</summary>
        [JsonPropertyName("ratings")]
        public JsonElement? Ratings { get; init; }

        /// <summary>Per-season figures, if present.</summary>
        [JsonPropertyName("season")]
        public JsonElement? Season { get; init; }
    }

    /// <summary>A player, or — when <see cref="IsDoublesTeam"/> is <c>true</c> — a doubles pairing.</summary>
    public sealed record Player : LiveTennisModel
    {
        /// <summary>Stable player (or team) id.</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>Display name. For a doubles team, both members joined by <c>/</c>.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        /// <summary>
        /// The record's <b>own</b> tour, which is <b>not</b> the <see cref="LiveTennisApi.Tour"/>
        /// filter vocabulary. It is granular (<c>juniors_boys</c>,
        /// <c>challenger_men</c>) where the filter is grouped, and a doubles team
        /// reports it UPPERCASE (<c>ATP</c>) where an individual reports lowercase
        /// (<c>atp</c>). Treat it as an opaque string; do not parse it into the
        /// filter enum.
        /// </summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>ISO country code, or <c>null</c>.</summary>
        [JsonPropertyName("country")]
        public string? Country { get; init; }

        /// <summary>Current ranking, or <c>null</c>.</summary>
        [JsonPropertyName("ranking")]
        public int? Ranking { get; init; }

        /// <summary>Current ranking points, or <c>null</c>.</summary>
        [JsonPropertyName("ranking_points")]
        public int? RankingPoints { get; init; }

        /// <summary>Recent ranking movement: <c>up</c>, <c>down</c>, <c>same</c>, or <c>null</c>.</summary>
        [JsonPropertyName("ranking_movement")]
        public string? RankingMovement { get; init; }

        /// <summary>Playing hand: <c>R</c>, <c>L</c>, or <c>null</c>.</summary>
        [JsonPropertyName("hand")]
        public string? Hand { get; init; }

        /// <summary>Backhand style (<c>1</c> or <c>2</c>), or <c>null</c>.</summary>
        [JsonPropertyName("backhand")]
        public int? Backhand { get; init; }

        /// <summary>Birth date as an ISO 8601 date string (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("birthday")]
        public string? Birthday { get; init; }

        /// <summary>Whether this record is a doubles team rather than an individual.</summary>
        [JsonPropertyName("is_doubles_team")]
        public bool? IsDoublesTeam { get; init; }

        /// <summary>Biographical completeness, present on players inside a match payload.</summary>
        [JsonPropertyName("data_completeness")]
        public DataCompleteness? DataCompleteness { get; init; }

        /// <summary>Cached stats. Populated by the single-player endpoint only.</summary>
        [JsonPropertyName("stats")]
        public PlayerStats? Stats { get; init; }
    }
}
