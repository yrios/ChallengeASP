using Challenge.Models;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Mapping
{
    public class TweetsMap : ClassMap<Tweets>
    {
        public TweetsMap()
        {
            Table("hidden_tweets");
            Id( x => x.id);
            References( x => x.user); //"Many" Tweets to "One" User
            Map( x => x.hidden);
        }
    }
}