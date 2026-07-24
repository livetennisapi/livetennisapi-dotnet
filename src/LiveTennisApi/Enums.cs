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
    }
}
