using NHibernate;
using NHibernate.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;



namespace Challenge
{
    public class LoggingNHibernateSessionAttribute : ActionFilterAttribute
    {
        public LoggingNHibernateSessionAttribute()
        {
            SessionFactory = WebApiApplication.SessionFactory;
        }

        private ISessionFactory SessionFactory { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            Debug.WriteLine("On action Executing");
            var session = SessionFactory.OpenSession();
            CurrentSessionContext.Bind(session);
            session.BeginTransaction();
            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var session = SessionFactory.GetCurrentSession();
            var transaction = session.Transaction;
            if (transaction != null && transaction.IsActive)
            {
                transaction.Commit();
            }
            session = CurrentSessionContext.Unbind(SessionFactory);
            session.Close();
            session.Dispose();
        }

    }
}