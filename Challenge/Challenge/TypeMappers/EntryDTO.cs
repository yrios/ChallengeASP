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

        public static EntryDTO creatEntry(Entry item)
        {
            EntryDTO entry = new EntryDTO();
            entry.id = item.id;
            entry.title = item.title;
            entry.content = item.content;
            entry.creationDate = item.creationDate;
            entry.user = item.user.username;

            return entry;
        }
    }
}