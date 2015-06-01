using System;
using System.Data.Entity;

namespace login.Models
{
    public enum SearchType { SourceCode, Article, Video, Slide }
    public class Search
    {
        public int Id { get; set; }
        public DateTime SearchDate { get; set; }
        public string SearchTerm { get; set; }
        public int SearchFrequency { get; set; }
        public string UserEmail { get; set; }
        public SearchType SearchType { get; set; }
    }

    public class SearchDBContext : DbContext
    {
        public DbSet<Search> Searches { get; set; }
    }
}