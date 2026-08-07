using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// Measured counting statistics for one player. These are <b>counted
    /// upstream</b>, not derived from the point record — which is why they can
    /// include aces and double faults, and the derived fields cannot.
    /// </summary>
    /// <remarks>
    /// Every field is optional and an absent field is <b>omitted, never
    /// zero-filled</b> — read the keys you are given (an absent key deserializes
    /// to <c>null</c>). Coverage falls into three tiers: aces/double faults and
    /// the core points/games counters are present on every tour; the
    /// first/second-serve split and break points saved are present on the main
    /// tours and absent on ITF singles; winners and unforced errors appear on a
    /// minority of main-tour matches. A <c>_of</c>-suffixed property is the
    /// denominator of its base field and a <c>_pct</c> one is the percentage,
    /// recomputed from the two counts rather than read from upstream rounding.
    /// </remarks>
    public sealed record MatchStatisticsMeasured : LiveTennisModel
    {
        /// <summary>Aces.</summary>
        [JsonPropertyName("aces")]
        public int? Aces { get; init; }

        /// <summary>Double faults.</summary>
        [JsonPropertyName("double_faults")]
        public int? DoubleFaults { get; init; }

        /// <summary>Points won.</summary>
        [JsonPropertyName("points_won")]
        public int? PointsWon { get; init; }

        /// <summary>Break points won.</summary>
        [JsonPropertyName("break_points_won")]
        public int? BreakPointsWon { get; init; }

        /// <summary>Service points won.</summary>
        [JsonPropertyName("service_points_won")]
        public int? ServicePointsWon { get; init; }

        /// <summary>Return points won.</summary>
        [JsonPropertyName("return_points_won")]
        public int? ReturnPointsWon { get; init; }

        /// <summary>Service games won.</summary>
        [JsonPropertyName("service_games_won")]
        public int? ServiceGamesWon { get; init; }

        /// <summary>Longest run of consecutive games.</summary>
        [JsonPropertyName("max_games_in_row")]
        public int? MaxGamesInRow { get; init; }

        /// <summary>Longest run of consecutive points.</summary>
        [JsonPropertyName("max_points_in_row")]
        public int? MaxPointsInRow { get; init; }

        /// <summary>First-serve return points won.</summary>
        [JsonPropertyName("first_return_points_won")]
        public int? FirstReturnPointsWon { get; init; }

        /// <summary>First-serve return points played (denominator).</summary>
        [JsonPropertyName("first_return_points_won_of")]
        public int? FirstReturnPointsWonOf { get; init; }

        /// <summary>First-serve return points won, percent.</summary>
        [JsonPropertyName("first_return_points_won_pct")]
        public int? FirstReturnPointsWonPct { get; init; }

        /// <summary>Second-serve return points won.</summary>
        [JsonPropertyName("second_return_points_won")]
        public int? SecondReturnPointsWon { get; init; }

        /// <summary>Second-serve return points played (denominator).</summary>
        [JsonPropertyName("second_return_points_won_of")]
        public int? SecondReturnPointsWonOf { get; init; }

        /// <summary>Second-serve return points won, percent.</summary>
        [JsonPropertyName("second_return_points_won_pct")]
        public int? SecondReturnPointsWonPct { get; init; }

        /// <summary>Break points saved.</summary>
        [JsonPropertyName("break_points_saved")]
        public int? BreakPointsSaved { get; init; }

        /// <summary>Break points faced (denominator).</summary>
        [JsonPropertyName("break_points_saved_of")]
        public int? BreakPointsSavedOf { get; init; }

        /// <summary>Break points saved, percent.</summary>
        [JsonPropertyName("break_points_saved_pct")]
        public int? BreakPointsSavedPct { get; init; }

        /// <summary>First-serve points won.</summary>
        [JsonPropertyName("first_serve_points_won")]
        public int? FirstServePointsWon { get; init; }

        /// <summary>First-serve points played (denominator).</summary>
        [JsonPropertyName("first_serve_points_won_of")]
        public int? FirstServePointsWonOf { get; init; }

        /// <summary>First-serve points won, percent.</summary>
        [JsonPropertyName("first_serve_points_won_pct")]
        public int? FirstServePointsWonPct { get; init; }

        /// <summary>First serves in.</summary>
        [JsonPropertyName("first_serves_in")]
        public int? FirstServesIn { get; init; }

        /// <summary>First serves struck (denominator).</summary>
        [JsonPropertyName("first_serves_in_of")]
        public int? FirstServesInOf { get; init; }

        /// <summary>First serves in, percent.</summary>
        [JsonPropertyName("first_serves_in_pct")]
        public int? FirstServesInPct { get; init; }

        /// <summary>Second-serve points won.</summary>
        [JsonPropertyName("second_serve_points_won")]
        public int? SecondServePointsWon { get; init; }

        /// <summary>Second-serve points played (denominator).</summary>
        [JsonPropertyName("second_serve_points_won_of")]
        public int? SecondServePointsWonOf { get; init; }

        /// <summary>Second-serve points won, percent.</summary>
        [JsonPropertyName("second_serve_points_won_pct")]
        public int? SecondServePointsWonPct { get; init; }

        /// <summary>Second serves in.</summary>
        [JsonPropertyName("second_serves_in")]
        public int? SecondServesIn { get; init; }

        /// <summary>Second serves struck (denominator).</summary>
        [JsonPropertyName("second_serves_in_of")]
        public int? SecondServesInOf { get; init; }

        /// <summary>Second serves in, percent.</summary>
        [JsonPropertyName("second_serves_in_pct")]
        public int? SecondServesInPct { get; init; }

        /// <summary>Games won.</summary>
        [JsonPropertyName("games_won")]
        public int? GamesWon { get; init; }

        /// <summary>Service games played.</summary>
        [JsonPropertyName("service_games_played")]
        public int? ServiceGamesPlayed { get; init; }

        /// <summary>Tiebreaks won.</summary>
        [JsonPropertyName("tiebreaks_won")]
        public int? TiebreaksWon { get; init; }

        /// <summary>Winners, total.</summary>
        [JsonPropertyName("winners_total")]
        public int? WinnersTotal { get; init; }

        /// <summary>Errors, total.</summary>
        [JsonPropertyName("errors_total")]
        public int? ErrorsTotal { get; init; }

        /// <summary>Unforced errors, total.</summary>
        [JsonPropertyName("unforced_errors_total")]
        public int? UnforcedErrorsTotal { get; init; }

        /// <summary>Forehand winners.</summary>
        [JsonPropertyName("forehand_winners")]
        public int? ForehandWinners { get; init; }

        /// <summary>Forehand errors.</summary>
        [JsonPropertyName("forehand_errors")]
        public int? ForehandErrors { get; init; }

        /// <summary>Forehand unforced errors.</summary>
        [JsonPropertyName("forehand_unforced_errors")]
        public int? ForehandUnforcedErrors { get; init; }

        /// <summary>Backhand winners.</summary>
        [JsonPropertyName("backhand_winners")]
        public int? BackhandWinners { get; init; }

        /// <summary>Backhand errors.</summary>
        [JsonPropertyName("backhand_errors")]
        public int? BackhandErrors { get; init; }

        /// <summary>Backhand unforced errors.</summary>
        [JsonPropertyName("backhand_unforced_errors")]
        public int? BackhandUnforcedErrors { get; init; }

        /// <summary>Groundstroke winners.</summary>
        [JsonPropertyName("groundstroke_winners")]
        public int? GroundstrokeWinners { get; init; }

        /// <summary>Groundstroke errors.</summary>
        [JsonPropertyName("groundstroke_errors")]
        public int? GroundstrokeErrors { get; init; }

        /// <summary>Groundstroke unforced errors.</summary>
        [JsonPropertyName("groundstroke_unforced_errors")]
        public int? GroundstrokeUnforcedErrors { get; init; }

        /// <summary>Volley winners.</summary>
        [JsonPropertyName("volley_winners")]
        public int? VolleyWinners { get; init; }

        /// <summary>Volley unforced errors.</summary>
        [JsonPropertyName("volley_unforced_errors")]
        public int? VolleyUnforcedErrors { get; init; }

        /// <summary>Overhead winners.</summary>
        [JsonPropertyName("overhead_winners")]
        public int? OverheadWinners { get; init; }

        /// <summary>Overhead errors.</summary>
        [JsonPropertyName("overhead_errors")]
        public int? OverheadErrors { get; init; }

        /// <summary>Drop-shot winners.</summary>
        [JsonPropertyName("drop_shot_winners")]
        public int? DropShotWinners { get; init; }

        /// <summary>Drop-shot unforced errors.</summary>
        [JsonPropertyName("drop_shot_unforced_errors")]
        public int? DropShotUnforcedErrors { get; init; }

        /// <summary>Lob winners.</summary>
        [JsonPropertyName("lob_winners")]
        public int? LobWinners { get; init; }

        /// <summary>Lob unforced errors.</summary>
        [JsonPropertyName("lob_unforced_errors")]
        public int? LobUnforcedErrors { get; init; }

        /// <summary>Return winners.</summary>
        [JsonPropertyName("return_winners")]
        public int? ReturnWinners { get; init; }

        /// <summary>Return errors.</summary>
        [JsonPropertyName("return_errors")]
        public int? ReturnErrors { get; init; }
    }

    /// <summary>
    /// One player's in-play statistics, in <b>two families that are deliberately
    /// not merged</b>. The fields at this level are <b>derived</b> — rebuilt from
    /// the point-by-point record. <see cref="Measured"/> holds counts taken
    /// upstream, including the ones no point record can yield: aces, double
    /// faults, the serve split, winners and unforced errors. Both families name
    /// some of the same quantities, computed two entirely different ways — that
    /// is a cross-check, not a duplication to collapse.
    /// </summary>
    public sealed record MatchStatisticsSide : LiveTennisModel
    {
        /// <summary>The measured family, counted upstream.</summary>
        [JsonPropertyName("measured")]
        public MatchStatisticsMeasured? Measured { get; init; }

        /// <summary>Service games played (derived).</summary>
        [JsonPropertyName("service_games_played")]
        public int? ServiceGamesPlayed { get; init; }

        /// <summary>Service games won (derived).</summary>
        [JsonPropertyName("service_games_won")]
        public int? ServiceGamesWon { get; init; }

        /// <summary>
        /// Hold percentage. <c>null</c> when no service game was played — never
        /// <c>0</c>, so a present <c>0</c> is a real measured zero.
        /// </summary>
        [JsonPropertyName("hold_pct")]
        public int? HoldPct { get; init; }

        /// <summary>Return games played (derived).</summary>
        [JsonPropertyName("return_games_played")]
        public int? ReturnGamesPlayed { get; init; }

        /// <summary>Return games won (derived).</summary>
        [JsonPropertyName("return_games_won")]
        public int? ReturnGamesWon { get; init; }

        /// <summary>Break percentage, or <c>null</c> when no return game was played.</summary>
        [JsonPropertyName("break_pct")]
        public int? BreakPct { get; init; }

        /// <summary>Break points faced (derived).</summary>
        [JsonPropertyName("break_points_faced")]
        public int? BreakPointsFaced { get; init; }

        /// <summary>Break points saved (derived).</summary>
        [JsonPropertyName("break_points_saved")]
        public int? BreakPointsSaved { get; init; }

        /// <summary>Break points saved, percent, or <c>null</c>.</summary>
        [JsonPropertyName("break_points_saved_pct")]
        public int? BreakPointsSavedPct { get; init; }

        /// <summary>Break points played on return (derived).</summary>
        [JsonPropertyName("break_points_played")]
        public int? BreakPointsPlayed { get; init; }

        /// <summary>Break points converted (derived).</summary>
        [JsonPropertyName("break_points_converted")]
        public int? BreakPointsConverted { get; init; }

        /// <summary>Break points converted, percent, or <c>null</c>.</summary>
        [JsonPropertyName("break_points_converted_pct")]
        public int? BreakPointsConvertedPct { get; init; }

        /// <summary>Service points played (derived).</summary>
        [JsonPropertyName("service_points_played")]
        public int? ServicePointsPlayed { get; init; }

        /// <summary>Service points won (derived).</summary>
        [JsonPropertyName("service_points_won")]
        public int? ServicePointsWon { get; init; }

        /// <summary>Service points won, percent, or <c>null</c>.</summary>
        [JsonPropertyName("service_points_won_pct")]
        public int? ServicePointsWonPct { get; init; }

        /// <summary>Return points played (derived).</summary>
        [JsonPropertyName("return_points_played")]
        public int? ReturnPointsPlayed { get; init; }

        /// <summary>Return points won (derived).</summary>
        [JsonPropertyName("return_points_won")]
        public int? ReturnPointsWon { get; init; }

        /// <summary>Return points won, percent, or <c>null</c>.</summary>
        [JsonPropertyName("return_points_won_pct")]
        public int? ReturnPointsWonPct { get; init; }

        /// <summary>Points played (derived).</summary>
        [JsonPropertyName("points_played")]
        public int? PointsPlayed { get; init; }

        /// <summary>Points won (derived).</summary>
        [JsonPropertyName("points_won")]
        public int? PointsWon { get; init; }
    }

    /// <summary>The match state a statistics family describes, per upstream.</summary>
    public sealed record MatchStatisticsDescribes : LiveTennisModel
    {
        /// <summary>Games per set for player 1.</summary>
        [JsonPropertyName("games_p1")]
        public IReadOnlyList<int>? GamesP1 { get; init; }

        /// <summary>Games per set for player 2.</summary>
        [JsonPropertyName("games_p2")]
        public IReadOnlyList<int>? GamesP2 { get; init; }

        /// <summary>Total games described.</summary>
        [JsonPropertyName("total_games")]
        public int? TotalGames { get; init; }
    }

    /// <summary>Coverage and age for one statistics family.</summary>
    public sealed record MatchStatisticsFamily : LiveTennisModel
    {
        /// <summary>
        /// Family coverage: <c>live</c>, <c>final</c> (the closing figures of a
        /// completed match, age null), <c>stale</c>, <c>none</c>, or
        /// <c>diverged</c>.
        /// </summary>
        [JsonPropertyName("coverage")]
        public string? Coverage { get; init; }

        /// <summary>When the family was last updated (UTC ISO string), or <c>null</c>.</summary>
        [JsonPropertyName("as_of")]
        public string? AsOf { get; init; }

        /// <summary>
        /// Age of the family. The derived family's age is measured against the
        /// newest <b>score row</b>; the measured family's age is wall clock. The
        /// two use different clocks and must not be compared.
        /// </summary>
        [JsonPropertyName("age_seconds")]
        public int? AgeSeconds { get; init; }

        /// <summary>The match state these statistics describe, or <c>null</c> when unavailable.</summary>
        [JsonPropertyName("describes")]
        public MatchStatisticsDescribes? Describes { get; init; }
    }

    /// <summary>Why the measured values were withheld, with both match states.</summary>
    public sealed record MatchStatisticsDivergence : LiveTennisModel
    {
        /// <summary>The machine-readable reason.</summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; init; }

        /// <summary>Games according to the statistics feed.</summary>
        [JsonPropertyName("games_in_statistics")]
        public int? GamesInStatistics { get; init; }

        /// <summary>Games according to the score.</summary>
        [JsonPropertyName("games_in_score")]
        public int? GamesInScore { get; init; }

        /// <summary>Positive = statistics ahead of the score, which staleness cannot cause.</summary>
        [JsonPropertyName("delta_games")]
        public int? DeltaGames { get; init; }

        /// <summary>Human-readable detail.</summary>
        [JsonPropertyName("detail")]
        public string? Detail { get; init; }
    }

    /// <summary>
    /// Per-family coverage and age. Branch on this rather than on the top-level
    /// coverage, which only summarises the response.
    /// </summary>
    public sealed record MatchStatisticsFreshness : LiveTennisModel
    {
        /// <summary>
        /// <c>null</c> when the families agree; otherwise why the measured values
        /// were withheld.
        /// </summary>
        [JsonPropertyName("measured_divergence")]
        public MatchStatisticsDivergence? MeasuredDivergence { get; init; }

        /// <summary>The derived family's coverage and age (score-row clock).</summary>
        [JsonPropertyName("derived")]
        public MatchStatisticsFamily? Derived { get; init; }

        /// <summary>The measured family's coverage and age (wall clock).</summary>
        [JsonPropertyName("measured")]
        public MatchStatisticsFamily? Measured { get; init; }
    }

    /// <summary>The two players' statistics.</summary>
    public sealed record MatchStatisticsPlayers : LiveTennisModel
    {
        /// <summary>Player 1's statistics.</summary>
        [JsonPropertyName("p1")]
        public MatchStatisticsSide? P1 { get; init; }

        /// <summary>Player 2's statistics.</summary>
        [JsonPropertyName("p2")]
        public MatchStatisticsSide? P2 { get; init; }
    }

    /// <summary>
    /// In-play statistics for one match — aces, double faults, serve split,
    /// hold/break %, break points, service and return points. <b>ULTRA only.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two families that are deliberately not merged: <b>derived</b> (the top
    /// level of each <see cref="MatchStatisticsSide"/>) rebuilt from the
    /// point-by-point record, and <b>measured</b>
    /// (<see cref="MatchStatisticsSide.Measured"/>) counted upstream. Tiebreak
    /// games are excluded from the derived family and counted in
    /// <see cref="TiebreakGamesExcluded"/>.
    /// </para>
    /// <para>
    /// <c>none</c> coverage on both families returns <c>200</c> with null
    /// <see cref="Players"/>, not <c>404</c> — the match exists and holding
    /// nothing for it is the honest answer.
    /// </para>
    /// </remarks>
    public sealed record MatchStatistics : LiveTennisModel
    {
        /// <summary>The match id.</summary>
        [JsonPropertyName("match_id")]
        public int? MatchId { get; init; }

        /// <summary>
        /// Summary coverage of the response: <c>live</c>, <c>final</c>,
        /// <c>stale</c>, <c>none</c>, or <c>diverged</c>. Branch on
        /// <see cref="Freshness"/> per family rather than on this.
        /// </summary>
        [JsonPropertyName("coverage")]
        public string? Coverage { get; init; }

        /// <summary>When the underlying record was last updated (UTC ISO string), or <c>null</c>.</summary>
        [JsonPropertyName("as_of")]
        public string? AsOf { get; init; }

        /// <summary>Age behind the newest <b>score row</b>, not the wall clock.</summary>
        [JsonPropertyName("age_seconds")]
        public int? AgeSeconds { get; init; }

        /// <summary>Games counted into the derived family.</summary>
        [JsonPropertyName("games_counted")]
        public int? GamesCounted { get; init; }

        /// <summary>
        /// Tiebreak games excluded — the live record collapses a whole tiebreak
        /// onto one entry, so most of its points are lost.
        /// </summary>
        [JsonPropertyName("tiebreak_games_excluded")]
        public int? TiebreakGamesExcluded { get; init; }

        /// <summary>Games whose recorded outcome is neither a legal hold nor a legal break.</summary>
        [JsonPropertyName("inconsistent_games_excluded")]
        public int? InconsistentGamesExcluded { get; init; }

        /// <summary>The sets the derived family covers.</summary>
        [JsonPropertyName("sets_covered")]
        public IReadOnlyList<int>? SetsCovered { get; init; }

        /// <summary>Per-family coverage and age.</summary>
        [JsonPropertyName("freshness")]
        public MatchStatisticsFreshness? Freshness { get; init; }

        /// <summary>Present only when coverage is <c>none</c>.</summary>
        [JsonPropertyName("detail")]
        public string? Detail { get; init; }

        /// <summary>The two players' statistics, or <c>null</c> when nothing is held.</summary>
        [JsonPropertyName("players")]
        public MatchStatisticsPlayers? Players { get; init; }
    }
}
