using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using login.Models;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using Octokit;

using Google.Apis.Authentication.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Xml;
using System.Text;

namespace login.Controllers
{
    public class SearchesController : Controller
    {
        
        private SearchDBContext db = new SearchDBContext();
        private List<string> githubUrls = new List<string>();
        private List<string> youtubeUrls = new List<string>();
        // GET: Searches
        public ActionResult Index()
        {
            return View(db.Searches.ToList());
        }

        // GET: Searches/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Search search = db.Searches.Find(id);
            if (search == null)
            {
                return HttpNotFound();
            }
            return View(search);
        }

        // GET: Searches/Create
        public ActionResult Show()
        {
            return View();
        }

        // POST: Searches/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Show([Bind(Include = "Id,SearchDate,SearchTerm,SearchFrequency,UserEmail,SearchType")] Search search)
        {
            if (ModelState.IsValid)
            {
                string[] words = search.SearchTerm.Split(' ');
                if (words.Count() == 2)
                {
                    search.SearchTerm = words[0];
                    search.SearchLang = words[1];
                }
                try
                {
                    if (search.SearchType == SearchType.Article) search.Articles = ParseRssFile(search.SearchTerm);
                    if (search.SearchType == SearchType.Video) search.Videos = RunYoutube(search.SearchTerm);
                    if (search.SearchType == SearchType.SourceCode) search.Repos =  GetGithub(search.SearchTerm, search.SearchLang);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                var newSearch = (from s in db.Searches
                                  where s.SearchTerm == search.SearchTerm
                                  select s).SingleOrDefault();

                if (newSearch != null)
                {
                    newSearch.SearchFrequency += 1;
                    newSearch.SearchDate = System.DateTime.Now;
                    db.SaveChanges();
                }
                else
                {
                    search.UserEmail = User.Identity.GetUserName();
                    search.SearchDate = System.DateTime.Now;
                    search.SearchFrequency = 1;
                    db.Searches.Add(search);
                    db.SaveChanges();
                }
                //return RedirectToAction("Index");
            }

            return View(search);
        }

        // GET: Searches/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Search search = db.Searches.Find(id);
            if (search == null)
            {
                return HttpNotFound();
            }
            return View(search);
        }

        // POST: Searches/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,SearchDate,SearchTerm,SearchFrequency")] Search search)
        {
            if (ModelState.IsValid)
            {
                db.Entry(search).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(search);
        }

        // GET: Searches/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Search search = db.Searches.Find(id);
            if (search == null)
            {
                return HttpNotFound();
            }
            return View(search);
        }

        // POST: Searches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Search search = db.Searches.Find(id);
            db.Searches.Remove(search);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IEnumerable<login.Models.Repo> GetGithub(string searchTerm, string searchLang)
        {
            Language lang;
            try
            {
                lang = (Language)Enum.Parse(typeof(Language), searchLang, true);
            }
            catch(Exception)
            {
                lang = Language.CSharp;
            }

            List<login.Models.Repo> repos = new List<Repo>();
            var github = new GitHubClient(new ProductHeaderValue("rix-tw"));
            var searchReposRequest = new SearchRepositoriesRequest(searchTerm)
            {
                Language = lang,
                Order = SortDirection.Descending,
                PerPage = 100
            };
            var searchRepoResult = github.Search.SearchRepo(searchReposRequest).Result;
            foreach(var result in searchRepoResult.Items)
            {
                repos.Add(new Repo { Name = result.Name, AvatarUrl = result.Owner.AvatarUrl, Description = result.Description, Lang = result.Language, Url = result.HtmlUrl, User = result.Owner.Login });
            }
            return repos;
        }

        public IEnumerable<login.Models.Video> RunYoutube(string searchTerm)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyDuW3JrKor6WKZDFmj2Dw-Y4GoDlLgXnDw",
                ApplicationName = this.GetType().ToString()
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm; // Replace with your search term.
            searchListRequest.MaxResults = 20;

            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = searchListRequest.Execute();

            List<login.Models.Video> videos = new List<login.Models.Video>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        //videos.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                        videos.Add(new login.Models.Video { Title = searchResult.Snippet.Title, Url = "https://www.youtube.com/embed/" + searchResult.Id.VideoId });
                        break;

                    case "youtube#channel":
                        channels.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.ChannelId));
                        break;

                    case "youtube#playlist":
                        playlists.Add(String.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.PlaylistId));
                        break;
                }
            }
            return videos;
        }
    
        public IEnumerable<Article> ParseRssFile(string searchTerm)
        {
            List<Article> Articles = new List<Article>();

            XmlDocument rssXmlDoc = new XmlDocument();
            rssXmlDoc.Load("http://www.pcworld.com/howto/index.rss");

            XmlNodeList rssNodes = rssXmlDoc.SelectNodes("rss/channel/item");

            StringBuilder rssContent = new StringBuilder();
 
            foreach (XmlNode rssNode in rssNodes)
            {
                Article newArticle = new Article();
                XmlNode rssSubNode = rssNode.SelectSingleNode("title");
                newArticle.Title = rssSubNode != null ? rssSubNode.InnerText : "";

                rssSubNode = rssNode.SelectSingleNode("pubDate");
                string date = rssSubNode.InnerText;
                newArticle.PublicationDate = DateTime.Parse(date);

                rssSubNode = rssNode.SelectSingleNode("author");
                newArticle.Author = rssSubNode != null ? rssSubNode.InnerText : "";

                rssSubNode = rssNode.SelectSingleNode("description");
                newArticle.Description = rssSubNode != null ? rssSubNode.InnerText : "";

                rssSubNode = rssNode.SelectSingleNode("link");
                newArticle.Url = rssSubNode != null ? rssSubNode.InnerText : "";

                XmlNamespaceManager xmlNsMgr= new XmlNamespaceManager(new NameTable());
                xmlNsMgr.AddNamespace("media", "http://search.yahoo.com/mrss/");
                rssSubNode = rssNode.SelectSingleNode("media:thumbnail", xmlNsMgr);
                newArticle.ThumbnailUrl = rssSubNode != null ? rssSubNode.Attributes["url"].Value : "";

                XmlNode categories = rssNode.SelectSingleNode("categories");
                rssSubNode = categories.SelectSingleNode("category");
                newArticle.Category = rssSubNode != null ? rssSubNode.InnerText : "";

                if (newArticle.Title.Contains(searchTerm) || newArticle.Description.Contains(searchTerm))
                    Articles.Add(newArticle);
            }
            return Articles;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
