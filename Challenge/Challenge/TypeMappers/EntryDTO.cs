using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.TypeMappers
{
    public class EntryDTO
    {
        public  int id { get; set; }
        public  DateTime creationDate { get; set; }
        public  string title { get; set; }
        public  string content { get; set; }
        public  string user { get; set; }

        public EntryDTO creatEntry(Entry item)
        {
            this.id = item.id;
            this.title = item.title;
            this.content = item.content;
            this.creationDate = item.creationDate;
            this.user = item.user.username;

            return this;
        }
    }
}