using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace PtcgpTracker.Api.Tests.Infrastructure;

/// <summary>
/// Stands in for raw.githubusercontent.com when the image downloader runs in tests: serves a tiny
/// pack list and a few bytes for every image, and records what was asked for.
/// </summary>
internal class ImageMirrorStubHandler : HttpMessageHandler
{
    public static readonly byte[] ImageBytes = [1, 2, 3, 4];

    private const string ExpansionsJson = """
        [
          { "id": "a1", "packs": [ { "id": "a1-charizard", "image": "https://example.invalid/a1-charizard.webp" } ] },
          { "id": "pa", "packs": [ { "id": "pa-promov1", "image": null } ] }
        ]
        """;

    public ConcurrentQueue<string> Requests { get; } = new();

    /// <summary>Any URL ending with one of these gets a 404.</summary>
    public ConcurrentBag<string> Missing { get; } = new();

    public bool FailPackList { get; set; }

    /// <summary>While set, image requests wait for it — lets a test hold a download open.</summary>
    public TaskCompletionSource? Gate { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        Requests.Enqueue(url);

        if (url.EndsWith("/data/v5/expansions.json", StringComparison.Ordinal))
        {
            return FailPackList
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ExpansionsJson, Encoding.UTF8, "application/json") };
        }

        if (Gate is { } gate)
        {
            await gate.Task.WaitAsync(cancellationToken);
        }

        if (Missing.Any(suffix => url.EndsWith(suffix, StringComparison.Ordinal)))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(ImageBytes) };
        response.Content.Headers.ContentType = new("image/webp");
        return response;
    }
}
