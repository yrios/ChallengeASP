using Challenge.HttpResponses;
using Challenge.Security;
using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Security;

namespace Challenge.Controllers
{
    [LoggingNHibernateSession]
    public class UserController : ApiController
    {
        private readonly ChallengeCustomMembershipProvider _membershipProvider = WebApiApplication.MembershipProvider;
        // GET api/user
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/user/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/user
        public HttpResponseMessage Post([FromBody]User user)
        {
            try
            {

                MembershipCreateStatus status = new MembershipCreateStatus();
                var newUser = _membershipProvider.CreateUser(user.username, user.password, user.email, "", "", true, user, out status);

                var response = new Response((int)HttpStatusCode.OK, "success", _membershipProvider.GetErrorMessage(status));
                return Request.CreateResponse(HttpStatusCode.OK, response);

            }
            catch (Exception e)
            {
                var response = new Response((int)HttpStatusCode.InternalServerError, "fail", "User not created");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, response);
            }
        }

        // PUT api/user/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/user/5
        public void Delete(int id)
        {
        }
    }
}
