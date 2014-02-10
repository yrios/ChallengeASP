using NHibernate;
using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace Challenge.Membership
{
    public class ChallengeCustomMembershipProvider : MembershipProvider
    {
        //private region
        // Global connection string, generated password length, generic exception message, event log info.
        private int newPasswordLength = 8;
        private string eventSource = "ChallengeCustomMembership";
        private string eventLog = "Application";
        private string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private string connectionString;

        private static ISessionFactory _sessionFactory;
        private string _applicationName;
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;
        private int _maxInvalidPasswordAttempts;
        private int _passwordAttemptWindow;
        private MembershipPasswordFormat _passwordFormat;
        // Used when determining encryption key values.
        private MachineKeySection _machineKey;
        private int _minRequiredNonAlphanumericCharacters;
        private int _minRequiredPasswordLength;
        private string _passwordStrengthRegularExpression;
        //end of private region

        //public section
        public override string ApplicationName
        {
            get { return _applicationName;}
            set { _applicationName = value;}
        }
        public override bool EnablePasswordReset
        {
            get { return _enablePasswordReset; }
        }
        public override bool EnablePasswordRetrieval
        {
            get { return _enablePasswordRetrieval; }
        }
        public override bool RequiresQuestionAndAnswer
        {
            get { return _requiresQuestionAndAnswer; }
        }
        public override bool RequiresUniqueEmail
        {
            get { return _requiresUniqueEmail; }
        }
        public override int MaxInvalidPasswordAttempts
        {
            get { return _maxInvalidPasswordAttempts; }
        }
        public override int PasswordAttemptWindow
        {
            get { return _passwordAttemptWindow; }
        }
        public override MembershipPasswordFormat PasswordFormat
        {
            get { return _passwordFormat; }
        }
        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _minRequiredNonAlphanumericCharacters; }
        }
        public override int MinRequiredPasswordLength
        {
            get { return _minRequiredPasswordLength; }
        }
        public override string PasswordStrengthRegularExpression
        {
            get { return _passwordStrengthRegularExpression; }
        }

        // If false, exceptions are thrown to the caller. If true,
        // exceptions are written to the event log.
        public bool WriteExceptionsToEventLog { get; set; }

        /// <summary>Gets the session factory.</summary>
        private static ISessionFactory SessionFactory
        {
            get { return _sessionFactory; }
        }
        //end of public section

        //Helper function section
        // A Function to retrieve config values from the configuration file
        private string GetConfigValue(string configValue, string defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }

        //Fn to create a Membership user from a Entities.Users class
        private MembershipUser GetMembershipUserFromUser(Models.UserMembership usr)
        {
            MembershipUser membershipUser = new MembershipUser(this.Name,
                                                  usr.Username,
                                                  usr.Id,
                                                  usr.Email,
                                                  usr.PasswordQuestion,
                                                  usr.Comment,
                                                  usr.IsApproved,
                                                  usr.IsLockedOut,
                                                  usr.CreationDate,
                                                  usr.LastLoginDate,
                                                  usr.LastActivityDate,
                                                  usr.LastPasswordChangedDate,
                                                  usr.LastLockedOutDate);

            return membershipUser;
        }

        //Fn that performs the checks and updates associated with password failure tracking
        private void UpdateFailureCount(string username, string failureType)
        {
            DateTime windowStart = new DateTime();
            int failureCount = 0;
            Models.UserMembership usr = null;

            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    try
                    {
                        usr = GetUserByUsername(username);

                        if (!(usr == null))
                        {
                            if (failureType == "password")
                            {
                                failureCount = usr.FailedPasswordAttemptCount;
                                windowStart = usr.FailedPasswordAttemptWindowStart;
                            }

                            if (failureType == "passwordAnswer")
                            {
                                failureCount = usr.FailedPasswordAnswerAttemptCount;
                                windowStart = usr.FailedPasswordAnswerAttemptWindowStart;
                            }
                        }

                        DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                        if (failureCount == 0 || DateTime.Now > windowEnd)
                        {
                            // First password failure or outside of PasswordAttemptWindow. 
                            // Start a new password failure count from 1 and a new window starting now.

                            if (failureType == "password")
                            {
                                usr.FailedPasswordAttemptCount = 1;
                                usr.FailedPasswordAttemptWindowStart = DateTime.Now; ;
                            }

                            if (failureType == "passwordAnswer")
                            {
                                usr.FailedPasswordAnswerAttemptCount = 1;
                                usr.FailedPasswordAnswerAttemptWindowStart = DateTime.Now; ;
                            }
                            session.Update(usr);
                            transaction.Commit();
                        }
                        else
                        {
                            if (failureCount++ >= MaxInvalidPasswordAttempts)
                            {
                                // Password attempts have exceeded the failure threshold. Lock out
                                // the user.
                                usr.IsLockedOut = true;
                                usr.LastLockedOutDate = DateTime.Now;
                                session.Update(usr);
                                transaction.Commit();
                            }
                            else
                            {
                                // Password attempts have not exceeded the failure threshold. Update
                                // the failure counts. Leave the window the same.

                                if (failureType == "password")
                                    usr.FailedPasswordAttemptCount = failureCount;

                                if (failureType == "passwordAnswer")
                                    usr.FailedPasswordAnswerAttemptCount = failureCount;

                                session.Update(usr);
                                transaction.Commit();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (WriteExceptionsToEventLog)
                        {
                            WriteToEventLog(e, "UpdateFailureCount");
                            throw new ProviderException("Unable to update failure count and window start." + exceptionMessage);
                        }
                        else
                            throw e;
                    }
                }
            }

        }

        //CheckPassword: Compares password values based on the MembershipPasswordFormat.
        private bool CheckPassword(string password, string dbpassword)
        {
            string pass1 = password;
            string pass2 = dbpassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Encrypted:
                    pass2 = UnEncodePassword(dbpassword);
                    break;
                case MembershipPasswordFormat.Hashed:
                    pass1 = EncodePassword(password);
                    break;
                default:
                    break;
            }

            if (pass1 == pass2)
            {
                return true;
            }

            return false;
        }

        //EncodePassword:Encrypts, Hashes, or leaves the password clear based on the PasswordFormat.
        private string EncodePassword(string password)
        {
            string encodedPassword = password;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword =
                      Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    HMACSHA1 hash = new HMACSHA1();
                    hash.Key = HexToByte(_machineKey.ValidationKey);
                    encodedPassword =
                      Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(password)));
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }
            return encodedPassword;
        }

        // UnEncodePassword :Decrypts or leaves the password clear based on the PasswordFormat.
        private string UnEncodePassword(string encodedPassword)
        {
            string password = encodedPassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    password =
                      Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Cannot unencode a hashed password.");
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return password;
        }

        //   Converts a hexadecimal string to a byte array. Used to convert encryption key values from the configuration.    
        private byte[] HexToByte(string hexString)
        {
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        // WriteToEventLog
        // A helper function that writes exception detail to the event log. Exceptions
        // are written to the event log as a security measure to avoid private database
        // details from being returned to the browser. If a method does not return a status
        // or boolean indicating the action succeeded or failed, a generic exception is also 
        // thrown by the caller.

        private void WriteToEventLog(Exception e, string action)
        {
            EventLog log = new EventLog();
            log.Source = eventSource;
            log.Log = eventLog;

            string message = "An exception occurred communicating with the data source.\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            log.WriteEntry(message);
        }

        //End region

        //region private methods

        //single fn to get a membership user by key or username
        private MembershipUser GetMembershipUserByKeyOrUser(bool isKeySupplied, string username, object providerUserKey, bool userIsOnline)
        {
            Models.UserMembership usr = null;
            MembershipUser u = null;

            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        if (isKeySupplied)
                            usr = session.CreateCriteria(typeof(Models.UserMembership))
                                            .Add(NHibernate.Criterion.Restrictions.Eq("Id", (int)providerUserKey))
                                            .UniqueResult<Models.UserMembership>();

                        else
                            usr = session.CreateCriteria(typeof(Models.UserMembership))
                                            .Add(NHibernate.Criterion.Restrictions.Eq("Username", username))
                                            .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                            .UniqueResult<Models.UserMembership>();

                        if (usr != null)
                        {
                            u = GetMembershipUserFromUser(usr);

                            if (userIsOnline)
                            {
                                usr.LastActivityDate = System.DateTime.Now;
                                session.Update(usr);
                                transaction.Commit();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (WriteExceptionsToEventLog)
                            WriteToEventLog(e, "GetUser(Object, Boolean)");
                        throw new ProviderException(exceptionMessage);
                    }
                }
            }
            return u;
        }

        private Models.UserMembership GetUserByUsername(string username)
        {
            Models.UserMembership usr = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usr = session.CreateCriteria(typeof(Models.UserMembership))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("Username", username))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .UniqueResult<Models.UserMembership>();


                    }
                    catch (Exception e)
                    {
                        if (WriteExceptionsToEventLog)
                            WriteToEventLog(e, "UnlockUser");
                        throw new ProviderException(exceptionMessage);
                    }
                }
            }
            return usr;

        }

        private IList<Models.UserMembership> GetUsers()
        {
            IList<Models.UserMembership> usrs = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usrs = session.CreateCriteria(typeof(Models.UserMembership))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .List<Models.UserMembership>();

                    }
                    catch (Exception e)
                    {
                        if (WriteExceptionsToEventLog)
                            WriteToEventLog(e, "GetUsers");
                        throw new ProviderException(exceptionMessage);
                    }
                }
            }
            return usrs;

        }

        private IList<Models.UserMembership> GetUsersLikeUsername(string usernameToMatch)
        {
            IList<Models.UserMembership> usrs = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usrs = session.CreateCriteria(typeof(Models.UserMembership))
                                        .Add(NHibernate.Criterion.Restrictions.Like("Username", usernameToMatch))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .List<Models.UserMembership>();

                    }
                    catch (Exception e)
                    {
                        if (WriteExceptionsToEventLog)
                            WriteToEventLog(e, "GetUsersMatchByUsername");
                        throw new ProviderException(exceptionMessage);
                    }
                }
            }
            return usrs;

        }

        private IList<Models.UserMembership> GetUsersLikeEmail(string emailToMatch)
        {
            IList<Models.UserMembership> usrs = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usrs = session.CreateCriteria(typeof(Models.UserMembership))
                                        .Add(NHibernate.Criterion.Restrictions.Like("Email", emailToMatch))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .List<Models.UserMembership>();

                    }
                    catch (Exception e)
                    {
                        if (WriteExceptionsToEventLog)
                            WriteToEventLog(e, "GetUsersMatchByEmail");
                        throw new ProviderException(exceptionMessage);
                    }
                }
            }
            return usrs;

        }
     

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override bool EnablePasswordReset
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EnablePasswordRetrieval
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotImplementedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotImplementedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override bool ValidateUser(string username, string password)
        {
            throw new NotImplementedException();
        }
    }
}