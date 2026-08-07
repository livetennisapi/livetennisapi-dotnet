using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveTennisApi.Tests
{
    /// <summary>A single request as the stub handler observed it.</summary>
    internal sealed class CapturedRequest
    {
        public CapturedRequest(HttpRequestMessage request)
        {
            Uri = request.RequestUri!;
            Method = request.Method;
            Headers = request.Headers
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);
            Body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        public Uri Uri { get; }

        public HttpMethod Method { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }

        public string? Body { get; }

        public string Query => Uri.Query;
    }

    /// <summary>
    /// A test double for <see cref="HttpMessageHandler"/> that replays a queue of
    /// canned responses and records every request it saw. When the queue holds a
    /// single responder, it is reused for all requests; otherwise each request
    /// dequeues the next one (so retry sequences can be scripted).
    /// </summary>
    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<CapturedRequest, HttpResponseMessage>> _responders;

        public StubHttpMessageHandler(params Func<CapturedRequest, HttpResponseMessage>[] responders)
        {
            if (responders is null || responders.Length == 0)
            {
                throw new ArgumentException("At least one responder is required.", nameof(responders));
            }

            _responders = new Queue<Func<CapturedRequest, HttpResponseMessage>>(responders);
        }

        public List<CapturedRequest> Requests { get; } = new List<CapturedRequest>();

        public int CallCount => Requests.Count;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var captured = new CapturedRequest(request);
            Requests.Add(captured);
            var responder = _responders.Count > 1 ? _responders.Dequeue() : _responders.Peek();
            return Task.FromResult(responder(captured));
        }
    }

    /// <summary>Response builders and embedded-fixture loading.</summary>
    internal static class TestSupport
    {
        public const string TestKey = "test_key_not_a_real_credential";

        public static HttpResponseMessage Json(HttpStatusCode status, string body, params (string Name, string Value)[] headers)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            foreach (var (name, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return response;
        }

        public static HttpResponseMessage Ok(string body) => Json(HttpStatusCode.OK, body);

        /// <summary>Loads an embedded fixture by file name (for example <c>matches_live.json</c>).</summary>
        public static string Fixture(string name)
        {
            var assembly = typeof(TestSupport).Assembly;
            var suffix = "Fixtures." + name;
            var resource = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal));

            if (resource is null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                throw new FileNotFoundException("Fixture '" + name + "' not embedded. Available: " + available);
            }

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>Builds a client whose transport is the given stub handler.</summary>
        public static LiveTennisApiClient ClientOver(StubHttpMessageHandler handler, LiveTennisApiClientOptions? options = null)
        {
            // Supplying the HttpClient exercises the IHttpClientFactory-friendly
            // constructor; the client will not dispose it.
            var httpClient = new HttpClient(handler);
            return new LiveTennisApiClient(httpClient, TestKey, options);
        }
    }
}
