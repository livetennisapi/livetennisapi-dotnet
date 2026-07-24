using System;

namespace LiveTennisApi
{
    /// <summary>Which header carries the API key.</summary>
    public enum AuthHeader
    {
        /// <summary><c>Authorization: Bearer &lt;key&gt;</c> (default).</summary>
        Bearer,

        /// <summary><c>X-API-Key: &lt;key&gt;</c>.</summary>
        ApiKey,
    }

    /// <summary>Configuration for a <see cref="LiveTennisApiClient"/>.</summary>
    public sealed class LiveTennisApiClientOptions
    {
        /// <summary>The default API base URL.</summary>
        public const string DefaultBaseUrl = "https://api.livetennisapi.com/api/public/v1";

        /// <summary>The API base URL. Defaults to <see cref="DefaultBaseUrl"/>.</summary>
        public string BaseUrl { get; set; } = DefaultBaseUrl;

        /// <summary>Which header carries the key. Defaults to <see cref="AuthHeader.Bearer"/>.</summary>
        public AuthHeader AuthHeader { get; set; } = AuthHeader.Bearer;

        /// <summary>
        /// Per-request timeout. Defaults to 30 seconds. Applied only when the
        /// client owns its <see cref="System.Net.Http.HttpClient"/>; when you
        /// supply your own, configure its timeout yourself.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Automatic retries for transient failures (<c>429</c> and <c>5xx</c>)
        /// only. Defaults to 2. Set to 0 to disable.
        /// </summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>
        /// The <c>User-Agent</c> sent with each request. Defaults to
        /// <c>livetennisapi-dotnet/&lt;version&gt;</c>.
        /// </summary>
        public string? UserAgent { get; set; }
    }
}
