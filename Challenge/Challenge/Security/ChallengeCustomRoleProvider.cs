using Challenge.Models;
using NHibernate;
using NHibernate.Collection;
using NHibernate.Criterion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;

namespace Challenge.Security
{
    public class ChallengeCustomRoleProvider : RoleProvider
    {

        private string applicationName;
        private static ISessionFactory _sessionFactory;
        private bool writeToEventlog;

        public override void Initialize(string name, NameValueCollection config)
        {

            _sessionFactory = WebApiApplication.SessionFactory;
            applicationName = config["applicationName"];
            writeToEventlog = true;

            base.Initialize(name, config);
        }

        //Init Helper Methods

        private void WriteToEventLog(Exception e, string action)
        {
            string message = "An exception occurred communicating with the data source.\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            Debug.WriteLine(message);
        }

        private RoleMembership GetRole(string rolename)
        {
            RoleMembership role = null;

            try
            {
                role = _sessionFactory.GetCurrentSession().CreateCriteria(typeof(RoleMembership))
                    .Add(Restrictions.Eq("RoleName", rolename))
                    .Add(Restrictions.Eq("ApplicationName", applicationName))
                    .UniqueResult<RoleMembership>();

                //just to lazy init the collection, otherwise get the error 
                //NHibernate.LazyInitializationException: failed to lazily initialize a collection, no session or session was closed
                IList<UserMembership> us = role.UsersInRole;

            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "GetRole");
                else
                    throw e;
            }
            return role;
        }

        //End Helper Methods
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            UserMembership usr = null;
            foreach (string rolename in roleNames)
            {
                if (!RoleExists(rolename))
                    throw new ProviderException(String.Format("Role name {0} not found.", rolename));
            }

            foreach (string username in usernames)
            {
                if (username.Contains(","))
                    throw new ArgumentException(String.Format("User names {0} cannot contain commas.", username));
                //is user not exiting //throw exception

                foreach (string rolename in roleNames)
                {
                    if (IsUserInRole(username, rolename))
                        throw new ProviderException(String.Format("User {0} is already in role {1}.", username, rolename));
                }
            }


            try
            {
                foreach (string username in usernames)
                {
                    foreach (string rolename in roleNames)
                    {
                        //get the user
                        usr = _sessionFactory.GetCurrentSession().CreateCriteria(typeof(UserMembership))
                                    .Add(Restrictions.Eq("Username", username))
                                    .Add(Restrictions.Eq("ApplicationName", applicationName))
                                    .UniqueResult<UserMembership>();

                        if (usr != null)
                        {
                            //get the role first from db
                            RoleMembership role = _sessionFactory.GetCurrentSession().CreateCriteria(typeof(RoleMembership))
                                    .Add(Restrictions.Eq("RoleName", rolename))
                                    .Add(Restrictions.Eq("ApplicationName", applicationName))
                                    .UniqueResult<RoleMembership>();
                            
                            usr.AddRole(role);
                        }
                    }
                    _sessionFactory.GetCurrentSession().SaveOrUpdate(usr);
                }
            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "AddUsersToRoles");
                else
                    throw e;
            }
        }

        public override string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
        }


        public override void CreateRole(string roleName)
        {
            if (roleName.Contains(","))
                throw new ArgumentException("Role names cannot contain commas.");

            if (RoleExists(roleName))
                throw new ProviderException("Role name already exists.");

            try
            {
                RoleMembership role = new RoleMembership();
                role.ApplicationName = applicationName;
                role.RoleName = roleName;
                _sessionFactory.GetCurrentSession().Save(role);
            }
            catch (OdbcException e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "CreateRole");
                else
                    throw e;
            }
          
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            bool deleted = false;
            if (!RoleExists(roleName))
                throw new ProviderException("Role does not exist.");

            if (throwOnPopulatedRole && GetUsersInRole(roleName).Length > 0)
                throw new ProviderException("Cannot delete a populated role.");

            try
            {
                RoleMembership role = GetRole(roleName);
                _sessionFactory.GetCurrentSession().Delete(role);
                deleted = true;
            }
            catch (OdbcException e)
            {
                if (writeToEventlog)
                {
                    WriteToEventLog(e, "DeleteRole");
                    return deleted;
                }
                else
                    throw e;
            }
            return deleted;
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            string[] roles = null;

            try
            {

                IList<RoleMembership> allroles = _sessionFactory.GetCurrentSession().QueryOver<RoleMembership>().List();
                roles = new string[allroles.Count];

                foreach (RoleMembership role in allroles)
                {
                    roles[allroles.IndexOf(role)] = role.RoleName;
                }
            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "GetAllRoles");
                else
                    throw e;
            }

            return roles;
        }

        public override string[] GetRolesForUser(string username)
        {
            UserMembership usr = null;
            IList<RoleMembership> usrroles = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                usr = _sessionFactory.GetCurrentSession().CreateCriteria(typeof(UserMembership))
                                .Add(Restrictions.Eq("Username", username))
                                .Add(Restrictions.Eq("ApplicationName", applicationName))
                                .UniqueResult<UserMembership>();

                if (usr != null)
                {
                    usrroles = usr.Roles;
                    foreach (RoleMembership r in usrroles)
                    {
                        sb.Append(r.RoleName + ",");
                    }
                }
            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "GetRolesForUser");
                else
                    throw e;
            }

            if (sb.Length > 0)
            {
                // Remove trailing comma.
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString().Split(',');
            }

            return new string[0];
        }

        public override string[] GetUsersInRole(string roleName)
        {

            string[] usersInrole  = null;

            try
            {
                
                var result = _sessionFactory.GetCurrentSession().QueryOver<UserMembership>()
                    .Right.JoinQueryOver<RoleMembership>(x => x.Roles)
                    .Where(c => c.RoleName == roleName).List();

                IList<UserMembership> users = result;
                usersInrole = new string[users.Count];

                foreach (UserMembership u in users)
                {
                    usersInrole[users.IndexOf(u)] = u.Username;
                }
            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "GetUsersInRole");
                else
                    throw e;
            }

            if (usersInrole.Length > 0)
            {
                return usersInrole;
            }

            return new string[0];
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            bool userIsInRole = false;
            UserMembership usr = null;
            IList<RoleMembership> usrroles = null;
            
            try
            {
                usr = _sessionFactory.GetCurrentSession().CreateCriteria(typeof(UserMembership))
                                .Add(Restrictions.Eq("Username", username))
                                .Add(Restrictions.Eq("ApplicationName", applicationName))
                                .UniqueResult<UserMembership>();

                if (usr != null)
                {
                    usrroles = usr.Roles;
                    foreach (RoleMembership r in usrroles)
                    {
                        if (r.RoleName.Equals(roleName))
                        {
                            userIsInRole = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "IsUserInRole");
                else
                    throw e;
            }
            return userIsInRole;  
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            bool exists = false;

            try
            {
                RoleMembership role = _sessionFactory.GetCurrentSession().CreateCriteria(typeof(RoleMembership))
                                    .Add(Restrictions.Eq("ApplicationName", applicationName))
                                    .Add(Restrictions.Eq("RoleName", roleName))
                                    .UniqueResult<RoleMembership>();
                if (role != null)
                    exists = true;

            }
            catch (Exception e)
            {
                if (writeToEventlog)
                    WriteToEventLog(e, "RoleExists");
                else
                    throw e;
            }
            return exists;
        }
    }
}