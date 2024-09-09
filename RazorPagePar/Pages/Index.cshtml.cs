using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagePar.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ElasticsearchClient _elasticsearchClient;

    public class Person
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

    public IndexModel(ElasticsearchClient elasticsearchClient)
    {
        _elasticsearchClient = elasticsearchClient;
    }
    public async Task OnGetAsync()
    {
        // TODO read from secrets
        var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
            .Authentication(new BasicAuthentication("elastic", "Password1!"));

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