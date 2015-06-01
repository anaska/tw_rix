using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace login.Models
{
    public class Repo
    {
        public string User { get; set; }
        public string Url { get; set; }
        public string AvatarUrl { get; set; }
        public string Description { get; set; }
        public string Lang { get; set; }
    }
}