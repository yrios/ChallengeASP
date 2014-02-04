using Challenge.Models;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Mapping
{
    public class EntryMap : ClassMap<Entry>
    {
        public EntryMap()
        {
            Table("entries");
            Id( x => x.id );
            Map( x => x.creationDate);
            Map( x => x.title);
            Map( x => x.content);
            References(x => x.user);
        }
    }
}