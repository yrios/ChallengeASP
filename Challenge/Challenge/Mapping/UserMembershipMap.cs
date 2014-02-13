using Challenge.Models;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Mapping
{
    public class UserMembershipMap : ClassMap<UserMembership>
    {
        public UserMembershipMap()
        {
            Table("asp_membershipusers");
            Id(x => x.Id);
            Map(x => x.Username);
            Map(x => x.ApplicationName);
            Map(x => x.Email);
            Map(x => x.Comment);
            Map(x => x.Password);
            Map(x => x.PasswordQuestion);
            Map(x => x.PasswordAnswer);
            Map(x => x.IsApproved);
            Map(x => x.LastActivityDate);
            Map(x => x.LastLoginDate);
            Map(x => x.LastPasswordChangedDate);
            Map(x => x.CreationDate);
            Map(x => x.IsOnLine);
            Map(x => x.IsLockedOut);
            Map(x => x.LastLockedOutDate);
            Map(x => x.FailedPasswordAttemptCount);
            Map(x => x.FailedPasswordAnswerAttemptCount);
            Map(x => x.FailedPasswordAttemptWindowStart);
            Map(x => x.FailedPasswordAnswerAttemptWindowStart);
            HasManyToMany(x => x.Roles);
            HasMany( x => x.User);

        }

    }
}