using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Models
{
    public class Entry
    {
        public virtual int id { get; set; }
        public virtual DateTime creationDate { get; set; }
        public virtual string title { get; set; }
        public virtual string content { get; set; }
        public virtual User user { get; set; }

    }
}