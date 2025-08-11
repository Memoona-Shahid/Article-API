using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApi1.Data;
using WebApi1.Model;

namespace WebApi1.Controllers
{
    [Authorize]
    [ApiController]
    //[Route("api/[controller]")]
    [Route("")]

    public class ArticlesApiController : ControllerBase
    {
        private readonly ArticleDbContext _context;
        private readonly IMemoryCache _memoryCache;


        //CONSTRUCTOR TO INJECT DEPENDENCIES
        public ArticlesApiController(ArticleDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        //GET METHOD TO GET ALL ARTICLES
        [AllowAnonymous]
        [HttpGet("GETALLARTICLE")]
        public async Task<ActionResult<IEnumerable<ArticleData>>> GetAll()
        {
            if (_memoryCache.TryGetValue("ArticleData", out List<ArticleData> cachedArticles))
            {
                return Ok(cachedArticles);
            }

            var articles = await _context.ArticleData.Where(a => !a.Del).ToListAsync();

            _memoryCache.Set("ArticleData", articles, TimeSpan.FromMinutes(5));
            return Ok(articles);
        }


        //GET METHOD TO GET ALL ARTICLES BY PAGE NUMBER
        [HttpGet("api/articles")]
        public async Task<ActionResult<IEnumerable<ArticleData>>> Get(int page = 1)
        {
            int pagesize = 10;
            var totalcount = await _context.ArticleData.CountAsync(a => !a.Del);
            var totalpages = (int)Math.Ceiling((double)totalcount / pagesize);
            var articles = await _context.ArticleData
                .Where(a => !a.Del)
                .Skip((page - 1) * pagesize)

                .Take(pagesize)
                .ToListAsync();
            return articles.Count == 0
                ? NotFound("No articles found")
                : Ok(new { TotalPages = totalpages, Articles = articles });
        }


        //GET METHOD TO GET ARTICLE BY ID
        [AllowAnonymous]
        [HttpGet("GETARTICLEBY{id}")]
        public async Task<ActionResult<ArticleData>> GetArticle(int id)
        {
            if (_memoryCache.TryGetValue($"ArticleData", out List<ArticleData> cachedArticles))
            {
                return Ok(cachedArticles.Where(a => !a.Del && a.Id == id));
            }

            var article = await _context.ArticleData.Where(a => !a.Del && a.Id == id).FirstOrDefaultAsync();

            if (article == null)
                return NotFound();

           // _memoryCache.Set($"ArticleData{id}", article, TimeSpan.FromMinutes(5));
            return Ok(article);
        }


        //POST METHOD TO ADD ARTICLE
        [HttpPost("ADDNEWARTICLE")]
        [AllowAnonymous]
        public async Task<ActionResult<ArticleData>> CreateArticle([FromBody] ArticleData article)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool exists = await _context.ArticleData.AnyAsync(a => a.Title == article.Title);
            if (exists)
            {
                return BadRequest("Article already exists");
            }

            
            article.InsertedAt = DateTime.UtcNow;
            article.Del = false;

            _context.ArticleData.Add(article);
            await _context.SaveChangesAsync();

            return Ok(article);
        }


        //PUT METHOD TO UPDATE ARTICLE
        [HttpPut("UPDATEARTICLE")]
        public async Task<IActionResult> UpdateArticle(int id, ArticleData updated)
        {
            if (id != updated.Id)
                return BadRequest("ID not matched");

            _context.Entry(updated).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }


        //DELETE METHOD TO SOFT DELETE ARTICLE
        [HttpDelete("DELETEARTICLE")]
        public async Task<IActionResult> SoftDeleteArticle(int id)
        {
            var article = await _context.ArticleData.FindAsync(id);
            if (article == null)
                return NotFound();

            article.Del = true;  // 1
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
