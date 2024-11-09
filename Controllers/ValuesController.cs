using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetWithElasticSearch.Context;
using Newtonsoft.Json.Linq;

namespace NetWithElasticSearch.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public sealed class ValuesController : ControllerBase
    {
        AppDbContext _context = new();
        [HttpGet("[action]")]
        public async Task<IActionResult> CreateData(CancellationToken cancellationToken)
        {

            var random = new Random();
            IList<Travel> travels = new List<Travel>();

            var words = new List<string>();
            for (int i = 0; i < 10000; i++)
            {
                var title = new string(Enumerable.Repeat("abcdefghjklmnoprstuvyz", 5).Select(s => s[random.Next(s.Length)]).ToArray());

                for (int j = 0; j < 500; j++)
                {
                    words.Add(new string(Enumerable.Repeat("abcdefghjklmnoprstuvyz", 5).Select(s => s[random.Next(s.Length)]).ToArray()));
                }

                var description = string.Join(" ", words);
                var travel = new Travel()
                {
                    Title = title,
                    Description = description,
                };

                travels.Add(travel);
            }

            await _context.Set<Travel>().AddRangeAsync(travels);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }

        [HttpGet("[action]/{description}")]
        public async Task<IActionResult> GetDataList(string value)
        {
            IList<Travel> travels = await _context.Set<Travel>().Where(p => p.Description.Contains(value)).AsNoTracking().ToListAsync();
            return Ok(travels);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> SyncToELasticsearch()
        {

            var settings = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            var client = new ElasticLowLevelClient(settings);

            List<Travel> travels = await _context.Travels.ToListAsync();
            
            var tasks = new List<Task>();

            foreach (Travel travel in travels)
            {
                tasks.Add(client.IndexAsync<StringResponse>("travels", travel.Id.ToString(), PostData.Serializable(
                    new
                    {
                        travel.Id,
                        travel.Title,
                        travel.Description,
                    })));
            }

            await Task.WhenAll(tasks);

            return Ok();
        }

        [HttpGet("[action]/{description}")]
        public async Task<IActionResult> GetDataListWiithElasticsearch(string value)
        {
            var settings = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            var client = new ElasticLowLevelClient(settings);

            var response = await client.SearchAsync<StringResponse>("travels",
                PostData.Serializable(
                    new
                    {
                        query = new
                        {
                            wildcard = new
                            {
                                Description = new { value = $"*{value}*" }
                            }
                        }
                    }));
            var results = JObject.Parse(response.Body);

            var hits = results["hits"]["hits"].ToObject<List<JObject>>();

            List<Travel> travels = new();

            foreach (var  hit in hits)
            {
                travels.Add(hit["_source"].ToObject<Travel>());
            }

            return Ok(travels.Take(10));
        }
    }
}
