using Challenge.Models;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Challenge.HttpFetchers
{
    public class HttpUserFetcher : IHttpUserFetcher
    {
        private readonly ISession _session;

        public HttpUserFetcher(ISession session)
        {
            _session = session;
        }

        public User GetUser(int id)
        {
            var user = _session.Get<User>(id);
            if(user == null)
            {
                throw new HttpResponseException(
                    new HttpResponseMessage 
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            ReasonPhrase = string.Format("User {0} not found",id)
                        }); 
            }

            return user;
        }
    }
}