using Challenge.HttpResponses;
using Challenge.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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

        private readonly ChallengeCustomMembershipProvider  _membershipProvider = (ChallengeCustomMembershipProvider)Membership.Provider;

        public BasicAuthenticationHandler()
        {

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

            Debug.WriteLine(username + " " + password);

            if (!_membershipProvider.ValidateUser(username, password))
            {
                return CreateUnauthorizedResponse(request);
            }

            Debug.WriteLine("DE USER IS VALID");

            return await base.SendAsync(request, cancellationToken);
        }

        private HttpResponseMessage CreateUnauthorizedResponse(HttpRequestMessage request)
        {
            var response = new Response((int)HttpStatusCode.Unauthorized, "unauthorized", "Unauthorized user credentials");
            return request.CreateResponse(HttpStatusCode.OK, response);
        }
    }
}