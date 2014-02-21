﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.Models
{
    public class UserMembership
    {
        public UserMembership()
        {
            this.CreationDate = DateTime.MinValue;
            this.LastPasswordChangedDate = DateTime.MinValue;
            this.LastActivityDate = DateTime.MinValue;
            this.LastLockedOutDate = DateTime.MinValue;
            this.FailedPasswordAnswerAttemptWindowStart = DateTime.MinValue;
            this.FailedPasswordAttemptWindowStart = DateTime.MinValue;
            this.LastLoginDate = DateTime.MinValue;
            Roles = new List<RoleMembership>();
            Users = new List<User>();
        }

        public virtual int Id { get; set; }
        public virtual string Username { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual string Email { get; set; }
        public virtual string Comment { get; set; }
        public virtual string Password { get; set; }
        public virtual string PasswordQuestion { get; set; }
        public virtual string PasswordAnswer { get; set; }
        public virtual bool  IsApproved { get; set; }
        public virtual DateTime LastActivityDate { get; set; }
        public virtual DateTime LastLoginDate { get; set; }
        public virtual DateTime LastPasswordChangedDate { get; set; }
        public virtual DateTime CreationDate { get; set; }
        public virtual bool IsOnLine { get; set; }
        public virtual bool IsLockedOut { get; set; }
        public virtual DateTime LastLockedOutDate { get; set; }
        public virtual int FailedPasswordAttemptCount { get; set; }
        public virtual int FailedPasswordAnswerAttemptCount { get; set; }
        public virtual DateTime FailedPasswordAttemptWindowStart { get; set; }
        public virtual DateTime FailedPasswordAnswerAttemptWindowStart { get; set; }
        public virtual IList<RoleMembership> Roles { get; set; }
        public virtual IList<User> Users { get; set; }


        public virtual void AddRole(RoleMembership role)
        {
            //role.UsersInRole.Add(this);
            Roles.Add(role);
        }

        public virtual void RemoveRole(RoleMembership role)
        {
            //role.UsersInRole.Remove(this);
            Roles.Remove(role);
        }

    }
}