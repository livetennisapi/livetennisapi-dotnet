using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>A match score at a point in time.</summary>
    /// <remarks>
    /// <see cref="Sets"/> is <c>[sets_p1, sets_p2]</c>.
    /// <para>
    /// <see cref="Games"/> is <c>[games_p1, games_p2]</c> where <b>each side is a
    /// per-set list</b> — so <c>[[6,3,2],[4,6,1]]</c> reads 6-4, 3-6, 2-1. It is
    /// player-major, not set-major; indexing it the other way is the single most
    /// common mistake against this API. Use <see cref="GamesForSet(int)"/> rather
    /// than indexing by hand. The sub-arrays grow by one entry per set played.
    /// </para>
    /// <para>
    /// <see cref="Points"/> are <b>strings</b> (<c>"0"</c>, <c>"15"</c>,
    /// <c>"30"</c>, <c>"40"</c>, <c>"AD"</c>), not integers.
    /// </para>
    /// <para>
    /// <see cref="Server"/> is nullable: it is <c>null</c> between points and on a
    /// completed match. <see cref="WinProbabilityP1"/> and <see cref="Danger"/>
    /// are present only on the ULTRA tier.
    /// </para>
    /// </remarks>
    public sealed record Score : LiveTennisModel
    {
        /// <summary>Sets won per player: <c>[sets_p1, sets_p2]</c>.</summary>
        [JsonPropertyName("sets")]
        public IReadOnlyList<int>? Sets { get; init; }

        /// <summary>
        /// Games per player, player-major: <c>[games_p1, games_p2]</c>, each a
        /// per-set list. See the remarks on <see cref="Score"/>.
        /// </summary>
        [JsonPropertyName("games")]
        public IReadOnlyList<IReadOnlyList<int>>? Games { get; init; }

        /// <summary>Current-game points as strings, e.g. <c>["30","15"]</c>.</summary>
        [JsonPropertyName("points")]
        public IReadOnlyList<string>? Points { get; init; }

        /// <summary>
        /// Which player is serving (<c>1</c> or <c>2</c>), or <c>null</c> between
        /// points and on a finished match.
        /// </summary>
        [JsonPropertyName("server")]
        public int? Server { get; init; }

        /// <summary>Whether the current game is a tiebreak.</summary>
        [JsonPropertyName("is_tiebreak")]
        public bool? IsTiebreak { get; init; }

        /// <summary>Model win probability for player 1. <b>ULTRA only</b>; otherwise <c>null</c>.</summary>
        [JsonPropertyName("win_probability_p1")]
        public double? WinProbabilityP1 { get; init; }

        /// <summary>Model "danger" signal. <b>ULTRA only</b>; otherwise <c>null</c>.</summary>
        [JsonPropertyName("danger")]
        public double? Danger { get; init; }

        /// <summary>
        /// When this score was observed, as an ISO 8601 UTC string (<c>Z</c>
        /// suffix), or <c>null</c>. Kept as a string to pass the server's exact
        /// value through untouched.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; init; }

        /// <summary>
        /// Games for one set as <c>(p1, p2)</c>, guarding the player-major layout.
        /// Returns <c>(null, null)</c> when the data is absent or the index is out
        /// of range.
        /// </summary>
        /// <param name="setIndex">Zero-based set index.</param>
        /// <returns>The games won by each player in that set.</returns>
        public (int? P1, int? P2) GamesForSet(int setIndex)
        {
            if (Games is null || Games.Count < 2)
            {
                return (null, null);
            }

            var p1 = Games[0];
            var p2 = Games[1];
            int? g1 = p1 != null && setIndex >= 0 && setIndex < p1.Count ? p1[setIndex] : (int?)null;
            int? g2 = p2 != null && setIndex >= 0 && setIndex < p2.Count ? p2[setIndex] : (int?)null;
            return (g1, g2);
        }
    }
}
