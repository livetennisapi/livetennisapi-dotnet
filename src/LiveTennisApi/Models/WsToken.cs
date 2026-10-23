using System.Text.Json.Serialization;

namespace LiveTennisApi.Models
{
    /// <summary>
    /// The channel vocabulary of the push WebSocket feed. Minted with the
    /// model fields off, <see cref="Match"/> and <see cref="Slate"/> name the
    /// score-only pair instead: <c>match:{match_id}:score_only</c> and
    /// <c>slate:score_only</c>.
    /// </summary>
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
    /// endpoints return, nested under <c>score</c>. Since 2026-10-23 every
    /// <c>score</c> frame also carries <c>model</c> (<c>pending</c>,
    /// <c>ready</c> or <c>off</c>) and a state leaves the score channels
    /// twice, pending then ready, with the same <c>score.sequence</c>: dedup
    /// on <c>(match_id, score.sequence, model)</c>, and read a frame with no
    /// <c>model</c> as <c>ready</c>. Mint a fresh token on reconnect.
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

        /// <summary>
        /// Echo of the <c>model_fields</c> switch the token was minted with
        /// (since 2026-10-23). <c>off</c> means <see cref="Channels"/> names the
        /// score-only streams, one <c>model: off</c> frame per state with the
        /// model keys left out; <c>on</c> (the default) means the score
        /// channels, every state as <c>pending</c> then <c>ready</c>. Null on a
        /// server that predates the switch.
        /// </summary>
        [JsonPropertyName("model_fields")]
        public string? ModelFields { get; init; }
    }
}
