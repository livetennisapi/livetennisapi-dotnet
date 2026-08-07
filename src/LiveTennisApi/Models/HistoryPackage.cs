using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>One downloadable file of a history package.</summary>
    public sealed record HistoryPackageFile : LiveTennisModel
    {
        /// <summary>File format: <c>jsonl</c> or <c>csv</c>.</summary>
        [JsonPropertyName("format")]
        public string? Format { get; init; }

        /// <summary>The file name.</summary>
        [JsonPropertyName("filename")]
        public string? Filename { get; init; }

        /// <summary>File size in bytes.</summary>
        [JsonPropertyName("bytes")]
        public long? Bytes { get; init; }

        /// <summary>SHA-256 of the file contents.</summary>
        [JsonPropertyName("sha256")]
        public string? Sha256 { get; init; }
    }

    /// <summary>
    /// A published monthly bulk package. <b>PRO tier and above (or a package
    /// subscription).</b>
    /// </summary>
    /// <remarks>
    /// Coverage is not a contiguous run of months and is still being extended
    /// backwards, so treat the packages listing as the authoritative set of
    /// months that exist. The JSONL file holds one line per <b>match</b> (a whole
    /// tape object per line, coverage meta included); the CSV is flattened to one
    /// row per point and carries no coverage columns.
    /// </remarks>
    public sealed record HistoryPackage : LiveTennisModel
    {
        /// <summary>The month, <c>YYYY-MM</c>.</summary>
        [JsonPropertyName("period")]
        public string? Period { get; init; }

        /// <summary>Package status; only <c>ready</c> months are listed or served.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        /// <summary>
        /// Matches in the package. On a <c>rankings</c> package this is the
        /// number of players covered.
        /// </summary>
        [JsonPropertyName("match_count")]
        public int? MatchCount { get; init; }

        /// <summary>
        /// Tape rows in the package. On a <c>rankings</c> package this is the
        /// number of ranking records.
        /// </summary>
        [JsonPropertyName("row_count")]
        public long? RowCount { get; init; }

        /// <summary>The downloadable files.</summary>
        [JsonPropertyName("files")]
        public IReadOnlyList<HistoryPackageFile>? Files { get; init; }

        /// <summary>When the package was built (UTC ISO string), or <c>null</c>.</summary>
        [JsonPropertyName("built_at")]
        public string? BuiltAt { get; init; }

        /// <summary>
        /// The package family. Present only on non-tape packages (e.g.
        /// <c>rankings</c>), so the shape a tape client already parses is
        /// unchanged; <c>null</c> means <c>tape</c>.
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }
    }
}
