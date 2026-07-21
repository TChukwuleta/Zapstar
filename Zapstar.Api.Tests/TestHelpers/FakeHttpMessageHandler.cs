using System.Net;

namespace Zapstar.Api.Tests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string UrlContains, HttpStatusCode Status, string Body)> _routes = [];

    public FakeHttpMessageHandler When(string urlContains, HttpStatusCode status, string body)
    {
        _routes.Add((urlContains, status, body));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var url = request.RequestUri!.ToString();
        var match = _routes.FirstOrDefault(r => url.Contains(r.UrlContains, StringComparison.OrdinalIgnoreCase));

        if (match.UrlContains is null)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found")
            });
        }

        return Task.FromResult(new HttpResponseMessage(match.Status)
        {
            Content = new StringContent(match.Body)
        });
    }
}