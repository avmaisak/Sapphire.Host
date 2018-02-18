using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http.Extensions;

namespace Sapphire.Host {
	public class JsonContent : StringContent {
		public JsonContent(object obj) :
			base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json") { }
	}
	public class SapphireWebClient {
		private readonly HttpClient _httpClient;
		public SapphireWebClient(string host) {
			_httpClient = new HttpClient { BaseAddress = new Uri(host) };
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		//async Task<HttpResponseMessage>
		public async Task<string> Post(string controller, string action, object obj) {
			var json = new JsonContent(obj);
			//var objStr = new StringContent(json, Encoding.UTF8, "application/json");

			using (_httpClient) {
				var response = await _httpClient.PostAsync($"{controller}/{action}", json);
				if (response.IsSuccessStatusCode) {
					var res = await response.Content.ReadAsStringAsync();
					return res;
				}

				return string.Empty;
				//var uri = new Uri($"{_host}{controller}/{action}");
				//return await client.PostAsync($"{controller}/{action}", objStr);
				//client.DownloadStringAsync(uri);
			}
		}
	}
}
