using Challenge.Models;
using Challenge.Configuration;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Challenge.HttpFetchers;
using Challenge.TypeMappers;

namespace Challenge.Controllers
{

    public class EntryController : ApiController
    {

        // GET api/<controller>
        public IEnumerable<Entry> Get()
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    var modelentries = _session.QueryOver<Entry>().List();
                    return modelentries;
                }
            } 
        }

        // GET api/<controller> retorna las entradas de un usuario especifico
        public IEnumerable<Entry> GetByUserId([FromUri] int user_id)
        {
            Entry entry = null;
            User user   = null;
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    var entries = _session.QueryOver<Entry>(() => entry)
                        .JoinAlias(() => entry.user, () => user)
                        .Where(() => user.id == user_id).List<Entry>();

                    return entries;
                }
            }
        }

        // GET api/<controller>?entry_id=5 retorna un entrada con el id especificado
        public Entry Get([FromUri] int entry_id)
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    Entry entry = _session.Get<Entry>(entry_id);
                    if(entry == null)
                    {
                        Entry blank_entry = new Entry();
                        return blank_entry;
                    }
                    return entry;
                }
            } 
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}
