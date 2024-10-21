using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using STOdev.Models;
using STOdev.ViewModels;
using System.Diagnostics;

namespace STOdev.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		ElasticsearchClient _client;

		public HomeController(ILogger<HomeController> logger, ElasticsearchClient client)
		{
			_logger = logger;
			_client = client;
		}

		public async Task<IActionResult> Index()
		{
			var model = new SearchVM { Results = new List<string>() };

			var url = "https://www.sozcu.com.tr/";
			var links = new List<string>();

			// Linkleri �ekme ve Elasticsearch'a ekleme i�lemi
			var web = new HtmlWeb();
			var doc = await web.LoadFromWebAsync(url);

			foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
			{
				var hrefValue = link.GetAttributeValue("href", string.Empty);
				if (!string.IsNullOrEmpty(hrefValue))
				{
					links.Add(hrefValue);
				}
			}

			foreach (var link in links)
			{
				var response = await _client.IndexAsync(new { Url = link }, i => i.Index("links"));
			}

			// Elasticsearch'tan linkleri �ekme
			SearchRequest searchRequest = new("links")
			{
				Size = 100 // defaultta 10 tane geliyor diye bunu yapt�m
			};

			SearchResponse<SozcuData> searchResponse = await _client.SearchAsync<SozcuData>(searchRequest);


			// Dinamik sonu�lar� string listesine d�n��t�rme
			model.Results = searchResponse.Documents.Select(d => d.Url).ToList();

			// Linkleri g�r�n�mde g�sterme
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Search(SearchVM model)
		{
			if (!string.IsNullOrEmpty(model.SearchTerm))
			{

				SearchRequest searchRequest = new("links")
				{
					Size = 100,
					Query = new FuzzyQuery(new Field("url"))
					{
						Value = model.SearchTerm
					}
				};

				SearchResponse<SozcuData> searchResponse = await _client.SearchAsync<SozcuData>(searchRequest);

				// Sonu�lar� modelin Results listesine ekleme
				model.Results = searchResponse.Documents.Select(d => d.Url).ToList();
			}
			else
			{
				// Elasticsearch'tan linkleri �ekme
				SearchRequest searchRequest = new("links")
				{
					Size = 100 // defaultta 10 tane geliyor diye bunu yapt�m
				};

				SearchResponse<SozcuData> searchResponse = await _client.SearchAsync<SozcuData>(searchRequest);


				// Dinamik sonu�lar� string listesine d�n��t�rme
				model.Results = searchResponse.Documents.Select(d => d.Url).ToList();
			}

			// Do�ru model tipini kullanarak geri d�n
			return View("Index", model);
		}

	}
}
