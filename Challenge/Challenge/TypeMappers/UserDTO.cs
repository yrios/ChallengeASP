using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.TypeMappers
{
    public class UserDTO
    {
        public virtual int id { get; set; }
        public virtual string username { get; set; }
        public virtual string email { get; set; }
        public virtual string password { get; set; }
        public virtual string salt { get; set; }
        public virtual string twitterAccount { get; set; }
        public virtual IList<Entry> entries { get; set; }
    }
}