using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// Base for every response model. Captures any JSON field this version does
    /// not declare, so an additive <c>v1</c> change stays reachable via
    /// <see cref="AdditionalProperties"/> rather than being silently dropped.
    /// </summary>
    public abstract record LiveTennisModel
    {
        /// <summary>
        /// Fields the server sent that this model version does not declare.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
    }

    /// <summary>The pagination envelope returned alongside list responses.</summary>
    public sealed record ListMeta : LiveTennisModel
    {
        /// <summary>The page size that was applied.</summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; init; }

        /// <summary>The offset that was applied.</summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; init; }

        /// <summary>
        /// The number of items on <b>this page</b> — not the total across all
        /// pages. Prefer <see cref="HasMore"/> as the end-of-data signal rather
        /// than comparing this to the requested limit.
        /// </summary>
        [JsonPropertyName("count")]
        public int? Count { get; init; }

        /// <summary>
        /// Size of the whole filtered set. <c>null</c> when it cannot be counted
        /// cheaply (for example <c>/matches?status=completed</c>).
        /// </summary>
        [JsonPropertyName("total")]
        public int? Total { get; init; }

        /// <summary>
        /// Whether more results exist beyond this page. Read this rather than
        /// comparing <see cref="Count"/> to the limit.
        /// </summary>
        [JsonPropertyName("has_more")]
        public bool? HasMore { get; init; }
    }

    /// <summary>A single page of a list endpoint: <c>{ data, meta }</c>.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed record Page<T> : LiveTennisModel
    {
        /// <summary>The items on this page. Never <c>null</c> (empty when absent).</summary>
        [JsonPropertyName("data")]
        public IReadOnlyList<T> Data { get; init; } = new List<T>();

        /// <summary>The pagination envelope, if the server sent one.</summary>
        [JsonPropertyName("meta")]
        public ListMeta? Meta { get; init; }
    }

    /// <summary>The response from the <c>/health</c> liveness probe.</summary>
    public sealed record HealthStatus : LiveTennisModel
    {
        /// <summary>Service status, <c>ok</c> when healthy.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        /// <summary>API version, <c>v1</c>.</summary>
        [JsonPropertyName("version")]
        public string? Version { get; init; }
    }
}
