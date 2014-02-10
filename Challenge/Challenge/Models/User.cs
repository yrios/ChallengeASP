using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Models
{
    public class User
    {
        public virtual int id { get; set; }
        public virtual string username { get; set; }
        public virtual string email { get; set; }
        public virtual string password { get; set; }
        public virtual string salt { get; set; }
        public virtual string twitterAccount { get; set; }
        public virtual IList<Entry> entries { get; set; }
        public virtual UserMembership userMembership { get; set; }

        public User()
        {
            entries = new List<Entry>();
        }

        public virtual void addEntry(Entry entry)
        {
            entry.user = this;
            entries.Add(entry);
        }

        public virtual void removeEntry(Entry entry)
        {
            entry.user = null;
            entries.Remove(entry);
        }

    }

    public class UserProxy
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; } /*receive the password as string*/
        public string twitterAccount { get; set; }
    }
}