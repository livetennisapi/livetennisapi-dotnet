using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>The channel vocabulary of the push WebSocket feed.</summary>
    public sealed record WsChannels : LiveTennisModel
    {
        /// <summary>
        /// The per-match channel template, <c>match:{match_id}</c> — substitute a
        /// match id.
        /// </summary>
        [JsonPropertyName("match")]
        public string? Match { get; init; }

        /// <summary>
        /// The whole-slate channel, <c>slate:all</c> — every live score frame.
        /// </summary>
        [JsonPropertyName("slate")]
        public string? Slate { get; init; }
    }

    /// <summary>
    /// A short-lived connection token for the high-fan-out push feed.
    /// <b>ULTRA only.</b> Frames are the same allowlist score objects the polling
    /// endpoints return. Mint a fresh token on reconnect.
    /// </summary>
    public sealed record WsToken : LiveTennisModel
    {
        /// <summary>The signed connection token.</summary>
        [JsonPropertyName("token")]
        public string? Token { get; init; }

        /// <summary>Token lifetime in seconds.</summary>
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; init; }

        /// <summary>The push WebSocket URL to connect to.</summary>
        [JsonPropertyName("ws_url")]
        public string? WsUrl { get; init; }

        /// <summary>The channel vocabulary (per-match channels and <c>slate:all</c>).</summary>
        [JsonPropertyName("channels")]
        public WsChannels? Channels { get; init; }
    }
}
