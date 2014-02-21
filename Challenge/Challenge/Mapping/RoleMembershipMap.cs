using Challenge.Models;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Mapping
{
    public class RoleMembershipMap : ClassMap<RoleMembership>
    {
        public RoleMembershipMap()
        {
            Table("asp_membershiproles");
            Id(x => x.Id);
            Map(x => x.RoleName);
            Map(x => x.ApplicationName);
            HasManyToMany(x => x.UsersInRole).Cascade.All().Inverse()
                .Table("asp_membershipusersinroles")
                .ParentKeyColumn("Users_Id")
                .ChildKeyColumn("Roles_Id");
        }
    }
}