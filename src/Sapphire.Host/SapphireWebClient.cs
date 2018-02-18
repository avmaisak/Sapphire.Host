using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sapphire.Host {
	public class SapphireWebClient {
		private readonly HttpClient _httpClient;
		public SapphireWebClient(string host) {
			_httpClient = new HttpClient { BaseAddress = new Uri(host) };
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}
		public async Task<string> Post(string controller, string action, object obj) {
			var json = new JsonContent(obj);
			var response = await _httpClient.PostAsync($"{controller}/{action}", json);
			if (response.IsSuccessStatusCode) {
				var res = await response.Content.ReadAsStringAsync();
				return res;
			}
			return string.Empty;
		}
	}
}