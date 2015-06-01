using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace login.Models
{
    public class Article
    {
        public string Author { get; set; }
        public string Body { get; set; }
        public DateTime PublicationDate { get; set; }
    }
}