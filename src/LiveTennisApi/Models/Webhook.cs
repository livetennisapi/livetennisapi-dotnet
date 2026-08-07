using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// An outbound webhook registration. <b>ULTRA, direct keys only</b> (a
    /// marketplace key gets a <c>403</c> with code <c>direct_key_required</c>).
    /// </summary>
    /// <remarks>
    /// The API POSTs the same frames the WebSocket sends to your HTTPS endpoint
    /// on every live score commit. Up to <b>3 webhooks per key</b> (a fourth
    /// registration is a <c>409</c> <c>webhook_limit</c>).
    /// <see cref="Secret"/> is present <b>only on the registration response</b> —
    /// it is shown exactly once and never returned again, so store it
    /// immediately.
    /// </remarks>
    public sealed record Webhook : LiveTennisModel
    {
        /// <summary>The webhook id.</summary>
        [JsonPropertyName("id")]
        public int? Id { get; init; }

        /// <summary>The destination URL (HTTPS only, publicly routable).</summary>
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        /// <summary>The subscribed events: <c>score</c> and/or <c>break_point</c>.</summary>
        [JsonPropertyName("events")]
        public IReadOnlyList<string>? Events { get; init; }

        /// <summary>Whether deliveries are enabled.</summary>
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; init; }

        /// <summary>When the webhook was registered (UTC ISO string), or <c>null</c>.</summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        /// <summary>When the last delivery was made (UTC ISO string), or <c>null</c>.</summary>
        [JsonPropertyName("last_delivery_at")]
        public string? LastDeliveryAt { get; init; }

        /// <summary>Consecutive failed deliveries.</summary>
        [JsonPropertyName("consecutive_failures")]
        public int? ConsecutiveFailures { get; init; }

        /// <summary>The last delivery error, or <c>null</c>.</summary>
        [JsonPropertyName("last_error")]
        public string? LastError { get; init; }

        /// <summary>
        /// The signing secret. Present <b>only</b> on the <c>201</c>
        /// registration response — shown exactly once, never listed again.
        /// </summary>
        [JsonPropertyName("secret")]
        public string? Secret { get; init; }

        /// <summary>The server's note about the secret's one-time visibility, if sent.</summary>
        [JsonPropertyName("secret_note")]
        public string? SecretNote { get; init; }
    }

    /// <summary>The response of a webhook deletion.</summary>
    public sealed record WebhookDeleted : LiveTennisModel
    {
        /// <summary>The id of the deleted webhook.</summary>
        [JsonPropertyName("deleted")]
        public int? Deleted { get; init; }
    }
}
