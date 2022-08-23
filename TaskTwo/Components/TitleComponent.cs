using Microsoft.AspNetCore.Mvc;

using System.Xml.Linq;
using TaskTwo.Models;

namespace TaskTwo.Components
{
    public class TitleComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = new List<string>();

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

                    var chTitle = doc.Descendants("title").First().Value;
                    result.Add(chTitle);
                }
            }



            return View(result);
        }

        private DateTime ParseDate(string date)
        {
            DateTime result;
            if (DateTime.TryParse(date, out result))
                return result;
            else
                return DateTime.MinValue;
        }
    }
}
