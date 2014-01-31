using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Models
{
    public class Tweets
    {
        public virtual string id { get; set; }
        public virtual User user { get; set; }
        public virtual bool hidden { get; set; }
    }
}