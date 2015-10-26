﻿namespace SimpleIdentityServer.Api.Models
{
    public class Authorize
    {
        public string Code { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public bool IsChecked { get; set; }
    }
}