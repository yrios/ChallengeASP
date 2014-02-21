using Challenge.Models;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
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

        private void WriteToEventLog(Exception e, string action)
        {
            string message = "An exception occurred communicating with the data source.\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            Debug.WriteLine(message);
        }

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

                            //Entities.Roles role = GetRole(rolename);
                            usr.AddRole(role);
                        }
                    }
                    _sessionFactory.GetCurrentSession().SaveOrUpdate(usr);
                }
                //transaction.Commit();
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
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
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

            StringBuilder sb = new StringBuilder();
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