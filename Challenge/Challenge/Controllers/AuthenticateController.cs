using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Challenge.Controllers
{
    public class AuthenticateController : ApiController
    {
        // GET api/authenticate
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/authenticate/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/authenticate
        public void Post([FromBody]string value)
        {
        }

        // PUT api/authenticate/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/authenticate/5
        public void Delete(int id)
        {
        }
    }
}
