using Challenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Challenge.TypeMappers
{
    public interface IUserMapper
    {
        User CreateUser(string username, string twitter, string email, int userId);
        User CreateUser(User modelUser);
    }
}
