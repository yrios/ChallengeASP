using Challenge.Configuration;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace Challenge.Security
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
        private string applicationName;
        private bool enablePasswordReset;
        private bool enablePasswordRetrieval;
        private bool requiresQuestionAndAnswer;
        private bool requiresUniqueEmail;
        private int maxInvalidPasswordAttempts;
        private int passwordAttemptWindow;
        private MembershipPasswordFormat passwordFormat;
        // Used when determining encryption key values.
        private MachineKeySection machineKey;
        private int minRequiredNonAlphanumericCharacters;
        private int minRequiredPasswordLength;
        private string passwordStrengthRegularExpression;
        //end of private region

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            base.Initialize(name, config);

            applicationName = config["applicationName"];
            enablePasswordRetrieval = Convert.ToBoolean(config["enablePasswordRetrieval"]);
            enablePasswordReset = Convert.ToBoolean(config["enablePasswordReset"]);
            requiresQuestionAndAnswer = Convert.ToBoolean(config["requiresQuestionAndAnswer"]);
            requiresUniqueEmail = Convert.ToBoolean(config["requiresUniqueEmail"]);
            maxInvalidPasswordAttempts = Convert.ToInt32(config["maxInvalidPasswordAttempts"]);
            minRequiredPasswordLength = Convert.ToInt32(config["minRequiredPasswordLength"]);
            minRequiredNonAlphanumericCharacters = Convert.ToInt32(config["minRequiredNonalphanumericCharacters"]);
            passwordAttemptWindow = Convert.ToInt32(config["passwordAttemptWindow"]);
            WriteExceptionsToEventLog = true;
            string passformat = config["passwordFormat"];

            switch (passformat)
            {
                case "Hashed":
                    passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
            }

           //Get encryption and decryption key information from the configuration.
          System.Configuration.Configuration cfg = WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
          machineKey = cfg.GetSection("system.web/machineKey") as MachineKeySection;

          if (machineKey.ValidationKey.Contains("AutoGenerate"))
          {
            if (PasswordFormat != MembershipPasswordFormat.Clear)
            {
              throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
            }
          }
        }

        //public section
        public override string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
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

        public string GetErrorMessage(MembershipCreateStatus status)
        {
            switch (status)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "Username already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A username for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
                
                case MembershipCreateStatus.Success:
                    return "The user was successfully created.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
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
                    hash.Key = HexToByte(machineKey.ValidationKey);
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

            Debug.WriteLine(message);
        }

        //End region

        //region private methods

        //single fn to get a membership user by key or username
        private MembershipUser GetMembershipUserByKeyOrUser(bool isKeySupplied, string username, object providerUserKey, bool userIsOnline)
        {
            Models.User usr = null;
            MembershipUser u = null;
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory()){
                using (ISession session = sessionFactory.OpenSession()){
                    using (ITransaction transaction = session.BeginTransaction()){
                        try
                        {
                            if (isKeySupplied)
                                usr = session.CreateCriteria(typeof(Models.User))
                                                .Add(NHibernate.Criterion.Restrictions.Eq("Id", (int)providerUserKey))
                                                .UniqueResult<Models.User>();

                            else
                                usr = session.CreateCriteria(typeof(Models.User))
                                                .Add(Restrictions.Eq("username", username))
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
                        /* Implementacion Directa con la tabla custom membership
                        usr = session.CreateCriteria<Models.User>()
                                        .Add(Restrictions.Eq("Username", username))
                                        .Add(Restrictions.Eq("ApplicationName", this.ApplicationName))
                                        .UniqueResult<Models.User>();
                         * */
                        usr = session.CreateCriteria<Models.User>()
                                        .Add(Restrictions.Eq("username", username))
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
                                        .Add(Restrictions.Like("email", emailToMatch))
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
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != null)
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
                            userMembership.ApplicationName = applicationName;
                            userMembership.IsLockedOut = false;
                            userMembership.LastLockedOutDate = createDate;
                            userMembership.FailedPasswordAttemptCount = 0;
                            userMembership.FailedPasswordAttemptWindowStart = createDate;
                            userMembership.FailedPasswordAnswerAttemptCount = 0;
                            userMembership.FailedPasswordAnswerAttemptWindowStart = createDate;

                            Models.User user = (Models.User)providerUserKey;
                            user.password = userMembership.Password;
                            user.salt = "";
                            user.userMembership = userMembership;

                            try
                            {
                                int retId = (int)_session.Save(userMembership);
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
            get { return enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return enablePasswordRetrieval; }
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
                return user.username;
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return maxInvalidPasswordAttempts; }
        }


        public override int MinRequiredPasswordLength
        {
            get { return minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return passwordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return passwordStrengthRegularExpression; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return minRequiredNonAlphanumericCharacters; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return requiresUniqueEmail; }
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
            bool isValid = false;
            Models.User user = null;
            
            using(var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory()){
                using(var _session = sessionFactory.OpenSession()){
                    using(ITransaction transaction = _session.BeginTransaction()){
                        try
                        {
                            user = GetUserByUsername(username);
                            if (user == null)
                                return isValid;
                            
                            if(user.userMembership.IsLockedOut)
                                return isValid;

                            if (CheckPassword(password, user.password))
                            {
                                if (user.userMembership.IsApproved)
                                {
                                    isValid = true;
                                    user.userMembership.LastLoginDate = DateTime.Now;
                                    _session.Update(user);
                                    transaction.Commit();
                                }
                            }
                            //else
                                //UpdateFailureCount(username, "password");


                        }
                        catch (Exception e)
                        {
                            if (WriteExceptionsToEventLog)
                            {
                                WriteToEventLog(e, "ValidateUser");
                                throw new ProviderException(exceptionMessage);
                            }
                            throw e;
                        }

                    }

                }
            }
            return isValid;
        }

    }
}