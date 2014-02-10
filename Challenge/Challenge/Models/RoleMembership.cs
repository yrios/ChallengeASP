using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Models
{
    public class RoleMembership
    {
        public virtual int Id { get; private set; }
        public virtual string RoleName { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual IList<UserMembership> UsersInRole { get; set; }

        public RoleMembership()
        {
            UsersInRole = new List<UserMembership>();
        }
    }
}