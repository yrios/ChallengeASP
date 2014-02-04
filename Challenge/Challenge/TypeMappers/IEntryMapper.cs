using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Challenge.TypeMappers
{
    public interface IEntryMapper
    {
        Entry CreateEntry(Models.Entry modelEntry);
    }
}
