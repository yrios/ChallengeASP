using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.TypeMappers
{
    public class UserMapper : IUserMapper
    {
        public User CreateUser(string username, string twitter, string email, int userId)
        {
            return new User
                       {
                           id = userId,
                           username = username,
                           email = email,
                           twitterAccount = twitter
                       };
        }


        public User CreateUser(User modelUser)
        {
            throw new NotImplementedException();
        }
    }
}