using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocusignEntity
{
    public class LoginResponse
    {
        public string AccountId { get; set; }
        public string BaseUrl { get; set; }
        public string Email { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public string SiteDescription { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }
}
