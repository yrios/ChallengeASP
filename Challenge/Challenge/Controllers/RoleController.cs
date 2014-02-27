using Challenge.HttpResponses;
using Challenge.Models;
using Challenge.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Challenge.Controllers
{
    [LoggingNHibernateSession]
    public class RoleController : ApiController
    {
        private readonly ChallengeCustomRoleProvider _roleProvider = WebApiApplication.RoleProvider;
        // GET api/role
        public IEnumerable<string> Get()
        {
            return _roleProvider.GetAllRoles();
        }

        // GET api/role/5
        public string Get(int id)
        {
            return "value";
        }

        
        // POST api/role
        [Authorize(Roles = "Admin")]
        public HttpResponseMessage Post([FromBody]RoleMembership role)
        {
            try
            {
                _roleProvider.CreateRole(role.RoleName);

                var response = new Response((int)HttpStatusCode.OK, "success", "Role Created");
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception)
            {
                var response = new Response((int)HttpStatusCode.InternalServerError, "fail", "Role not created");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, response);
            }
        }

        // PUT api/role/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/role/5
        public HttpResponseMessage Delete([FromUri] String rolename, bool populaterole)
        {
            try
            {
                if (_roleProvider.DeleteRole(rolename, populaterole))
                {
                    var response = new Response((int)HttpStatusCode.OK, "success", "Role Deleted");
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
                else
                {
                    var response = new Response((int)HttpStatusCode.OK, "success", "Role Not Deleted");
                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }

            }
            catch (Exception e)
            {
                var response = new Response((int)HttpStatusCode.InternalServerError, "fail", e.Message);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, response);
            }
        }
    }
}
