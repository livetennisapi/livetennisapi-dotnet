using System.Text.Json;

namespace LiveTennisApi.Internal
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used to (de)serialize every Live
    /// Tennis API payload.
    /// </summary>
    /// <remarks>
    /// Property names are mapped explicitly with <c>[JsonPropertyName]</c> on the
    /// models (snake_case on the wire), so no naming policy is needed here.
    /// Unknown JSON properties are ignored by System.Text.Json by default, which
    /// is exactly the contract this API asks for: additive changes ship within
    /// <c>v1</c>, so a strict client would break the first time a field is added.
    /// Every model also carries a <c>[JsonExtensionData]</c> bag, so a
    /// newly-added field stays reachable rather than lost.
    /// </remarks>
    internal static class LiveTennisJson
    {
        /// <summary>The shared, thread-safe serializer options.</summary>
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // Numbers occasionally arrive as JSON strings from upstream feeds;
            // tolerate that rather than throw on an otherwise-valid payload.
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };
    }
}
