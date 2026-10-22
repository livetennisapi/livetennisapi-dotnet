using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// The response of <c>GET /players/{id}/stats</c>: one player's record over our
    /// own completed singles matches (2023 onward). <see cref="Records"/> is PRO;
    /// <see cref="ServeReturn"/> is ULTRA and <c>null</c> (absent on the wire) below it.
    /// </summary>
    public sealed record PlayerStatsResult : LiveTennisModel
    {
        /// <summary>The player the record is for.</summary>
        [JsonPropertyName("player")]
        public PlayerRef? Player { get; init; }

        /// <summary>The window applied, UTC.</summary>
        [JsonPropertyName("window")]
        public PlayerStatsWindow? Window { get; init; }

        /// <summary>The filters applied.</summary>
        [JsonPropertyName("filters")]
        public PlayerStatsFilters? Filters { get; init; }

        /// <summary>The results-based record. <b>PRO.</b></summary>
        [JsonPropertyName("records")]
        public PlayerRecords? Records { get; init; }

        /// <summary>
        /// Measured serve/return statistics summed over the window. <b>ULTRA</b>;
        /// <c>null</c> below ULTRA, where the server omits the key.
        /// </summary>
        [JsonPropertyName("serve_return")]
        public PlayerServeReturn? ServeReturn { get; init; }

        /// <summary><c>generated_at</c> and a note on what the aggregates cover.</summary>
        [JsonPropertyName("meta")]
        public PlayerStatsMeta? Meta { get; init; }
    }

    /// <summary>A player id and name.</summary>
    public sealed record PlayerRef : LiveTennisModel
    {
        /// <summary>Player id.</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>Player name, or <c>null</c>.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    /// <summary>The window a player record was computed over (ISO 8601 UTC).</summary>
    public sealed record PlayerStatsWindow : LiveTennisModel
    {
        /// <summary>Window start.</summary>
        [JsonPropertyName("from")]
        public string? From { get; init; }

        /// <summary>Window end.</summary>
        [JsonPropertyName("to")]
        public string? To { get; init; }
    }

    /// <summary>The filters a player record was computed with.</summary>
    public sealed record PlayerStatsFilters : LiveTennisModel
    {
        /// <summary>Surface filter, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Level filter, or <c>null</c>.</summary>
        [JsonPropertyName("level")]
        public string? Level { get; init; }

        /// <summary>How many recent matches were listed.</summary>
        [JsonPropertyName("last")]
        public int? Last { get; init; }
    }

    /// <summary>Response metadata of a player record.</summary>
    public sealed record PlayerStatsMeta : LiveTennisModel
    {
        /// <summary>When the body was computed (cached up to 60 seconds).</summary>
        [JsonPropertyName("generated_at")]
        public string? GeneratedAt { get; init; }

        /// <summary>What the aggregates are computed from.</summary>
        [JsonPropertyName("note")]
        public string? Note { get; init; }
    }

    /// <summary>
    /// The results-based record. W/L blocks count matches with a published winner
    /// and an outcome of completed, retired, walkover or default.
    /// </summary>
    public sealed record PlayerRecords : LiveTennisModel
    {
        /// <summary>W/L over the whole window.</summary>
        [JsonPropertyName("overall")]
        public PlayerWinLoss? Overall { get; init; }

        /// <summary>W/L over the last 12 months.</summary>
        [JsonPropertyName("last_12_months")]
        public PlayerWinLoss? Last12Months { get; init; }

        /// <summary>Keyed <c>hard</c>, <c>clay</c>, <c>grass</c>, <c>unknown</c>.</summary>
        [JsonPropertyName("by_surface")]
        public IReadOnlyDictionary<string, PlayerWinLoss>? BySurface { get; init; }

        /// <summary>Keyed <c>indoor</c>, <c>outdoor</c>, <c>unknown</c>.</summary>
        [JsonPropertyName("by_venue")]
        public IReadOnlyDictionary<string, PlayerWinLoss>? ByVenue { get; init; }

        /// <summary>Keyed by the match tour (<c>atp</c>, <c>wta</c>, <c>challenger</c>, <c>itf</c>, <c>juniors</c>) or <c>unknown</c>.</summary>
        [JsonPropertyName("by_level")]
        public IReadOnlyDictionary<string, PlayerWinLoss>? ByLevel { get; init; }

        /// <summary>Keyed by the UTC calendar year of the scheduled start.</summary>
        [JsonPropertyName("by_season")]
        public IReadOnlyDictionary<string, PlayerWinLoss>? BySeason { get; init; }

        /// <summary>The set-based record, over matches with a complete, legal final score.</summary>
        [JsonPropertyName("sets")]
        public PlayerSetRecord? Sets { get; init; }

        /// <summary>Matches this player did not finish (given) and the opponent did not finish (received).</summary>
        [JsonPropertyName("withdrawals")]
        public PlayerWithdrawals? Withdrawals { get; init; }

        /// <summary>Matches in the window left out of every record, by reason.</summary>
        [JsonPropertyName("excluded")]
        public PlayerRecordExclusions? Excluded { get; init; }

        /// <summary>The last N matches in the window, newest first.</summary>
        [JsonPropertyName("recent")]
        public IReadOnlyList<PlayerRecentMatch>? Recent { get; init; }
    }

    /// <summary>A win/loss count. <see cref="WinPct"/> is <c>null</c>, never 0, when <see cref="Matches"/> is 0.</summary>
    public sealed record PlayerWinLoss : LiveTennisModel
    {
        /// <summary>Matches counted.</summary>
        [JsonPropertyName("matches")]
        public int? Matches { get; init; }

        /// <summary>Wins.</summary>
        [JsonPropertyName("wins")]
        public int? Wins { get; init; }

        /// <summary>Losses.</summary>
        [JsonPropertyName("losses")]
        public int? Losses { get; init; }

        /// <summary>Percent with one decimal, or <c>null</c> on an empty sample.</summary>
        [JsonPropertyName("win_pct")]
        public double? WinPct { get; init; }
    }

    /// <summary>A played/won/lost record (tiebreaks, deciding sets).</summary>
    public sealed record PlayerPlayedRecord : LiveTennisModel
    {
        /// <summary>Played.</summary>
        [JsonPropertyName("played")]
        public int? Played { get; init; }

        /// <summary>Won.</summary>
        [JsonPropertyName("won")]
        public int? Won { get; init; }

        /// <summary>Lost.</summary>
        [JsonPropertyName("lost")]
        public int? Lost { get; init; }

        /// <summary>Percent, or <c>null</c> on an empty sample.</summary>
        [JsonPropertyName("win_pct")]
        public double? WinPct { get; init; }
    }

    /// <summary>A match split (after winning or losing the first set).</summary>
    public sealed record PlayerSplitRecord : LiveTennisModel
    {
        /// <summary>Matches in the split.</summary>
        [JsonPropertyName("matches")]
        public int? Matches { get; init; }

        /// <summary>Won.</summary>
        [JsonPropertyName("won")]
        public int? Won { get; init; }

        /// <summary>Lost.</summary>
        [JsonPropertyName("lost")]
        public int? Lost { get; init; }

        /// <summary>Percent, or <c>null</c> on an empty sample.</summary>
        [JsonPropertyName("win_pct")]
        public double? WinPct { get; init; }
    }

    /// <summary>The set-based record and its sample.</summary>
    public sealed record PlayerSetRecord : LiveTennisModel
    {
        /// <summary>Matches counted and those counted out, by reason.</summary>
        [JsonPropertyName("sample")]
        public PlayerSetSample? Sample { get; init; }

        /// <summary>Tiebreaks played, won and lost.</summary>
        [JsonPropertyName("tiebreaks")]
        public PlayerPlayedRecord? Tiebreaks { get; init; }

        /// <summary>Deciding sets played, won and lost.</summary>
        [JsonPropertyName("deciding_sets")]
        public PlayerPlayedRecord? DecidingSets { get; init; }

        /// <summary>Matches after winning the first set.</summary>
        [JsonPropertyName("after_winning_first_set")]
        public PlayerSplitRecord? AfterWinningFirstSet { get; init; }

        /// <summary>Matches after losing the first set.</summary>
        [JsonPropertyName("after_losing_first_set")]
        public PlayerSplitRecord? AfterLosingFirstSet { get; init; }

        /// <summary>Matches in which the player lost a set, and how many they won.</summary>
        [JsonPropertyName("comebacks")]
        public PlayerComebacks? Comebacks { get; init; }

        /// <summary>Sets won and lost.</summary>
        [JsonPropertyName("sets")]
        public PlayerSetsCount? Sets { get; init; }
    }

    /// <summary>The sample behind the set-based record.</summary>
    public sealed record PlayerSetSample : LiveTennisModel
    {
        /// <summary>Matches counted.</summary>
        [JsonPropertyName("matches")]
        public int? Matches { get; init; }

        /// <summary>Counted out by reason: <c>retired</c>, <c>walkover</c>, <c>default</c>, <c>score_incomplete</c>.</summary>
        [JsonPropertyName("excluded")]
        public IReadOnlyDictionary<string, int>? Excluded { get; init; }
    }

    /// <summary>Comeback record.</summary>
    public sealed record PlayerComebacks : LiveTennisModel
    {
        /// <summary>Matches in which the player lost at least one set.</summary>
        [JsonPropertyName("matches_lost_a_set")]
        public int? MatchesLostASet { get; init; }

        /// <summary>How many of those the player won.</summary>
        [JsonPropertyName("won")]
        public int? Won { get; init; }

        /// <summary>Percent, or <c>null</c> on an empty sample.</summary>
        [JsonPropertyName("win_pct")]
        public double? WinPct { get; init; }
    }

    /// <summary>Sets won and lost.</summary>
    public sealed record PlayerSetsCount : LiveTennisModel
    {
        /// <summary>Sets won.</summary>
        [JsonPropertyName("won")]
        public int? Won { get; init; }

        /// <summary>Sets lost.</summary>
        [JsonPropertyName("lost")]
        public int? Lost { get; init; }

        /// <summary>Percent, or <c>null</c> on an empty sample.</summary>
        [JsonPropertyName("win_pct")]
        public double? WinPct { get; init; }
    }

    /// <summary>Withdrawals per outcome.</summary>
    public sealed record PlayerWithdrawals : LiveTennisModel
    {
        /// <summary>Retirements.</summary>
        [JsonPropertyName("retired")]
        public PlayerWithdrawalCount? Retired { get; init; }

        /// <summary>Walkovers.</summary>
        [JsonPropertyName("walkover")]
        public PlayerWithdrawalCount? Walkover { get; init; }

        /// <summary>Defaults.</summary>
        [JsonPropertyName("default")]
        public PlayerWithdrawalCount? Default { get; init; }
    }

    /// <summary>Given (this player did not finish) and received (the opponent did not).</summary>
    public sealed record PlayerWithdrawalCount : LiveTennisModel
    {
        /// <summary>Matches this player did not finish.</summary>
        [JsonPropertyName("given")]
        public int? Given { get; init; }

        /// <summary>Matches the opponent did not finish.</summary>
        [JsonPropertyName("received")]
        public int? Received { get; init; }
    }

    /// <summary>Matches left out of every record, by reason.</summary>
    public sealed record PlayerRecordExclusions : LiveTennisModel
    {
        /// <summary>Not a singles match.</summary>
        [JsonPropertyName("not_singles")]
        public int? NotSingles { get; init; }

        /// <summary>Unresolved.</summary>
        [JsonPropertyName("unresolved")]
        public int? Unresolved { get; init; }

        /// <summary>Abandoned.</summary>
        [JsonPropertyName("abandoned")]
        public int? Abandoned { get; init; }

        /// <summary>No published winner.</summary>
        [JsonPropertyName("no_published_winner")]
        public int? NoPublishedWinner { get; init; }
    }

    /// <summary>One recent match, seen from the player's side.</summary>
    public sealed record PlayerRecentMatch : LiveTennisModel
    {
        /// <summary>Match id (joins <c>/matches/{id}</c>).</summary>
        [JsonPropertyName("match_id")]
        public int? MatchId { get; init; }

        /// <summary>Play date (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        /// <summary>Scheduled start (ISO 8601 UTC), or <c>null</c>.</summary>
        [JsonPropertyName("scheduled_time")]
        public string? ScheduledTime { get; init; }

        /// <summary>Tournament id, or <c>null</c>.</summary>
        [JsonPropertyName("tournament_id")]
        public string? TournamentId { get; init; }

        /// <summary>Tournament name, or <c>null</c>.</summary>
        [JsonPropertyName("tournament")]
        public string? Tournament { get; init; }

        /// <summary>Round label, or <c>null</c>.</summary>
        [JsonPropertyName("round")]
        public string? Round { get; init; }

        /// <summary>Normalized round code, or <c>null</c>.</summary>
        [JsonPropertyName("round_code")]
        public string? RoundCode { get; init; }

        /// <summary>Surface, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Indoor flag, or <c>null</c> when not stated.</summary>
        [JsonPropertyName("indoor")]
        public bool? Indoor { get; init; }

        /// <summary>The match tour, or <c>null</c>.</summary>
        [JsonPropertyName("level")]
        public string? Level { get; init; }

        /// <summary>The opponent.</summary>
        [JsonPropertyName("opponent")]
        public PlayerRef? Opponent { get; init; }

        /// <summary>Whether this player won.</summary>
        [JsonPropertyName("won")]
        public bool? Won { get; init; }

        /// <summary><c>completed</c>, <c>retired</c>, <c>walkover</c> or <c>default</c>.</summary>
        [JsonPropertyName("outcome")]
        public string? Outcome { get; init; }

        /// <summary>The final score turned round to this player, or <c>null</c>.</summary>
        [JsonPropertyName("score")]
        public PlayerRecentScore? Score { get; init; }
    }

    /// <summary>A final score with this player first: sets [player, opponent], games [[player per set], [opponent per set]].</summary>
    public sealed record PlayerRecentScore : LiveTennisModel
    {
        /// <summary>Sets won, [player, opponent].</summary>
        [JsonPropertyName("sets")]
        public IReadOnlyList<int?>? Sets { get; init; }

        /// <summary>Games per set, player-major.</summary>
        [JsonPropertyName("games")]
        public IReadOnlyList<IReadOnlyList<int>>? Games { get; init; }
    }

    /// <summary>
    /// Measured per-match statistics summed over the window. <b>ULTRA.</b> Each
    /// field carries the number of matches it was summed over.
    /// </summary>
    public sealed record PlayerServeReturn : LiveTennisModel
    {
        /// <summary>Completed matches in the window.</summary>
        [JsonPropertyName("matches")]
        public int? Matches { get; init; }

        /// <summary>Matches that carried any statistic.</summary>
        [JsonPropertyName("matches_with_stats")]
        public int? MatchesWithStats { get; init; }

        /// <summary>Serve-side sums.</summary>
        [JsonPropertyName("serve")]
        public PlayerServeTotals? Serve { get; init; }

        /// <summary>Return-side sums.</summary>
        [JsonPropertyName("return")]
        public PlayerReturnTotals? Return { get; init; }
    }

    /// <summary>Serve-side sums.</summary>
    public sealed record PlayerServeTotals : LiveTennisModel
    {
        /// <summary>Aces.</summary>
        [JsonPropertyName("aces")]
        public PlayerStatCount? Aces { get; init; }

        /// <summary>Double faults.</summary>
        [JsonPropertyName("double_faults")]
        public PlayerStatCount? DoubleFaults { get; init; }

        /// <summary>First serves in.</summary>
        [JsonPropertyName("first_serve_in")]
        public PlayerStatRatio? FirstServeIn { get; init; }

        /// <summary>First-serve points won.</summary>
        [JsonPropertyName("first_serve_points_won")]
        public PlayerStatRatio? FirstServePointsWon { get; init; }

        /// <summary>Second-serve points won.</summary>
        [JsonPropertyName("second_serve_points_won")]
        public PlayerStatRatio? SecondServePointsWon { get; init; }

        /// <summary>Service games won.</summary>
        [JsonPropertyName("service_games_won")]
        public PlayerStatRatio? ServiceGamesWon { get; init; }

        /// <summary>Break points saved.</summary>
        [JsonPropertyName("break_points_saved")]
        public PlayerStatRatio? BreakPointsSaved { get; init; }
    }

    /// <summary>Return-side sums.</summary>
    public sealed record PlayerReturnTotals : LiveTennisModel
    {
        /// <summary>First-serve return points won.</summary>
        [JsonPropertyName("first_serve_return_points_won")]
        public PlayerStatRatio? FirstServeReturnPointsWon { get; init; }

        /// <summary>Second-serve return points won.</summary>
        [JsonPropertyName("second_serve_return_points_won")]
        public PlayerStatRatio? SecondServeReturnPointsWon { get; init; }

        /// <summary>Return games won.</summary>
        [JsonPropertyName("return_games_won")]
        public PlayerStatRatio? ReturnGamesWon { get; init; }

        /// <summary>Break points converted.</summary>
        [JsonPropertyName("break_points_converted")]
        public PlayerStatRatio? BreakPointsConverted { get; init; }
    }

    /// <summary>A sum of numerators over a sum of denominators. All null with <see cref="Matches"/> 0 when no match carried the field.</summary>
    public sealed record PlayerStatRatio : LiveTennisModel
    {
        /// <summary>Summed numerator.</summary>
        [JsonPropertyName("numerator")]
        public int? Numerator { get; init; }

        /// <summary>Summed denominator.</summary>
        [JsonPropertyName("denominator")]
        public int? Denominator { get; init; }

        /// <summary>Percent, or <c>null</c>.</summary>
        [JsonPropertyName("pct")]
        public double? Pct { get; init; }

        /// <summary>Matches summed over.</summary>
        [JsonPropertyName("matches")]
        public int? Matches { get; init; }
    }

    /// <summary>A summed count. <see cref="Total"/> and <see cref="PerMatch"/> are null with <see cref="Matches"/> 0 when no match carried it.</summary>
    public sealed record PlayerStatCount : LiveTennisModel
    {
        /// <summary>Summed total.</summary>
        [JsonPropertyName("total")]
        public int? Total { get; init; }

        /// <summary>Matches summed over.</summary>
        [JsonPropertyName("matches")]
        public int? Matches { get; init; }

        /// <summary>Total per match, or <c>null</c>.</summary>
        [JsonPropertyName("per_match")]
        public double? PerMatch { get; init; }
    }
}
