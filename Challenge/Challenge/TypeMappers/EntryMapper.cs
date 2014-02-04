using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.TypeMappers
{
    public class EntryMapper : IEntryMapper
    {
        private readonly IUserMapper _userMapper;

        public EntryMapper(IUserMapper userMapper)
        {
            _userMapper = userMapper;
        }
        public Entry CreateEntry(Models.Entry modelEntry)
        {
            var entry = new Entry
            {
                id = modelEntry.id,
                creationDate = modelEntry.creationDate,
                title = modelEntry.title,
                content = modelEntry.content,
                user = _userMapper.CreateUser(modelEntry.user)
            };

            return entry;
        }
    }
}