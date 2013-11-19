using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ELearningCrawler
{
    class Crawler
    {
        CookieContainer cookies;

        public void DownloadAllInFolder(string destFolder)
        {
            ParseCourses();
        }

        public async Task LoginToELeraning(string user, string password)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.CreateHttp("https://elearning.fhws.de/login/index.php");
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            cookies = new CookieContainer();
            req.CookieContainer = cookies;

            using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
            {
                sw.Write(string.Format("username={0}&password={1}", user, password));
            }

            using (HttpWebResponse response = (HttpWebResponse)(await req.GetResponseAsync()))
            {
                HttpWebRequest req2 = (HttpWebRequest)WebRequest.CreateHttp(response.ResponseUri);
                req2.Method = "GET";
                req2.CookieContainer = cookies;

                using (HttpWebResponse response2 = (HttpWebResponse)(await req2.GetResponseAsync()))
                {

                }
            }
        }

        private WebClient CreateWebclient()
        {
            CookieAwareWebClient wc = new CookieAwareWebClient();
            wc.CookieContainer = cookies;
            return wc;
        }

        private async Task<HtmlDocument> HtmlDocumentFromUrl(string url)
        {
            using (WebClient client = CreateWebclient())
            {
                string htmlString = await client.DownloadStringTaskAsync(url);
                HtmlDocument doc = new HtmlDocument();

                using (StringReader sr = new StringReader(htmlString))
                {
                    doc.Load(sr);

                    return doc;
                }
            }
        }

        private async void ParseCourses()
        {
            HtmlDocument doc = await HtmlDocumentFromUrl("https://elearning.fhws.de/my/");

            var courses = doc.DocumentNode.SelectNodes("//div[@class='box coursebox']/h3/a");

            if (courses == null || courses.Count == 0)
                return;

            foreach (HtmlNode node in courses)
            {
                if (node.Attributes["title"] == null || string.IsNullOrEmpty(node.Attributes["title"].Value))
                    continue;
                if (node.Attributes["href"] == null || string.IsNullOrEmpty(node.Attributes["href"].Value))
                    continue;

                string courseName = node.Attributes["title"].Value;
                string courseLink = node.Attributes["href"].Value;

                Console.WriteLine("Found course '{0}' follow link: {1}", courseName, node.Attributes["href"].Value);

                HtmlDocument course = await HtmlDocumentFromUrl(courseLink);

                string destFolder = courseName; // TODO command line parameter
                Directory.CreateDirectory(destFolder);

                ParseCourse(course, destFolder);
            }
        }

        private async void ParseCourse(HtmlDocument course, string dest)
        {
            var sections = course.DocumentNode.SelectNodes("//li[starts-with(@id, 'section-')]").Where(n => n.Id != "section-0");

            if (sections == null || sections.Count() == 0)
                return;

            foreach (var sec in sections)
            {
                var materials = sec.SelectNodes("div[@class='content']/ul/li/div/div/a");

                if (materials == null || materials.Count == 0)
                    continue;

                foreach (var mat in materials)
                {
                    string downloadLink = mat.Attributes["href"].Value;
                    downloadLink += "&redirect=1";

                    // skip folder links etc.
                    if (!downloadLink.StartsWith("https://elearning.fhws.de/mod/resource/"))
                        continue;

                    string title = mat.SelectSingleNode("span[@class='instancename']").FirstChild.InnerText;

                    WebClient client = CreateWebclient();

                    WebRequest req = WebRequest.Create(downloadLink);
                    req.Headers = client.Headers;

                    using (WebResponse response = await req.GetResponseAsync())
                    {
                        string fileName = Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));

                        Console.WriteLine("Download material '{0}' to: {1}", fileName, dest);

                        using (Stream source = response.GetResponseStream())
                        using (MemoryStream mem = new MemoryStream())
                        {
                            await source.CopyToAsync(mem);

                            File.WriteAllBytes(Path.Combine(dest, fileName), mem.ToArray());
                        }
                    }
                }
            }
        }
    }
}
