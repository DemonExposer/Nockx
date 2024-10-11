using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SecureChat.util; 

public static class Https {
	public class Header {
		public string Name;
		public string Value;
	}
    
	public class Response {
		public string Body;
		public bool IsSuccessful;
	}
	
	public static Response Get(string endpoint, Header[]? headers = null) {
		using HttpClient client = new ();
		HttpRequestMessage request = new () {
			RequestUri = new Uri(endpoint),
			Method = HttpMethod.Get
		};
		
		if (headers != null)
			foreach (Header header in headers)
				request.Headers.Add(header.Name, header.Value);
		
		HttpResponseMessage response;
		try {
			response = client.SendAsync(request).Result;
		} catch (Exception) {
			return new Response { IsSuccessful = false, Body = "" };
		} 

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

	public static Response Delete(string endpoint, string body) {
		using HttpClient client = new ();
		HttpRequestMessage request = new () {
			RequestUri = new Uri(endpoint),
			Method = HttpMethod.Delete,
			Content = new StringContent(body, Encoding.UTF8, "application/json")
		};

		HttpResponseMessage response = client.SendAsync(request).Result;

		return new Response { IsSuccessful = response.IsSuccessStatusCode, Body = response.Content.ReadAsStringAsync().Result };
	}
}