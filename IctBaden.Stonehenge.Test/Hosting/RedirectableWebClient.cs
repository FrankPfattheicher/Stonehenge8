using System;
using System.Net.Http;

namespace IctBaden.Stonehenge.Test.Hosting
{
    public class RedirectableWebClient : IDisposable
    {
        private readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = true });

        public void Dispose()
        {
            _client.Dispose();
        }
        
        
        public string DownloadString(string address)
        {
            for (var redirect = 0; redirect < 10; redirect++)
            {
                var response = _client.GetAsync(address).Result;

                var redirectUrl = response.Headers.Location;
                if (redirectUrl == null)
                {
                    // address = response.Headers.ResponseUri.ToString();
                }

                response.Dispose();

                if (redirectUrl == null)
                    break;

                var newAddress = new Uri(new Uri(address), redirectUrl).AbsoluteUri;
                if (newAddress == address)
                    break;

                address = newAddress;
            }

            return _client.GetStringAsync(address).Result;
        }

    }
}
