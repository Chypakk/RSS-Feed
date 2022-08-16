using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TaskTwo.Models;

namespace TaskTwo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private MyConfig myConfig;

        public HomeController(ILogger<HomeController> logger, IOptions<MyConfig> settings)
        {
            _logger = logger;
            myConfig = settings.Value;
        }

        public async Task<IActionResult> Index()
        {
            var feed = new List<FeedItem>();
            ViewBag.RefreshTime = myConfig.refreshTime;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(myConfig.link);
                var responseMessage = await client.GetAsync(myConfig.link);
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                XDocument doc = XDocument.Parse(responseString);
                Regex regex = new Regex(@"(<[\a-zA-Z]+>)+(&[a-z]+;)*(Читать дальше? &rarr;?)?(Читать далее?)?", RegexOptions.Compiled);
                
                var feedItems = from item in doc.Root.Descendants().First(a => a.Name.LocalName == "channel").Elements().Where(a => a.Name.LocalName == "item")
                                select new FeedItem
                                {
                                    Description = regex.Replace(item.Elements().First(a => a.Name.LocalName == "description").Value, ""),
                                    Link = item.Elements().First(a => a.Name.LocalName == "link").Value,
                                    pubDate = ParseDate(item.Elements().First(a => a.Name.LocalName == "pubDate").Value),
                                    Title = item.Elements().First(a => a.Name.LocalName == "title").Value
                                };
                feed = feedItems.ToList();
            }
            return View(feed);

        }

        private DateTime ParseDate(string date)
        {
            DateTime result;
            if (DateTime.TryParse(date, out result))
                return result;
            else
                return DateTime.MinValue;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}