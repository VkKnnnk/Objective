using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PriceService.Models
{
    class GetRequest
    {
        private readonly HttpClient _client;
        private readonly string _url;
        public GetRequest(string url)
        {
            _client = new HttpClient();
            _url = url;
        }
        public async Task<string> RunRequest()
        {
            using (HttpResponseMessage response = await _client.GetAsync(_url))
            {
                if (response is not null && response.IsSuccessStatusCode)
                {
                    var source = await response.Content.ReadAsStringAsync();
                    _client.Dispose();
                    return source;
                }
                else
                    throw new ArgumentNullException("empty response");
            }
            throw new InvalidOperationException("bad url");
        }
    }
}
