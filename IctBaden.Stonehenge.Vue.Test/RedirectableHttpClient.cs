using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

// ReSharper disable ConvertToUsingDeclaration

namespace IctBaden.Stonehenge.Vue.Test;

public class RedirectableHttpClient : HttpClient
{
    // ReSharper disable once MemberCanBePrivate.Global
    public string? SessionId { get; set; }

    public async Task<string> DownloadStringWithSession(string address)
    {
        if (SessionId == null)
        {
            await DownloadString(address);
        }

        var url = new UriBuilder(address);
        var query = HttpUtility.ParseQueryString(url.Query);
        query["stonehenge-id"] = SessionId;
        url.Query = query.ToString() ?? string.Empty;
        return await DownloadString(url.ToString());
    }

    public async Task<string> DownloadString(string address)
    {
        for (var redirect = 0; redirect < 10; redirect++)
        {
            var response = await GetAsync(address);

            var redirectUrl = response.Headers.Location;
            string? redirectAddr = null;
            if (redirectUrl == null)
            {
                redirectAddr = response.RequestMessage?.RequestUri?.ToString();
            }
            if (redirectAddr != null)
            {
                var match = new Regex("stonehenge-id=([a-f0-9A-F]+)", RegexOptions.RightToLeft)
                    .Match(redirectAddr);
                if (match.Success)
                {
                    SessionId = match.Groups[1].Value;
                }
            }

            var body = response.Content.ReadAsStringAsync().Result;
            response.Dispose();

            if (redirectUrl == null)
            {
                return body;
            }

            var newAddress = new Uri(response.RequestMessage!.RequestUri!, redirectUrl).AbsoluteUri;
            if (newAddress == address)
                break;

            address = newAddress;
        }

        return string.Empty;
    }

    public async Task<string> Post(string address, string data)
    {
        DefaultRequestHeaders.Add("X-Stonehenge-Id", SessionId);
        var response = await PostAsync(address, new StringContent(data));
        var body = response.Content.ReadAsStringAsync().Result;
        response.Dispose();
        return body;
    }

    public async Task<string> Put(string address, string data)
    {
        var response = await PutAsync(address, new StringContent(data));
        var body = response.Content.ReadAsStringAsync().Result;
        response.Dispose();
        return body;
    }
    
    public async Task<string> Delete(string address)
    {
        var response = await DeleteAsync(address);
        var body = response.Content.ReadAsStringAsync().Result;
        response.Dispose();
        return body;
    }
    
}