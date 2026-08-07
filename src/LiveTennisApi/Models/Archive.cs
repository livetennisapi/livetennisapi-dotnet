using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// One side (winner or loser) of an archive result. Results data is recorded
    /// winner/loser-shaped at the source, so the winner is a field, never an
    /// inference.
    /// </summary>
    public sealed record ArchiveSidePlayer : LiveTennisModel
    {
        /// <summary>Name, or <c>null</c>.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        /// <summary>Playing hand, or <c>null</c>.</summary>
        [JsonPropertyName("hand")]
        public string? Hand { get; init; }

        /// <summary>3-letter country code (same vocabulary as <see cref="Player.Country"/>), or <c>null</c>.</summary>
        [JsonPropertyName("country")]
        public string? Country { get; init; }

        /// <summary>The player's rank <b>at the time</b> of the match, as published, or <c>null</c>.</summary>
        [JsonPropertyName("rank")]
        public int? Rank { get; init; }

        /// <summary>Seed, or <c>null</c>.</summary>
        [JsonPropertyName("seed")]
        public int? Seed { get; init; }

        /// <summary>
        /// The corpus person id — joins <c>/history/archive/players</c> within the
        /// same tour. <b>Not</b> a roster player id.
        /// </summary>
        [JsonPropertyName("player_id")]
        public int? PlayerId { get; init; }

        /// <summary>Height in cm, or <c>null</c>.</summary>
        [JsonPropertyName("height_cm")]
        public int? HeightCm { get; init; }

        /// <summary>Age at the time of the match, as the corpus records it, or <c>null</c>.</summary>
        [JsonPropertyName("age")]
        public double? Age { get; init; }

        /// <summary>
        /// Draw entry where recorded (<c>WC</c>, <c>Q</c>, <c>LL</c>, <c>PR</c>,
        /// <c>SE</c>, …) — <c>null</c> for direct acceptances.
        /// </summary>
        [JsonPropertyName("entry")]
        public string? Entry { get; init; }
    }

    /// <summary>
    /// One deep-archive result (1968–2022). <b>BASIC tier, or any History plan.</b>
    /// A separate id space from <c>/matches</c>; <see cref="SourceId"/> is the
    /// stable corpus key. <see cref="EventDate"/> is the <b>tournament start</b>
    /// date — per-match dates do not exist in this era's records.
    /// </summary>
    public sealed record ArchiveMatch : LiveTennisModel
    {
        /// <summary>Archive record id (own id space).</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>The stable corpus key.</summary>
        [JsonPropertyName("source_id")]
        public string? SourceId { get; init; }

        /// <summary>Tour: <c>atp</c> or <c>wta</c>.</summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>
        /// Source tier code: <c>G</c>=grand slam, <c>M</c>=masters, <c>A</c>=tour,
        /// <c>F</c>=finals, <c>D</c>=davis cup, <c>C</c>=challenger,
        /// <c>O</c>=olympics; futures tiers carry their category codes (e.g.
        /// <c>15</c>, <c>25</c>) as published.
        /// </summary>
        [JsonPropertyName("level")]
        public string? Level { get; init; }

        /// <summary>Tournament name, or <c>null</c>.</summary>
        [JsonPropertyName("tournament")]
        public string? Tournament { get; init; }

        /// <summary>Surface, or <c>null</c>.</summary>
        [JsonPropertyName("surface")]
        public string? Surface { get; init; }

        /// <summary>Draw size, or <c>null</c>.</summary>
        [JsonPropertyName("draw_size")]
        public int? DrawSize { get; init; }

        /// <summary>Tournament <b>start</b> date (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("event_date")]
        public string? EventDate { get; init; }

        /// <summary>Round code (<c>F</c>, <c>SF</c>, <c>QF</c>, <c>R16</c>, …), or <c>null</c>.</summary>
        [JsonPropertyName("round")]
        public string? Round { get; init; }

        /// <summary>Best of 3 or 5, or <c>null</c>.</summary>
        [JsonPropertyName("best_of")]
        public int? BestOf { get; init; }

        /// <summary>Match duration in minutes where recorded, or <c>null</c>.</summary>
        [JsonPropertyName("minutes")]
        public int? Minutes { get; init; }

        /// <summary>The winner. A stored column, never an inference.</summary>
        [JsonPropertyName("winner")]
        public ArchiveSidePlayer? Winner { get; init; }

        /// <summary>The loser.</summary>
        [JsonPropertyName("loser")]
        public ArchiveSidePlayer? Loser { get; init; }

        /// <summary>The final score as published, e.g. <c>"6-4 7-6(5)"</c>, <c>"6-3 RET"</c>, <c>"W/O"</c>.</summary>
        [JsonPropertyName("score")]
        public string? Score { get; init; }

        /// <summary>
        /// Parsed from the score's own vocabulary: <c>completed</c>,
        /// <c>retired</c>, <c>walkover</c>, <c>default</c>, <c>abandoned</c>, or
        /// <c>null</c> when unparseable — never guessed.
        /// </summary>
        [JsonPropertyName("outcome")]
        public string? Outcome { get; init; }

        /// <summary>
        /// Per-match serve statistics, detail endpoint only:
        /// <c>{"winner":{…},"loser":{…}}</c> with aces, double_faults,
        /// serve_points, first_in, first_won, second_won, serve_games, bp_saved,
        /// bp_faced where the source recorded them. <c>null</c> otherwise (most
        /// rows before 1991) — never synthesised.
        /// </summary>
        [JsonPropertyName("stats")]
        public JsonElement? Stats { get; init; }
    }

    /// <summary>
    /// One archive person — hand, date of birth, country, height, career-high.
    /// <b>BASIC tier, or any History plan.</b> Own id space: <see cref="Id"/> is
    /// the corpus person id archive match rows carry as
    /// <c>winner.player_id</c>/<c>loser.player_id</c>, scoped per tour; never a
    /// roster id. Null fields are the era's silence, never guessed.
    /// </summary>
    public sealed record ArchivePlayerBio : LiveTennisModel
    {
        /// <summary>The corpus person id (per tour).</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>Tour: <c>atp</c> or <c>wta</c>.</summary>
        [JsonPropertyName("tour")]
        public string? Tour { get; init; }

        /// <summary>Name, or <c>null</c>.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        /// <summary>Playing hand, or <c>null</c>.</summary>
        [JsonPropertyName("hand")]
        public string? Hand { get; init; }

        /// <summary>Date of birth (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("dob")]
        public string? Dob { get; init; }

        /// <summary>3-letter country code, or <c>null</c>.</summary>
        [JsonPropertyName("country")]
        public string? Country { get; init; }

        /// <summary>Height in cm, or <c>null</c>.</summary>
        [JsonPropertyName("height_cm")]
        public int? HeightCm { get; init; }

        /// <summary>Career-high rank, computed from the corpus's own weekly ranking tables, or <c>null</c>.</summary>
        [JsonPropertyName("career_high_rank")]
        public int? CareerHighRank { get; init; }

        /// <summary>The earliest week the career-high rank was reached (<c>YYYY-MM-DD</c>), or <c>null</c>.</summary>
        [JsonPropertyName("career_high_date")]
        public string? CareerHighDate { get; init; }
    }

    /// <summary>A wins/losses split.</summary>
    public sealed record ArchiveWinLoss : LiveTennisModel
    {
        /// <summary>Wins.</summary>
        [JsonPropertyName("wins")]
        public int? Wins { get; init; }

        /// <summary>Losses.</summary>
        [JsonPropertyName("losses")]
        public int? Losses { get; init; }
    }

    /// <summary>The career span of an archive player.</summary>
    public sealed record ArchiveCareerSpan : LiveTennisModel
    {
        /// <summary>First recorded appearance, or <c>null</c>.</summary>
        [JsonPropertyName("first")]
        public string? First { get; init; }

        /// <summary>Last recorded appearance, or <c>null</c>.</summary>
        [JsonPropertyName("last")]
        public string? Last { get; init; }
    }

    /// <summary>The W-L record block of an archive career.</summary>
    public sealed record ArchiveCareerRecord : LiveTennisModel
    {
        /// <summary>Career wins.</summary>
        [JsonPropertyName("wins")]
        public int? Wins { get; init; }

        /// <summary>Career losses.</summary>
        [JsonPropertyName("losses")]
        public int? Losses { get; init; }

        /// <summary>Finals won (excluding abandoned finals).</summary>
        [JsonPropertyName("titles")]
        public int? Titles { get; init; }

        /// <summary>W-L by surface; keys are surface names.</summary>
        [JsonPropertyName("by_surface")]
        public IReadOnlyDictionary<string, ArchiveWinLoss>? BySurface { get; init; }

        /// <summary>W-L by source tier code.</summary>
        [JsonPropertyName("by_level")]
        public IReadOnlyDictionary<string, ArchiveWinLoss>? ByLevel { get; init; }
    }

    /// <summary>One season of an archive career.</summary>
    public sealed record ArchiveCareerYear : LiveTennisModel
    {
        /// <summary>The year.</summary>
        [JsonPropertyName("year")]
        public int? Year { get; init; }

        /// <summary>Wins that year.</summary>
        [JsonPropertyName("wins")]
        public int? Wins { get; init; }

        /// <summary>Losses that year.</summary>
        [JsonPropertyName("losses")]
        public int? Losses { get; init; }
    }

    /// <summary>
    /// Summed serve statistics + derived ratios over an archive career. Ratios
    /// are <c>null</c> where the denominator is zero. The corpus records
    /// per-match serve statistics from 1991 only —
    /// <see cref="MatchesWithStats"/> states the coverage honestly.
    /// </summary>
    public sealed record ArchiveCareerServe : LiveTennisModel
    {
        /// <summary>Matches contributing serve statistics (from 1991 only).</summary>
        [JsonPropertyName("matches_with_stats")]
        public int? MatchesWithStats { get; init; }

        /// <summary>Total aces.</summary>
        [JsonPropertyName("aces")]
        public int? Aces { get; init; }

        /// <summary>Total double faults.</summary>
        [JsonPropertyName("double_faults")]
        public int? DoubleFaults { get; init; }

        /// <summary>Total serve points.</summary>
        [JsonPropertyName("serve_points")]
        public int? ServePoints { get; init; }

        /// <summary>Total first serves in.</summary>
        [JsonPropertyName("first_in")]
        public int? FirstIn { get; init; }

        /// <summary>Total first-serve points won.</summary>
        [JsonPropertyName("first_won")]
        public int? FirstWon { get; init; }

        /// <summary>Total second-serve points won.</summary>
        [JsonPropertyName("second_won")]
        public int? SecondWon { get; init; }

        /// <summary>Total serve games.</summary>
        [JsonPropertyName("serve_games")]
        public int? ServeGames { get; init; }

        /// <summary>Total break points saved.</summary>
        [JsonPropertyName("bp_saved")]
        public int? BpSaved { get; init; }

        /// <summary>Total break points faced.</summary>
        [JsonPropertyName("bp_faced")]
        public int? BpFaced { get; init; }

        /// <summary>First-serve-in percentage, or <c>null</c>.</summary>
        [JsonPropertyName("first_in_pct")]
        public double? FirstInPct { get; init; }

        /// <summary>First-serve points-won percentage, or <c>null</c>.</summary>
        [JsonPropertyName("first_won_pct")]
        public double? FirstWonPct { get; init; }

        /// <summary>Second-serve points-won percentage, or <c>null</c>.</summary>
        [JsonPropertyName("second_won_pct")]
        public double? SecondWonPct { get; init; }

        /// <summary>Break-points-saved percentage, or <c>null</c>.</summary>
        [JsonPropertyName("bp_saved_pct")]
        public double? BpSavedPct { get; init; }

        /// <summary>Aces per match, or <c>null</c>.</summary>
        [JsonPropertyName("aces_per_match")]
        public double? AcesPerMatch { get; init; }
    }

    /// <summary>The player header of an archive career.</summary>
    public sealed record ArchiveCareerPlayer : LiveTennisModel
    {
        /// <summary>The resolved name.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    /// <summary>
    /// One player's whole archive career (1968–2022) in one response — sums and
    /// ratios of sums only, nothing modelled. <b>BASIC tier, or any History
    /// plan.</b> Ambiguous name fragments are refused with candidates (same
    /// <c>ambiguous_name</c> rule as <c>/h2h</c>).
    /// </summary>
    public sealed record ArchiveCareer : LiveTennisModel
    {
        /// <summary>The resolved player.</summary>
        [JsonPropertyName("player")]
        public ArchiveCareerPlayer? Player { get; init; }

        /// <summary>First and last recorded appearance.</summary>
        [JsonPropertyName("span")]
        public ArchiveCareerSpan? Span { get; init; }

        /// <summary>W-L record: overall, by surface, by level, plus titles.</summary>
        [JsonPropertyName("record")]
        public ArchiveCareerRecord? Record { get; init; }

        /// <summary>Per-year W-L.</summary>
        [JsonPropertyName("by_year")]
        public IReadOnlyList<ArchiveCareerYear>? ByYear { get; init; }

        /// <summary>Summed serve statistics with derived ratios.</summary>
        [JsonPropertyName("serve")]
        public ArchiveCareerServe? Serve { get; init; }
    }
}
