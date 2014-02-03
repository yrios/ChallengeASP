using Challenge.Models;
using Challenge.Configuration;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Challenge.Controllers
{

    public class EntryController : ApiController
    {
        
        // GET api/entry
        public IEnumerable<Entry> Get()
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    var entries = _session.QueryOver<Entry>().List();
                    return entries;
                }
            } 
        }

        // GET api/entry/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/entry
        public void Post([FromBody]string value)
        {
        }

        // PUT api/entry/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/entry/5
        public void Delete(int id)
        {
        }
    }
}
