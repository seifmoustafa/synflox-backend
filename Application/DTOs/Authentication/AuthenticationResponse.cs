using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Authentication
{
    public class AuthenticationResponse
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }

        //JSONIgnore is an attribute that restricts the property from being shown in JSON results.
        //[JsonIgnore]
        public string RefreshToken { get; set; }

        public string ErrorMessage { get; set; }
    }
}
