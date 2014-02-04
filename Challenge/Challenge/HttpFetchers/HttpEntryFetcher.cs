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
    public class HttpEntryFetcher : IHttpEntryFetcher
    {
        private readonly ISession _session;

        public HttpEntryFetcher(ISession session)
        {
            _session = session;
        }
        public Entry GetEntry(int id)
        {
            var entry = _session.Get<Entry>(id);
            if (entry == null)
            {
                throw new HttpResponseException(
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        ReasonPhrase = string.Format("Entry {0} not found", id)
                    });
            }

            return entry;
        }


        public Entry GetEntry()
        {
            throw new NotImplementedException();
        }
    }
}