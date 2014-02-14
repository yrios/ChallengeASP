using Challenge.Configuration;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Challenge
{
    public class WebApiApplication : System.Web.HttpApplication
    {

        public static ISessionFactory SessionFactory { get; private set; }

        protected void Application_Start()
        {
            SessionFactory = FluentNhibernateConfiguration.CreateSessionFactory();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
