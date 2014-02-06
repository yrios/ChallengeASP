using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Challenge.HttpResponses
{
    public class Response
    {
        public Response(int statusCode, string statusDescription, Object response)
        {
            this.statusCode = statusCode;
            this.statusDescription = statusDescription;
            this.response = response;
        }
        public int statusCode { get; set; }
        public string statusDescription { get; set; }
        public Object response { get; set; }
    }
}