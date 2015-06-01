using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace login.Models
{
    public class Article
    {
        public string Author { get; set; }
        public string Description { get; set; }
        public DateTime PublicationDate { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}