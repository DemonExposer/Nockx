using System;
using System.Net.Http;
using System.Text;

namespace SecureChat.util; 

public static class Https {
	public class Response {
		public string Body;
		public bool IsSuccessful;
	}
	
	public static Response Get(string endpoint) {
		using HttpClient client = new ();
		HttpRequestMessage request = new () {
			RequestUri = new Uri(endpoint),
			Method = HttpMethod.Get
		};
		
		HttpResponseMessage response = client.SendAsync(request).Result;

		return new Response { IsSuccessful = response.IsSuccessStatusCode, Body = response.Content.ReadAsStringAsync().Result };
	}

	public static Response Post(string endpoint, string postData) {
		using HttpClient client = new ();
		HttpRequestMessage request = new () {
			RequestUri = new Uri(endpoint),
			Method = HttpMethod.Post,
			Content = new StringContent(postData, Encoding.UTF8, "application/json")
		};
		
		HttpResponseMessage response = client.SendAsync(request).Result;

		return new Response { IsSuccessful = response.IsSuccessStatusCode, Body = response.Content.ReadAsStringAsync().Result };
	}
}