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

        public async Task<IActionResult> Index(string rss)
        {
            var feed = new List<FeedItem>();
            ViewBag.RefreshTime = myConfig.refreshTime;
            if (rss != null)
            {
                ViewBag.RssFeed = rss;
            }

            var config = XDocument.Load("config.xml");
            var links = config.Root.Element("RSS").Element("links");
            var countLinks = links.Elements().Count();

            for (int i = 0; i < countLinks; i++)
            {
                using (var client = new HttpClient())
                {

                    var link = links.Element($"link{i}").Value;
                    client.BaseAddress = new Uri(link);
                    var responseMessage = await client.GetAsync(link);
                    var responseString = await responseMessage.Content.ReadAsStringAsync();

                    XDocument doc = XDocument.Parse(responseString);

                    Regex regex = new Regex(@"(<[\a-zA-Z]+>)+(&[a-z]+;)*(Читать дальше? &rarr;?)?(Читать далее?)?", RegexOptions.Compiled);
                    Regex regexSpace = new Regex(@"(&nbsp;)+", RegexOptions.Compiled);
                    Regex regexTags = new Regex(@"<+(\w+\W+)+>+", RegexOptions.Compiled);

                    if (rss != null && doc.Root.Descendants("title").First().Value == rss)
                    {
                        var feedItem = from item in doc.Root.Descendants().First(a => a.Name.LocalName == "channel").Elements().Where(a => a.Name.LocalName == "item")
                                       select new FeedItem
                                       {
                                           Description = regexSpace.Replace(regexTags.Replace(regex.Replace(item.Elements().First(a => a.Name.LocalName == "description").Value, ""), ""), " "),
                                           Link = item.Elements().First(a => a.Name.LocalName == "link").Value,
                                           pubDate = ParseDate(item.Elements().First(a => a.Name.LocalName == "pubDate").Value),
                                           Title = item.Elements().First(a => a.Name.LocalName == "title").Value,
                                       };
                        feed.AddRange(feedItem.ToList());
                        break;
                    }
                    else if (rss == null)
                    {
                        var feedItems = from item in doc.Root.Descendants().First(a => a.Name.LocalName == "channel").Elements().Where(a => a.Name.LocalName == "item")
                                        select new FeedItem
                                        {
                                            Description = regexSpace.Replace(regexTags.Replace(regex.Replace(item.Elements().First(a => a.Name.LocalName == "description").Value, ""), ""), " "),
                                            Link = item.Elements().First(a => a.Name.LocalName == "link").Value,
                                            pubDate = ParseDate(item.Elements().First(a => a.Name.LocalName == "pubDate").Value),
                                            Title = item.Elements().First(a => a.Name.LocalName == "title").Value,
                                        };
                        feed.AddRange(feedItems.ToList());
                    }


                }
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

        public IActionResult AddNewLink(string link)
        {
            var config = XDocument.Load("config.xml");
            int count = config.Root.Element("RSS").Element("links").Elements().Count();
            var newLink = config.Descendants("links").First();
            newLink.Add(new XElement($"link{count}", link));
            config.Save("config.xml");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DeleteLink(string rssTitle)
        {
            XDocument config = XDocument.Load("config.xml");

            var links = config.Root.Element("RSS").Element("links");
            var countLinks = links.Elements().Count();

            for (int i = 0; i < countLinks; i++)
            {
                using (var client = new HttpClient())
                {

                    var link = links.Element($"link{i}").Value;
                    client.BaseAddress = new Uri(link);
                    var responseMessage = await client.GetAsync(link);
                    var responseString = await responseMessage.Content.ReadAsStringAsync();

                    XDocument doc = XDocument.Parse(responseString);

                    if (doc.Root.Descendants("title").First().Value == rssTitle)
                    {
                        var deletedEl = config.Root.Element("RSS").Element("links").Element($"link{i}");
                        deletedEl.Remove();

                        var newLinks = links.Elements();
                        int j = 0;
                        foreach (var item in newLinks)
                        {
                            item.Name = $"link{j}";
                            j++;
                        }

                        config.Save("config.xml");
                        break;
                    }


                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult SetTimeRefresh(int refTime)
        {
            XDocument config = XDocument.Load("config.xml");
            var newTime = config.Descendants("refreshTime").First();
            newTime.ReplaceWith(
                new XElement("refreshTime", refTime)
                );
            myConfig.refreshTime = refTime.ToString();
            config.Save("config.xml");
            return RedirectToAction("Index");
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