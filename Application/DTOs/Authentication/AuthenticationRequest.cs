using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Authentication
{
    public class AuthenticationRequest
    {
        [Required]
        /// <summary>
        /// Email address, phone number or username used for login.
        /// </summary>
        public string Credential { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
