using Challenge.Models;
using Challenge.Configuration;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Collections;
using Challenge.TypeMappers;
using NHibernate.Criterion;
using Challenge.HttpResponses;
using Newtonsoft.Json;

namespace Challenge.Controllers
{
    [LoggingNHibernateSession]
    public class EntryController : ApiController
    {

        public EntryController()
        {

        }

        // GET api/<controller>
        [Authorize(Roles = "Admin")]
        public IEnumerable<EntryDTO> Get()
        {
            var _session = WebApiApplication.SessionFactory.GetCurrentSession();
            var _ListEntry = new List<EntryDTO>();
            var entries = _session.QueryOver<Entry>().List();

            foreach (var item in entries)
            {
                EntryDTO _entry = new EntryDTO();
                _ListEntry.Add(EntryDTO.creatEntry(item));
            }
            return _ListEntry;
        }

        // GET api/<controller>?user_id[id] retorna las entradas de un usuario especifico
        public IEnumerable<EntryDTO> GetByUserId([FromUri] int user_id)
        {
            Entry entry = null;
            User user   = null;
            var _ListEntry = new List<EntryDTO>();

            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    var entries = _session.QueryOver<Entry>(() => entry)
                        .JoinAlias(() => entry.user, () => user)
                        .Where(() => user.id == user_id).List<Entry>();

                    foreach (var item in entries)
                    {
                        _ListEntry.Add(EntryDTO.creatEntry(item));
                    }

                    return _ListEntry;
                }
            }
        }

        // GET api/<controller>?entry_id=[id] retorna un entrada con el id especificado
        public IEnumerable<EntryDTO> Get([FromUri] int entry_id)
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    var entry = _session.Get<Entry>(entry_id);
                    if(entry == null)
                    {
                        return null;
                    }
                    var entryResult = new List<EntryDTO>();
                    entryResult.Add(EntryDTO.creatEntry(entry));
                    return entryResult;
                }
            } 
        }

        // POST api/<controller> request payload {creationDate, tittle, content, user}
        public HttpResponseMessage Post([FromBody]EntryDTO entryDto)
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    using(ITransaction transaction = _session.BeginTransaction())
                    {
                        try
                        {
                            User author = _session.CreateCriteria<User>().Add(Restrictions.Eq("username", entryDto.user)).UniqueResult<User>();
                            Entry entry = new Entry(
                                entryDto.id,
                                entryDto.creationDate,
                                entryDto.title,
                                entryDto.content,
                                author);

                            _session.Save(entry);
                            transaction.Commit();

                            var response = new Response((int)HttpStatusCode.OK,"success",entryDto);
                            return Request.CreateResponse(HttpStatusCode.OK, response);
                            
                        }catch(Exception){
                            transaction.Rollback();
                            var response = new Response((int)HttpStatusCode.InternalServerError, "error", "internal server error");
                            return Request.CreateResponse(HttpStatusCode.InternalServerError, response);
                        }
                    }
                }
            }
        }

        // PUT api/<controller>/5
        public HttpResponseMessage Put([FromBody]EntryDTO entryDto)
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    Entry oldEntry = _session.Get<Entry>(entryDto.id);

                    if (oldEntry == null)
                    {
                        var response = new Response((int)HttpStatusCode.OK, "error", "entry not found");
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }

                    using (ITransaction transaction = _session.BeginTransaction())
                    {
                        Entry entry = new Entry(
                                entryDto.id,
                                entryDto.creationDate,
                                entryDto.title,
                                entryDto.content,
                                oldEntry.user);

                        _session.Merge(entry);
                        transaction.Commit();

                        var response = new Response((int)HttpStatusCode.OK, "success", EntryDTO.creatEntry(entry));
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                }
            }
        }

        // DELETE api/<controller>/5
        public HttpResponseMessage Delete([FromUri] int id)
        {
            using (var sessionFactory = FluentNhibernateConfiguration.CreateSessionFactory())
            {
                using (var _session = sessionFactory.OpenSession())
                {
                    Entry entry = _session.Get<Entry>(id);

                    if (entry == null)
                    {
                        var response = new Response((int)HttpStatusCode.OK, "error", "entry not found");
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }

                    using (ITransaction transaction = _session.BeginTransaction())
                    {
                        _session.Delete(entry);
                        transaction.Commit();

                        var response = new Response((int)HttpStatusCode.OK, "success", "entry removed");
                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                }
            }
        }
    }
}
