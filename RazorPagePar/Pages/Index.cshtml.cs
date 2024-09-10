using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace RazorPagePar.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ElasticsearchClient _elasticsearchClient;
    private readonly IConfiguration _configuration;

    public class Person
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

    public IndexModel(ElasticsearchClient elasticsearchClient, IConfiguration configuration)
    {
        _elasticsearchClient = elasticsearchClient;
        _configuration = configuration;
    }
    public async Task OnGetAsync()
    {
        var settings = new ElasticsearchClientSettings(new Uri(_configuration["ElasearchUrl"]!))
            .Authentication(new BasicAuthentication(_configuration["ElasticsearchUserName"]!, 
                _configuration["ElasearchPassword"]!));

        var client = new ElasticsearchClient(settings);

        var exist = await _elasticsearchClient.Indices.ExistsAsync("people");
        if (exist.Exists)
        {
            await _elasticsearchClient.Indices.DeleteAsync("people");
        }

        var person = new Person
        {
            FirstName = "Alireza",
            LastName = "Baloochi"
        };

        var response = await _elasticsearchClient.IndexAsync<Person>(person, "people", "1");

        var response2 = await _elasticsearchClient.GetAsync<Person>("people", "1");
    }
}