using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Challenge.HttpFetchers
{
    public interface IHttpUserFetcher
    {
        User GetUser(int id);
    }
}
