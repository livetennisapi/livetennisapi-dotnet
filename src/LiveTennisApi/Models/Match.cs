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

        /// <summary>
        /// The tour, in the <b>same vocabulary the <c>tour</c> query filter
        /// accepts</b> (<c>atp</c>, <c>wta</c>, <c>challenger</c>, <c>itf</c>,
        /// <c>juniors</c>) — a match selected by <c>?tour=X</c> always carries
        /// that value here. <c>null</c> when the feed never stated a tour or the
        /// event has no public tour name (exhibitions, team and mixed events).
        /// Safe to group and filter on; never parse the tournament name for this.
        /// Unlike <see cref="Player.Tour"/>, which stays opaque and granular.
        /// </summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>
        /// Stable tournament identity — one id per tournament × event type,
        /// stable across seasons. <c>null</c> on matches ingested before the
        /// catalogue covered their tournament.
        /// </summary>
        [JsonPropertyName("tournament_id")]
        public string? TournamentId { get; init; }

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

        /// <summary>
        /// The round in a controlled vocabulary (<c>F</c>, <c>SF</c>, <c>QF</c>,
        /// <c>R16</c>, <c>R32</c>, <c>R64</c>, <c>R128</c>, <c>RR</c>, <c>BR</c>,
        /// <c>Q</c>, <c>Q1</c>–<c>Q4</c>, <c>ER</c>), normalized from the
        /// free-text <see cref="Round"/> label. <c>null</c> when the label is
        /// unrecognised — never guessed.
        /// </summary>
        [JsonPropertyName("round_code")]
        public string? RoundCode { get; init; }

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

        /// <summary>
        /// Which player retired or conceded the walkover (<c>1</c> or <c>2</c>).
        /// Completed matches only — present only when <see cref="EventStatus"/> is
        /// <c>Retired</c> or <c>Walk Over</c> and the winner is derivable; the
        /// withdrawer is the loser by the rules of the sport.
        /// </summary>
        [JsonPropertyName("withdrew")]
        public int? Withdrew { get; init; }

        /// <summary>
        /// What point-by-point data is held for this match. Populated on
        /// <c>/history/matches</c> list rows only; <c>null</c> elsewhere.
        /// </summary>
        [JsonPropertyName("tape")]
        public TapeInfo? Tape { get; init; }

        /// <summary>Embedded market. <b>PRO+ only</b>; <c>null</c> below that tier.</summary>
        [JsonPropertyName("market")]
        public Market? Market { get; init; }

        /// <summary>Embedded analysis. <b>ULTRA only</b>; <c>null</c> below that tier.</summary>
        [JsonPropertyName("analysis")]
        public Analysis? Analysis { get; init; }
    }

    /// <summary>
    /// Tape coverage summary carried on <c>/history/matches</c> list rows, so a
    /// whole page can be qualified in one call instead of one request per match.
    /// </summary>
    public sealed record TapeInfo : LiveTennisModel
    {
        /// <summary>
        /// How the tape came to exist: <c>from_start</c>, <c>partial</c>,
        /// <c>reconstructed</c>, <c>reconstructed_partial</c>, or <c>none</c>.
        /// <c>from_start</c> says how the rows were obtained (watched live from
        /// 0-0), not that the tape is "complete".
        /// </summary>
        [JsonPropertyName("coverage")]
        public string? Coverage { get; init; }

        /// <summary>
        /// Rows observed (watched live). Not the length of the tape that will be
        /// served — use <see cref="HistoryTapeMeta.Rows"/> on the per-match tape
        /// for that.
        /// </summary>
        [JsonPropertyName("rows")]
        public int? Rows { get; init; }

        /// <summary>Reconstructed rows available for this match.</summary>
        [JsonPropertyName("reconstructed_rows")]
        public int? ReconstructedRows { get; init; }
    }
}
