using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// One tournament of the catalogue — the id space
    /// <see cref="Match.TournamentId"/> joins. One row per tournament × event
    /// type, stable across seasons.
    /// </summary>
    /// <remarks>
    /// <see cref="Category"/> is populated only where the catalogues agree
    /// unambiguously on an exact-name join — <c>null</c> otherwise, never
    /// derived from the tournament name.
    /// </remarks>
    public sealed record Tournament : LiveTennisModel
    {
        /// <summary>The stable id that <see cref="Match.TournamentId"/> joins.</summary>
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        /// <summary>Tournament name, or <c>null</c>.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        /// <summary>
        /// Tour in the filter vocabulary (<c>atp</c>, <c>wta</c>,
        /// <c>challenger</c>, <c>itf</c>, <c>juniors</c>), or <c>null</c>.
        /// </summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>Surface: <c>hard</c>, <c>clay</c>, <c>grass</c>, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Whether the tournament is played indoors.</summary>
        [JsonPropertyName("indoor")]
        public bool? Indoor { get; init; }

        /// <summary>Host city, from a curated table — <c>null</c> where not curated.</summary>
        [JsonPropertyName("city")]
        public string? City { get; init; }

        /// <summary>
        /// Host country as ISO 3166 alpha-2 (e.g. <c>NL</c>) — a <b>different
        /// vocabulary</b> from <see cref="Player.Country"/>'s IOC-style 3-letter
        /// codes. <c>null</c> where not curated.
        /// </summary>
        [JsonPropertyName("country")]
        public string? Country { get; init; }

        /// <summary>
        /// Tournament category (<c>grand_slam</c>, <c>masters_1000</c>,
        /// <c>tour_finals</c>, <c>atp_500</c>, <c>atp_250</c>, <c>wta_1000</c>,
        /// <c>wta_500</c>, <c>wta_250</c>, <c>wta_125</c>, <c>challenger</c>,
        /// <c>itf</c>, <c>juniors</c>), or <c>null</c> where the catalogues do
        /// not agree — never guessed from the name.
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; init; }
    }
}
