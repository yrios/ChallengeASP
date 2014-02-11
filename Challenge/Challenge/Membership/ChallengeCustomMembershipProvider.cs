using Challenge.Configuration;
using NHibernate;
using NHibernate.Criterion;
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
        private MembershipUser GetMembershipUserFromUser(Models.User usr)
        {
            MembershipUser membershipUser = new MembershipUser(this.Name,
                                                  usr.userMembership.Username,
                                                  usr.userMembership.Id,
                                                  usr.userMembership.Email,
                                                  usr.userMembership.PasswordQuestion,
                                                  usr.userMembership.Comment,
                                                  usr.userMembership.IsApproved,
                                                  usr.userMembership.IsLockedOut,
                                                  usr.userMembership.CreationDate,
                                                  usr.userMembership.LastLoginDate,
                                                  usr.userMembership.LastActivityDate,
                                                  usr.userMembership.LastPasswordChangedDate,
                                                  usr.userMembership.LastLockedOutDate);

            return membershipUser;
        }

        //Fn that performs the checks and updates associated with password failure tracking
        private void UpdateFailureCount(string username, string failureType)
        {
            DateTime windowStart = new DateTime();
            int failureCount = 0;
            Models.User usr = null;

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
                                failureCount = usr.userMembership.FailedPasswordAttemptCount;
                                windowStart = usr.userMembership.FailedPasswordAttemptWindowStart;
                            }

                            if (failureType == "passwordAnswer")
                            {
                                failureCount = usr.userMembership.FailedPasswordAnswerAttemptCount;
                                windowStart = usr.userMembership.FailedPasswordAnswerAttemptWindowStart;
                            }
                        }

                        DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                        if (failureCount == 0 || DateTime.Now > windowEnd)
                        {
                            // First password failure or outside of PasswordAttemptWindow. 
                            // Start a new password failure count from 1 and a new window starting now.

                            if (failureType == "password")
                            {
                                usr.userMembership.FailedPasswordAttemptCount = 1;
                                usr.userMembership.FailedPasswordAttemptWindowStart = DateTime.Now; ;
                            }

                            if (failureType == "passwordAnswer")
                            {
                                usr.userMembership.FailedPasswordAnswerAttemptCount = 1;
                                usr.userMembership.FailedPasswordAnswerAttemptWindowStart = DateTime.Now; ;
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
                                usr.userMembership.IsLockedOut = true;
                                usr.userMembership.LastLockedOutDate = DateTime.Now;
                                session.Update(usr);
                                transaction.Commit();
                            }
                            else
                            {
                                // Password attempts have not exceeded the failure threshold. Update
                                // the failure counts. Leave the window the same.

                                if (failureType == "password")
                                    usr.userMembership.FailedPasswordAttemptCount = failureCount;

                                if (failureType == "passwordAnswer")
                                    usr.userMembership.FailedPasswordAnswerAttemptCount = failureCount;

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
            Models.User usr = null;
            MembershipUser u = null;

            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        if (isKeySupplied)
                            usr = session.CreateCriteria(typeof(Models.User))
                                            .Add(NHibernate.Criterion.Restrictions.Eq("Id", (int)providerUserKey))
                                            .UniqueResult<Models.User>();

                        else
                            usr = session.CreateCriteria(typeof(Models.User))
                                            .Add(NHibernate.Criterion.Restrictions.Eq("Username", username))
                                            .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                            .UniqueResult<Models.User>();

                        if (usr != null)
                        {
                            u = GetMembershipUserFromUser(usr);

                            if (userIsOnline)
                            {
                                usr.userMembership.LastActivityDate = System.DateTime.Now;
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

        private Models.User GetUserByUsername(string username)
        {
            Models.User usr = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usr = session.CreateCriteria<Models.User>()
                                        .Add(Restrictions.Eq("Username", username))
                                        .Add(Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .UniqueResult<Models.User>();


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

        private IList<Models.User> GetUsers()
        {
            IList<Models.User> usrs = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usrs = session.CreateCriteria(typeof(Models.User))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .List<Models.User>();

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

        private IList<Models.User> GetUsersLikeUsername(string usernameToMatch)
        {
            IList<Models.User> usrs = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usrs = session.CreateCriteria(typeof(Models.User))
                                        .Add(NHibernate.Criterion.Restrictions.Like("Username", usernameToMatch))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .List<Models.User>();

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

        private IList<Models.User> GetUsersLikeEmail(string emailToMatch)
        {
            IList<Models.User> usrs = null;
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    try
                    {
                        usrs = session.CreateCriteria(typeof(Models.User))
                                        .Add(NHibernate.Criterion.Restrictions.Like("Email", emailToMatch))
                                        .Add(NHibernate.Criterion.Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .List<Models.User>();

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
     
        //OKKK
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            int rowAffected = 0;
            Models.User user = null;

            if (!ValidateUser(username, oldPassword))
                return false;

            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Change password canceled due to password validation failure");

            using(ISessionFactory session = FluentNhibernateConfiguration.CreateSessionFactory()){
                using(var _session = session.OpenSession()){
                    using(ITransaction transaction = _session.BeginTransaction()){
                        try
                        {
                            user = GetUserByUsername(username);
                            if(user != null)
                            {
                                string encodedPass = EncodePassword(newPassword);
                                user.password = encodedPass;
                                user.userMembership.Password = encodedPass;
                                user.userMembership.LastPasswordChangedDate = DateTime.Now;
                                _session.Update(user);
                                transaction.Commit();
                                rowAffected = 2;
                            }
                        }
                        catch (Exception e)
                        {
                            if (WriteExceptionsToEventLog)
                                WriteToEventLog(e, "ChangePassword");
                            throw new ProviderException(exceptionMessage);
                        }

                    }

                }

                if (rowAffected > 0)
                    return true;
                return false;
            }
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        //OKKK
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status, string twitterAccount)
        {
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != "")
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            MembershipUser u = GetUser(username, false);

            if (u == null)
            {
                DateTime createDate = DateTime.Now;
                using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
                {
                    using (var _session = sessionFactory.OpenSession())
                    {
                        using (ITransaction transaction = _session.BeginTransaction())
                        {

                            Models.UserMembership userMembership = new Models.UserMembership();
                            userMembership.Username = username;
                            userMembership.Password = EncodePassword(password);
                            userMembership.Email = email;
                            userMembership.PasswordQuestion = passwordQuestion;
                            userMembership.PasswordAnswer = EncodePassword(passwordAnswer);
                            userMembership.IsApproved = isApproved;
                            userMembership.Comment = "";
                            userMembership.CreationDate = createDate;
                            userMembership.LastPasswordChangedDate = createDate;
                            userMembership.LastActivityDate = createDate;
                            userMembership.ApplicationName = _applicationName;
                            userMembership.IsLockedOut = false;
                            userMembership.LastLockedOutDate = createDate;
                            userMembership.FailedPasswordAttemptCount = 0;
                            userMembership.FailedPasswordAttemptWindowStart = createDate;
                            userMembership.FailedPasswordAnswerAttemptCount = 0;
                            userMembership.FailedPasswordAnswerAttemptWindowStart = createDate;

                            Models.User user = new Models.User();
                            user.username = username;
                            user.email = email;
                            user.password = password;
                            user.salt = "";
                            user.twitterAccount = twitterAccount;
                            user.userMembership = userMembership;

                            try
                            {
                                int retId = (int)_session.Save(user);
                                transaction.Commit();
                                if (retId < 1)
                                    status = MembershipCreateStatus.UserRejected;
                                else
                                    status = MembershipCreateStatus.Success;
                            }
                            catch (Exception e)
                            {
                                status = MembershipCreateStatus.ProviderError;
                                if (WriteExceptionsToEventLog)
                                    WriteToEventLog(e, "CreateUser");
                            }

                        }

                    }
                }
                return GetUser(username, false);
            }
            else
                status = MembershipCreateStatus.DuplicateUserName;
            return null;
        }

        //OKKKK
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

        //OKKK
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        //OKKK
        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        //OKK
        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        //OKKKK
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            return GetMembershipUserByKeyOrUser(false, username, 0, userIsOnline);
        }

        //OKKK
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            return GetMembershipUserByKeyOrUser(true, string.Empty, providerUserKey, userIsOnline);
        }

        //OKKKKK
        public override string GetUserNameByEmail(string email)
        {
            Models.User user = null;
            using(var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory()){
                using(var _session = sessionFactory.OpenSession()){
                    using(ITransaction transaction = _session.BeginTransaction()){
                        try
                        {
                            user = _session.CreateCriteria<Models.User>().Add(Restrictions.Eq("email", email)).UniqueResult<Models.User>();
                        }
                        catch (Exception e)
                        {
                            if (WriteExceptionsToEventLog)
                                WriteToEventLog(e, "GetUserNameByEmail");
                            throw new ProviderException(exceptionMessage);
                        }
                    }
                }
            }

            if (user == null)
                return null;
            else
                return user.userMembership.Username;
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