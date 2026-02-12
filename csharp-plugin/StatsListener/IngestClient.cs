using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace StatsListener
{
    public class IngestClient
    {
        private static readonly HttpClient http = new HttpClient();
        private readonly string ingestUrl;
        private readonly string apiKey;

        public IngestClient(string ingestUrl, string apiKey)
        {
            this.ingestUrl = ingestUrl;
            this.apiKey = apiKey;
        }


        public async Task SendManyAsync(IEnumerable<object> payloads, int maxConcurrency = 12)
        {
            using var sem = new SemaphoreSlim(maxConcurrency);
            var tasks = payloads.Select(async payload =>
            {
                await sem.WaitAsync();
                try { await SendAsync(payload); }
                finally { sem.Release(); }
            });

            await Task.WhenAll(tasks);
        }


        public async Task SendAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, ingestUrl);
            req.Headers.Add("X-API-KEY", apiKey);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await http.SendAsync(req);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Ingest] HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine($"[Ingest] Response body {body}");
                Console.WriteLine($"[Ingest] Sent JSON {json}");
                throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
            }
        }
    }
}
