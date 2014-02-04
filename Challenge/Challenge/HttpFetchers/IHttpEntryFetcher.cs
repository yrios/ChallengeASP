using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Challenge.HttpFetchers
{
    public interface IHttpEntryFetcher
    {
        Entry GetEntry(int id);
        Entry GetEntry();
    }
}
