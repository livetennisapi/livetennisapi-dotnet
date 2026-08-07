using System;
using System.Collections.Generic;
using System.Net;

namespace LiveTennisApi
{
    /// <summary>
    /// Base type for every error raised by this library.
    /// </summary>
    /// <remarks>
    /// Errors that reached the server and came back non-2xx carry a
    /// <see cref="StatusCode"/> and, when the body supplied one, a machine-readable
    /// <see cref="Code"/> (for example <c>upgrade_required</c>). Transport failures
    /// that never produced a response (see <see cref="ApiConnectionException"/>)
    /// leave <see cref="StatusCode"/> as <c>0</c>.
    /// <para>
    /// The common cases are distinguishable by type alone — catch
    /// <see cref="UpgradeRequiredException"/>, <see cref="RateLimitedException"/> or
    /// <see cref="UnauthorizedException"/> directly.
    /// </para>
    /// </remarks>
    public class LiveTennisApiException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="LiveTennisApiException"/> class.</summary>
        /// <param name="message">A human-readable description.</param>
        /// <param name="statusCode">The HTTP status code, or <c>0</c> for transport failures.</param>
        /// <param name="code">The API's machine-readable error code, if any.</param>
        /// <param name="requestUri">The request URL, if known.</param>
        /// <param name="body">The raw response body, if any.</param>
        /// <param name="headers">The response headers, if any.</param>
        /// <param name="innerException">The underlying exception, if any.</param>
        public LiveTennisApiException(
            string message,
            int statusCode = 0,
            string? code = null,
            string? requestUri = null,
            string? body = null,
            IReadOnlyDictionary<string, string>? headers = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Code = code;
            RequestUri = requestUri;
            Body = body;
            Headers = headers ?? EmptyHeaders;
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
            new Dictionary<string, string>(0);

        /// <summary>The HTTP status code, or <c>0</c> when no response was received.</summary>
        public int StatusCode { get; }

        /// <summary>
        /// The API's machine-readable error code from the response body
        /// (for example <c>upgrade_required</c>, <c>bad_tour</c>), or <c>null</c>.
        /// </summary>
        public string? Code { get; }

        /// <summary>The request URL, if known.</summary>
        public string? RequestUri { get; }

        /// <summary>The raw response body, if one was read.</summary>
        public string? Body { get; }

        /// <summary>The response headers, if any.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }
    }

    /// <summary>
    /// The request never produced a response (DNS, TLS, connection refused, or a
    /// cancelled/timed-out send). <see cref="LiveTennisApiException.StatusCode"/>
    /// is <c>0</c>.
    /// </summary>
    public class ApiConnectionException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="ApiConnectionException"/> class.</summary>
        /// <param name="message">A human-readable description.</param>
        /// <param name="requestUri">The request URL, if known.</param>
        /// <param name="innerException">The underlying transport exception.</param>
        public ApiConnectionException(string message, string? requestUri = null, Exception? innerException = null)
            : base(message, statusCode: 0, code: null, requestUri: requestUri, innerException: innerException)
        {
        }
    }

    /// <summary>The request exceeded the configured timeout.</summary>
    public sealed class ApiTimeoutException : ApiConnectionException
    {
        /// <summary>Initializes a new instance of the <see cref="ApiTimeoutException"/> class.</summary>
        /// <param name="message">A human-readable description.</param>
        /// <param name="requestUri">The request URL, if known.</param>
        /// <param name="innerException">The underlying exception.</param>
        public ApiTimeoutException(string message, string? requestUri = null, Exception? innerException = null)
            : base(message, requestUri, innerException)
        {
        }
    }

    /// <summary><c>400</c> — a query parameter was malformed (for example an unknown <c>tour</c>).</summary>
    public sealed class BadRequestException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="BadRequestException"/> class.</summary>
        public BadRequestException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers)
            : base(message, statusCode, code, requestUri, body, headers)
        {
        }
    }

    /// <summary><c>401</c> — the key is missing, unknown, or disabled.</summary>
    public sealed class UnauthorizedException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="UnauthorizedException"/> class.</summary>
        public UnauthorizedException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers)
            : base(message, statusCode, code, requestUri, body, headers)
        {
        }
    }

    /// <summary>
    /// <c>403</c> — the endpoint exists but your tier does not unlock it.
    /// </summary>
    /// <remarks>
    /// This is not an authentication failure: the key is valid, the plan is too
    /// low. <see cref="RequiredTier"/> is the lowest tier that unlocks the
    /// endpoint, inferred from the request path because the API returns only
    /// <c>{"error":"upgrade_required"}</c>.
    /// </remarks>
    public sealed class UpgradeRequiredException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="UpgradeRequiredException"/> class.</summary>
        public UpgradeRequiredException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers, string? requiredTier)
            : base(BuildMessage(message, requiredTier), statusCode, code, requestUri, body, headers)
        {
            RequiredTier = requiredTier;
        }

        /// <summary>
        /// The lowest tier that unlocks the endpoint (for example <c>PRO</c> or
        /// <c>ULTRA</c>), or <c>null</c> when it could not be inferred.
        /// </summary>
        public string? RequiredTier { get; }

        private static string BuildMessage(string message, string? requiredTier) =>
            requiredTier is null
                ? message
                : message + " — this endpoint requires the " + requiredTier + " tier. See https://livetennisapi.com/#pricing";
    }

    /// <summary><c>404</c> — no such resource, or no data for it yet.</summary>
    public sealed class NotFoundException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="NotFoundException"/> class.</summary>
        public NotFoundException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers)
            : base(message, statusCode, code, requestUri, body, headers)
        {
        }
    }

    /// <summary>
    /// <c>409</c> — the request conflicts with current state (for example
    /// <c>webhook_limit</c>: 3 webhooks already registered; delete one first).
    /// </summary>
    public sealed class ConflictException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="ConflictException"/> class.</summary>
        public ConflictException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers)
            : base(message, statusCode, code, requestUri, body, headers)
        {
        }
    }

    /// <summary><c>429</c> — the tier's rate-limit window was exceeded.</summary>
    /// <remarks>
    /// Two window scopes share this status. The per-minute window clears within
    /// seconds — honour <see cref="RetryAfterSeconds"/>. The daily window
    /// (<c>scope: "day"</c> in the body) clears at <see cref="ResetsAt"/>, an
    /// absolute instant derived from the account's local midnight — not a fixed
    /// UTC time, so read the instant rather than assuming one. A
    /// chronic-over-cap block is the separate
    /// <see cref="AbuseThrottledException"/> subtype.
    /// </remarks>
    public class RateLimitedException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="RateLimitedException"/> class.</summary>
        public RateLimitedException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers, double? retryAfterSeconds, string? resetsAt = null)
            : base(BuildMessage(message, retryAfterSeconds), statusCode, code, requestUri, body, headers)
        {
            RetryAfterSeconds = retryAfterSeconds;
            ResetsAt = resetsAt;
        }

        /// <summary>
        /// Seconds the API asked you to wait, parsed from the <c>Retry-After</c>
        /// header, or <c>null</c> when the header was absent or unparseable.
        /// </summary>
        public double? RetryAfterSeconds { get; }

        /// <summary>
        /// When the <b>daily</b> quota window resets, as the ISO 8601 UTC instant
        /// from the response body (<c>resets_at</c>), or <c>null</c> on a
        /// per-minute 429. The instant is derived from the account's local
        /// midnight — never assume a fixed UTC reset time.
        /// </summary>
        public string? ResetsAt { get; }

        /// <summary><see cref="ResetsAt"/> parsed, or <c>null</c> when absent or unparseable.</summary>
        public DateTimeOffset? ResetsAtTime =>
            DateTimeOffset.TryParse(
                ResetsAt,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed)
                ? parsed
                : (DateTimeOffset?)null;

        private static string BuildMessage(string message, double? retryAfter) =>
            retryAfter is null
                ? message
                : message + " — retry after " + retryAfter.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "s";
    }

    /// <summary>
    /// <c>429</c> with code <c>abuse_throttled</c> — the key was blocked for
    /// chronically exceeding its caps (typically for 24 hours).
    /// </summary>
    /// <remarks>
    /// This is not the ordinary rate-limit window: retrying does not help until
    /// <see cref="RetryAt"/>. It usually means a retry loop is hammering the API
    /// after 429s instead of backing off — fix the loop rather than rescheduling
    /// the request.
    /// </remarks>
    public sealed class AbuseThrottledException : RateLimitedException
    {
        /// <summary>Initializes a new instance of the <see cref="AbuseThrottledException"/> class.</summary>
        public AbuseThrottledException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers, double? retryAfterSeconds, long? retryAtEpoch)
            : base(message, statusCode, code, requestUri, body, headers, retryAfterSeconds)
        {
            RetryAtEpoch = retryAtEpoch;
        }

        /// <summary>
        /// When the block lifts, as Unix seconds (the body's
        /// <c>retry_at_epoch</c>), or <c>null</c> when the body carried none.
        /// </summary>
        public long? RetryAtEpoch { get; }

        /// <summary><see cref="RetryAtEpoch"/> as a <see cref="DateTimeOffset"/>, or <c>null</c>.</summary>
        public DateTimeOffset? RetryAt =>
            RetryAtEpoch.HasValue ? DateTimeOffset.FromUnixTimeSeconds(RetryAtEpoch.Value) : (DateTimeOffset?)null;
    }

    /// <summary><c>5xx</c> — the API failed to serve the request.</summary>
    public sealed class ServerException : LiveTennisApiException
    {
        /// <summary>Initializes a new instance of the <see cref="ServerException"/> class.</summary>
        public ServerException(string message, int statusCode, string? code, string? requestUri, string? body, IReadOnlyDictionary<string, string>? headers)
            : base(message, statusCode, code, requestUri, body, headers)
        {
        }
    }

    /// <summary>Maps an HTTP status code to the exception type that represents it.</summary>
    internal static class ExceptionFactory
    {
        public static LiveTennisApiException ForStatus(
            HttpStatusCode statusCode,
            string message,
            string? code,
            string? requestUri,
            string? body,
            IReadOnlyDictionary<string, string> headers,
            string? requiredTier,
            double? retryAfterSeconds,
            string? resetsAt = null,
            long? retryAtEpoch = null)
        {
            int status = (int)statusCode;
            switch (status)
            {
                case 400:
                    return new BadRequestException(message, status, code, requestUri, body, headers);
                case 401:
                    return new UnauthorizedException(message, status, code, requestUri, body, headers);
                case 403:
                    return new UpgradeRequiredException(message, status, code, requestUri, body, headers, requiredTier);
                case 404:
                    return new NotFoundException(message, status, code, requestUri, body, headers);
                case 409:
                    return new ConflictException(message, status, code, requestUri, body, headers);
                case 429:
                    return code == "abuse_throttled"
                        ? new AbuseThrottledException(message, status, code, requestUri, body, headers, retryAfterSeconds, retryAtEpoch)
                        : new RateLimitedException(message, status, code, requestUri, body, headers, retryAfterSeconds, resetsAt);
                default:
                    return status >= 500
                        ? new ServerException(message, status, code, requestUri, body, headers)
                        : new LiveTennisApiException(message, status, code, requestUri, body, headers);
            }
        }
    }
}
