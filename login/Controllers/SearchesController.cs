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
                //RunAsyncGithub().Wait();
                try
                {
                    search.Videos = RunYoutube(search.SearchTerm);
                    search.Repos =  GetGithub(search.SearchTerm, Language.CSharp);
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

        public async Task RunAsyncGithub()
        {
            using (var httpClient = new HttpClient())
            {
                var url = new Uri("https://api.github.com/search/?q=addClass+in:file+language:js+repo:jquery/jquery");
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    httpRequestMessage.Headers.Add(System.Net.HttpRequestHeader.Accept.ToString(),
                      "application/vnd.github.v3+json");
                    httpRequestMessage.Headers.Add(System.Net.HttpRequestHeader.ContentType.ToString(),
                        "application/json");
                    httpRequestMessage.Headers.Add("User-Agent", "rix explorer");
                    using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage))
                    {
                        //to something with the response
                        var data = await httpResponseMessage.Content.ReadAsStringAsync();
                        var json = JObject.Parse(data);
                        IList<JToken> results = json["items"].Children().ToList();

                        // serialize JSON results into .NET objects
                        foreach (JToken result in results)
                        {
                            string resultURL = result.Value<string>("html_url");
                            githubUrls.Add(resultURL);
                        }
                    }
                }
            }
        }

        public IEnumerable<login.Models.Repo> GetGithub(string searchTerm, Language lang)
        {
            List<login.Models.Repo> repos = new List<Repo>();
            var github = new GitHubClient(new ProductHeaderValue("rix-tw"));
            var searchReposRequest = new SearchRepositoriesRequest(searchTerm)
            {
                Language = lang,
                Order = SortDirection.Descending,
                PerPage = 20
            };
            var searchRepoResult = github.Search.SearchRepo(searchReposRequest).Result;
            foreach(var result in searchRepoResult.Items)
            {
                repos.Add(new Repo { AvatarUrl = result.Owner.AvatarUrl, Description = result.Description, Lang = result.Language, Url = result.HtmlUrl, User = result.Owner.Login });
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
            searchListRequest.MaxResults = 10;

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
