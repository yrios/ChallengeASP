using Challenge.Models;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Mapping
{
    public class UserMap : ClassMap<User>
    {
        /* la X en el codigo es una instancia de la clase User*/
        public UserMap()
        {
            Table("users");
            Id( x => x.id);
            Map( x => x.username);
            Map( x => x.email);
            Map( x => x.password);
            Map( x => x.salt);
            Map( x => x.twitterAccount);
            HasMany( x => x.entries);// esto indica una relacion User tiene muchas entries (one-to-many)

        }
    }
}