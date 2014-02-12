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

namespace Challenge
{
    public class BasicAuthenticationHandler : DelegatingHandler
    {
        public const string BasicScheme = "Basic";
        public const string ChallengeAuthenticationHeaderName = "WWW-Authenticate";
        public const char AuthorizationHeaderSeparator = ':';

        private readonly ChallengeCustomMembershipProvider _membershipProvider;

        public BasicAuthenticationHandler()
        {

        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Debug.WriteLine("Se ejecuta el Handler");
            var authHeader = request.Headers.Authorization;
            if (authHeader == null)
            {
                //return CreateUnauthorizedResponse();
                return base.SendAsync(request, cancellationToken);
            }
                
            if(authHeader.Scheme != BasicScheme)
            {
                //return CreateUnauthorizedResponse();
                return base.SendAsync(request, cancellationToken);
            }

            var encodedCredentials = authHeader.Parameter;
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.ASCII.GetString(credentialBytes);
            var credentialsParts = credentials.Split(AuthorizationHeaderSeparator);

            if(credentialsParts.Length != 2)
            {
                return CreateUnauthorizedResponse();
            }
            var username = credentialsParts[0].Trim();
            var password = credentialsParts[1].Trim();

            Debug.WriteLine(username + " " + password);

            if (!_membershipProvider.ValidateUser(username, password))
            {
                return CreateUnauthorizedResponse();
            }

            

            return base.SendAsync(request, cancellationToken);
        }

        private Task<HttpResponseMessage> CreateUnauthorizedResponse()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            //response.Headers.Add(ChallengeAuthenticationHeaderName, BasicScheme);

            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(response);
            return taskCompletionSource.Task;
        }
    }
}