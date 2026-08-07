using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// One row of the score sequence.
    /// </summary>
    /// <remarks>
    /// Rows we watched live carry a real <see cref="Timestamp"/>. Rows expanded
    /// after the fact from a finished-match point-by-point record carry a null
    /// timestamp <b>and</b> null model fields, because neither a wall clock nor a
    /// model output ever existed for them — nothing is synthesised. A null
    /// timestamp is the reliable row-level marker of a reconstructed row; the
    /// model fields alone are not, since they are stamped best-effort and an
    /// observed row may lack them.
    /// </remarks>
    public sealed record HistoryTapeRow : LiveTennisModel
    {
        /// <summary>Sets won per player: <c>[sets_p1, sets_p2]</c>.</summary>
        [JsonPropertyName("sets")]
        public IReadOnlyList<int>? Sets { get; init; }

        /// <summary>Games per player, player-major (same layout as <see cref="Score.Games"/>).</summary>
        [JsonPropertyName("games")]
        public IReadOnlyList<IReadOnlyList<int>>? Games { get; init; }

        /// <summary>Current-game points as strings, e.g. <c>["30","15"]</c>.</summary>
        [JsonPropertyName("points")]
        public IReadOnlyList<string>? Points { get; init; }

        /// <summary>The server (<c>1</c> or <c>2</c>), or <c>null</c>.</summary>
        [JsonPropertyName("server")]
        public int? Server { get; init; }

        /// <summary>Whether the row is inside a tiebreak.</summary>
        [JsonPropertyName("is_tiebreak")]
        public bool? IsTiebreak { get; init; }

        /// <summary>Model win probability for player 1, or <c>null</c> (no model output existed).</summary>
        [JsonPropertyName("win_probability_p1")]
        public double? WinProbabilityP1 { get; init; }

        /// <summary>Model "danger" signal, or <c>null</c>.</summary>
        [JsonPropertyName("danger")]
        public double? Danger { get; init; }

        /// <summary>
        /// When the row was committed (UTC ISO string). <c>null</c> marks a
        /// reconstructed row.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; init; }

        /// <summary>
        /// Who won the point this row records — present only on
        /// <c>?sequence=clean</c> rows, and only where the transition from the
        /// previous row is a single attributable point; <c>null</c> on gaps, torn
        /// rows and the first row. Never on the raw sequence (raw is deliberately
        /// non-monotonic: consecutive raw rows are corrections, not points).
        /// Derived at read time, never stored or guessed.
        /// </summary>
        [JsonPropertyName("point_winner")]
        public int? PointWinner { get; init; }
    }

    /// <summary>The final score of one set's tiebreak.</summary>
    public sealed record TapeTiebreak : LiveTennisModel
    {
        /// <summary>Tiebreak points for player 1.</summary>
        [JsonPropertyName("p1")]
        public int? P1 { get; init; }

        /// <summary>Tiebreak points for player 2.</summary>
        [JsonPropertyName("p2")]
        public int? P2 { get; init; }
    }

    /// <summary>The coverage meta of a per-match tape.</summary>
    public sealed record HistoryTapeMeta : LiveTennisModel
    {
        /// <summary>The match id.</summary>
        [JsonPropertyName("match_id")]
        public int? MatchId { get; init; }

        /// <summary>Rows <b>returned</b> — after any <c>sequence=clean</c> collapse.</summary>
        [JsonPropertyName("rows")]
        public int? Rows { get; init; }

        /// <summary>
        /// Tape coverage: <c>from_start</c>, <c>partial</c>,
        /// <c>reconstructed</c>, <c>reconstructed_partial</c>, or <c>none</c>.
        /// Check this before backtesting.
        /// </summary>
        [JsonPropertyName("coverage")]
        public string? Coverage { get; init; }

        /// <summary>
        /// Where the rows came from: <c>observed</c>, <c>reconstructed</c>,
        /// <c>mixed</c>, or <c>null</c> on an empty tape. Reported once here and
        /// never per row.
        /// </summary>
        [JsonPropertyName("point_source")]
        public string? PointSource { get; init; }

        /// <summary>Rows <b>before</b> any collapse — equals <see cref="Rows"/> when raw.</summary>
        [JsonPropertyName("raw_rows")]
        public int? RawRows { get; init; }

        /// <summary>Distinct score states in the raw tape.</summary>
        [JsonPropertyName("unique_states")]
        public int? UniqueStates { get; init; }

        /// <summary>Echoes the requested <c>?sequence=</c>: <c>raw</c> or <c>clean</c>.</summary>
        [JsonPropertyName("sequence")]
        public string? Sequence { get; init; }

        /// <summary>
        /// The rows were served from the immutable archive rather than the live
        /// table. Informational — the content contract is identical.
        /// </summary>
        [JsonPropertyName("from_archive")]
        public bool? FromArchive { get; init; }

        /// <summary>When the response was generated (UTC ISO string).</summary>
        [JsonPropertyName("generated_at")]
        public string? GeneratedAt { get; init; }
    }

    /// <summary>
    /// The per-match tape: point-by-point score sequence + per-point model
    /// probabilities. <b>BASIC tier, or any History plan.</b>
    /// </summary>
    /// <remarks>
    /// Works on a <b>live</b> match, not only a completed one — the tape is
    /// assembled from whatever has been committed so far. It is not guaranteed
    /// to cover the whole match: check <see cref="Meta"/>'s coverage and point
    /// source before backtesting.
    /// </remarks>
    public sealed record HistoryTape : LiveTennisModel
    {
        /// <summary>The match header.</summary>
        [JsonPropertyName("match")]
        public Match? Match { get; init; }

        /// <summary>The chronological score sequence.</summary>
        [JsonPropertyName("tape")]
        public IReadOnlyList<HistoryTapeRow>? Tape { get; init; }

        /// <summary>
        /// Per-set tiebreak final scores from <b>observed</b> states only,
        /// aligned to the sets of the final scoreline: an entry for a 7-6 set
        /// whose observed maximum tiebreak state is a valid terminal shape
        /// (max ≥ 7, margin ≥ 2), <c>null</c> per set otherwise — a breaker whose
        /// closing point the feed skipped reads <c>null</c> rather than an
        /// under-report. <c>null</c> when the match has no 7-6 set.
        /// </summary>
        [JsonPropertyName("tiebreaks")]
        public IReadOnlyList<TapeTiebreak?>? Tiebreaks { get; init; }

        /// <summary>Model profiles, oldest first (the Analysis <c>profile</c> shape, raw JSON).</summary>
        [JsonPropertyName("profiles")]
        public IReadOnlyList<JsonElement>? Profiles { get; init; }

        /// <summary>The coverage meta. Read it before backtesting.</summary>
        [JsonPropertyName("meta")]
        public HistoryTapeMeta? Meta { get; init; }
    }
}
