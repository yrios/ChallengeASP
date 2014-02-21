using Challenge.HttpResponses;
using Challenge.Security;
using NHibernate;
using NHibernate.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace Challenge
{
    
    public class BasicAuthenticationHandler : DelegatingHandler
    {
        public const string BasicScheme = "Basic";
        public const string ChallengeAuthenticationHeaderName = "WWW-Authenticate";
        public const char AuthorizationHeaderSeparator = ':';
        private ISessionFactory SessionFactory { get; set; }
        private readonly ChallengeCustomMembershipProvider _membershipProvider = WebApiApplication.MembershipProvider;

        public BasicAuthenticationHandler()
        {
            SessionFactory = WebApiApplication.SessionFactory;
        }
        
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Debug.WriteLine("Se ejecuta el Handler");
            var authHeader = request.Headers.Authorization;
            if (authHeader == null)
            {
                //return CreateUnauthorizedResponse();
                return await base.SendAsync(request, cancellationToken);
            }
                
            if(authHeader.Scheme != BasicScheme)
            {
                //return CreateUnauthorizedResponse();
                return await base.SendAsync(request, cancellationToken);
            }

            var encodedCredentials = authHeader.Parameter;
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.ASCII.GetString(credentialBytes);
            var credentialsParts = credentials.Split(AuthorizationHeaderSeparator);

            if(credentialsParts.Length != 2)
            {
                return CreateUnauthorizedResponse(request);
            }
            var username = credentialsParts[0].Trim();
            var password = credentialsParts[1].Trim();

          
            var session = SessionFactory.OpenSession();
            CurrentSessionContext.Bind(session);
            session.BeginTransaction();

            

            if (!_membershipProvider.ValidateUser(username, password))
            {
                CloseSession();
                return CreateUnauthorizedResponse(request);
            }

            SetPrincipal(username);

            CloseSession();
            return await base.SendAsync(request, cancellationToken);
        }

        private HttpResponseMessage CreateUnauthorizedResponse(HttpRequestMessage request)
        {
            var response = new Response((int)HttpStatusCode.Unauthorized, "unauthorized", "Unauthorized user credentials");
            return request.CreateResponse(HttpStatusCode.OK, response);
        }

        private void SetPrincipal(string username)
        {
            //var roles = _membershipAdapter.
            //GetRolesForUser(username);
            Models.User user = _membershipProvider.GetUserByUsername(username);
            var identity = CreateIdentity(user.username, user);

            var principal = new ClaimsPrincipal(identity);
            Thread.CurrentPrincipal = principal;

            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        private GenericIdentity CreateIdentity(string username, Models.User modelUser)
        {
            var identity = new GenericIdentity(username, BasicScheme);
            identity.AddClaim(new Claim(ClaimTypes.Sid, modelUser.id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.GivenName, modelUser.username));
            identity.AddClaim(new Claim(ClaimTypes.Email, modelUser.email));
            return identity;
        }

        private void CloseSession()
        {
            var session = SessionFactory.GetCurrentSession();
            var transaction = session.Transaction;
            if (transaction != null && transaction.IsActive)
            {
                transaction.Commit();
            }
            session = CurrentSessionContext.Unbind(SessionFactory);
            session.Close();
            session.Dispose();
        }
    }
}