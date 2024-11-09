using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetWithElasticSearch.Context;

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
        public async Task<IActionResult> GetDataList(string description)
        {
            IList<Travel> travels = await _context.Set<Travel>().Where(p => p.Description.Contains(description)).AsNoTracking().ToListAsync();
            return Ok(travels);
        }
    }
}
