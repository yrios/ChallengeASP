using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Models
{
    public class Entry
    {
        public Entry()
        {

        }
        public Entry(int id, DateTime date, string title, string content, User user)
        {
            this.id = id;
            this.creationDate = date;
            this.title = title;
            this.content = content;
            this.user = user;
        }

        public virtual int id { get; set; }
        public virtual DateTime creationDate { get; set; }
        public virtual string title { get; set; }
        public virtual string content { get; set; }
        public virtual User user { get; set; }

    }
}