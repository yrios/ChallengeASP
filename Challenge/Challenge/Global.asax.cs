using Challenge.Configuration;
using Challenge.Security;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace Challenge
{
    public class WebApiApplication : System.Web.HttpApplication
    {

        public static ISessionFactory SessionFactory { get; private set; }
        public static ChallengeCustomMembershipProvider MembershipProvider { get; private set; }

        protected void Application_Start()
        {
            SessionFactory = FluentNhibernateConfiguration.CreateSessionFactory();
            MembershipProvider = (ChallengeCustomMembershipProvider)Membership.Provider;
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
