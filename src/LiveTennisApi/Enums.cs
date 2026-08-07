using System;

namespace LiveTennisApi
{
    /// <summary>
    /// The <c>tour</c> <b>filter</b> accepted by <c>/matches</c> and
    /// <c>/fixtures</c>.
    /// </summary>
    /// <remarks>
    /// This is the request-side vocabulary only. Each value covers its singles
    /// and doubles draws — <see cref="Atp"/> includes ATP doubles,
    /// <see cref="Juniors"/> covers the boys' and girls' Grand Slam draws. An
    /// unrecognised value is a <c>400</c>, never a silent pass-through.
    /// <para>
    /// It is deliberately <b>not</b> the same vocabulary as a record's own
    /// <c>tour</c> field (see <see cref="Models.Player.Tour"/>), which is a
    /// granular, sometimes UPPERCASE, opaque string. Do not map one onto the
    /// other.
    /// </para>
    /// </remarks>
    public enum Tour
    {
        /// <summary>ATP tour (men's singles and doubles).</summary>
        Atp,

        /// <summary>WTA tour (women's singles and doubles).</summary>
        Wta,

        /// <summary>ATP Challenger tour.</summary>
        Challenger,

        /// <summary>ITF tour.</summary>
        Itf,

        /// <summary>Junior Grand Slam draws (boys and girls).</summary>
        Juniors,
    }

    /// <summary>The lifecycle status filter accepted by <c>/matches</c>.</summary>
    public enum MatchStatus
    {
        /// <summary>Matches currently in play.</summary>
        Live,

        /// <summary>Scheduled matches that have not started.</summary>
        Upcoming,

        /// <summary>Finished matches.</summary>
        Completed,
    }

    /// <summary>
    /// The <c>tour</c> filter accepted by the deep-archive endpoints
    /// (<c>/history/archive/*</c>). The 1968–2022 results archive covers the two
    /// main tours only, so this is deliberately narrower than <see cref="Tour"/>.
    /// </summary>
    public enum ArchiveTour
    {
        /// <summary>ATP archive records.</summary>
        Atp,

        /// <summary>WTA archive records.</summary>
        Wta,
    }

    /// <summary>A ranking system accepted by <c>/rankings</c>.</summary>
    /// <remarks>
    /// Systems are never collapsed into a single "rank" — they are not
    /// comparable. ATP/WTA and the ITF circuits carry rank+points; UTR carries a
    /// rating with null rank and points (it is a rating, not a ranking), and has
    /// no listing mode.
    /// </remarks>
    public enum RankingSystem
    {
        /// <summary>ATP singles ranking.</summary>
        Atp,

        /// <summary>WTA singles ranking.</summary>
        Wta,

        /// <summary>ITF junior circuit ranking.</summary>
        ItfJuniors,

        /// <summary>ITF men's World Tennis Tour ranking.</summary>
        ItfMen,

        /// <summary>ITF women's World Tennis Tour ranking.</summary>
        ItfWomen,

        /// <summary>UTR rating (no rank/points, no listing mode).</summary>
        Utr,
    }

    /// <summary>The gender filter accepted by the rally and charting endpoints.</summary>
    public enum Gender
    {
        /// <summary>Men's matches (<c>M</c> / <c>men</c> on the wire).</summary>
        Men,

        /// <summary>Women's matches (<c>W</c> / <c>women</c> on the wire).</summary>
        Women,
    }

    /// <summary>The <c>?sequence=</c> mode of the per-match tape.</summary>
    public enum TapeSequence
    {
        /// <summary>
        /// Every row as committed — deliberately non-monotonic, since independent
        /// sources race and a higher-trust one may correct a lower-trust one
        /// backwards.
        /// </summary>
        Raw,

        /// <summary>
        /// One row per distinct score state, keeping the last assertion of each.
        /// Only clean rows carry <see cref="Models.HistoryTapeRow.PointWinner"/>.
        /// </summary>
        Clean,
    }

    /// <summary>The package family of <c>/history/packages</c>.</summary>
    public enum HistoryPackageKind
    {
        /// <summary>Point-by-point match tapes (the default family).</summary>
        Tape,

        /// <summary>As-of ranking records. <b>ULTRA.</b></summary>
        Rankings,
    }

    /// <summary>An event family a webhook can subscribe to.</summary>
    public enum WebhookEvent
    {
        /// <summary>Live score commits (the default).</summary>
        Score,

        /// <summary>Break-point alerts.</summary>
        BreakPoint,
    }

    /// <summary>Serialization helpers for the request-side enums.</summary>
    internal static class EnumExtensions
    {
        /// <summary>The lowercase wire value for a <see cref="Tour"/> filter.</summary>
        public static string ToQueryValue(this Tour tour)
        {
            switch (tour)
            {
                case Tour.Atp: return "atp";
                case Tour.Wta: return "wta";
                case Tour.Challenger: return "challenger";
                case Tour.Itf: return "itf";
                case Tour.Juniors: return "juniors";
                default: throw new ArgumentOutOfRangeException(nameof(tour), tour, "Unknown tour filter.");
            }
        }

        /// <summary>The lowercase wire value for a <see cref="MatchStatus"/>.</summary>
        public static string ToQueryValue(this MatchStatus status)
        {
            switch (status)
            {
                case MatchStatus.Live: return "live";
                case MatchStatus.Upcoming: return "upcoming";
                case MatchStatus.Completed: return "completed";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown match status.");
            }
        }

        /// <summary>The lowercase wire value for an <see cref="ArchiveTour"/> filter.</summary>
        public static string ToQueryValue(this ArchiveTour tour)
        {
            switch (tour)
            {
                case ArchiveTour.Atp: return "atp";
                case ArchiveTour.Wta: return "wta";
                default: throw new ArgumentOutOfRangeException(nameof(tour), tour, "Unknown archive tour filter.");
            }
        }

        /// <summary>The wire value for a <see cref="RankingSystem"/>.</summary>
        public static string ToQueryValue(this RankingSystem system)
        {
            switch (system)
            {
                case RankingSystem.Atp: return "atp";
                case RankingSystem.Wta: return "wta";
                case RankingSystem.ItfJuniors: return "itf_jt";
                case RankingSystem.ItfMen: return "itf_mt";
                case RankingSystem.ItfWomen: return "itf_wt";
                case RankingSystem.Utr: return "utr";
                default: throw new ArgumentOutOfRangeException(nameof(system), system, "Unknown ranking system.");
            }
        }

        /// <summary>The <c>M</c>/<c>W</c> wire value the rally endpoints accept.</summary>
        public static string ToRallyQueryValue(this Gender gender)
        {
            switch (gender)
            {
                case Gender.Men: return "M";
                case Gender.Women: return "W";
                default: throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown gender filter.");
            }
        }

        /// <summary>The <c>men</c>/<c>women</c> wire value the charting endpoints accept.</summary>
        public static string ToChartingQueryValue(this Gender gender)
        {
            switch (gender)
            {
                case Gender.Men: return "men";
                case Gender.Women: return "women";
                default: throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown gender filter.");
            }
        }

        /// <summary>The lowercase wire value for a <see cref="TapeSequence"/>.</summary>
        public static string ToQueryValue(this TapeSequence sequence)
        {
            switch (sequence)
            {
                case TapeSequence.Raw: return "raw";
                case TapeSequence.Clean: return "clean";
                default: throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Unknown tape sequence.");
            }
        }

        /// <summary>The lowercase wire value for a <see cref="HistoryPackageKind"/>.</summary>
        public static string ToQueryValue(this HistoryPackageKind kind)
        {
            switch (kind)
            {
                case HistoryPackageKind.Tape: return "tape";
                case HistoryPackageKind.Rankings: return "rankings";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown package kind.");
            }
        }

        /// <summary>The wire value for a <see cref="WebhookEvent"/>.</summary>
        public static string ToWireValue(this WebhookEvent webhookEvent)
        {
            switch (webhookEvent)
            {
                case WebhookEvent.Score: return "score";
                case WebhookEvent.BreakPoint: return "break_point";
                default: throw new ArgumentOutOfRangeException(nameof(webhookEvent), webhookEvent, "Unknown webhook event.");
            }
        }
    }
}
