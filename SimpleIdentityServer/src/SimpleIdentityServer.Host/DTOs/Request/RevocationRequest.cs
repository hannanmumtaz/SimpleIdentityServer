﻿#region copyright
// Copyright 2015 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System.Runtime.Serialization;

namespace SimpleIdentityServer.Host.DTOs.Request
{
    [DataContract]
    public class RevocationRequest
    {
        [DataMember(Name = Constants.RevocationRequestNames.Token)]
        public string Token { get; set; }

        [DataMember(Name = Constants.RevocationRequestNames.TokenTypeHint)]
        public string TokenTypeHint { get; set; }

        [DataMember(Name = Constants.RevocationRequestNames.ClientId)]
        public string ClientId { get; set; }

        [DataMember(Name = Constants.RevocationRequestNames.ClientSecret)]
        public string ClientSecret { get; set; }

        [DataMember(Name = Constants.RevocationRequestNames.ClientAssertionType)]
        public string ClientAssertionType { get; set; }

        [DataMember(Name = Constants.RevocationRequestNames.ClientAssertion)]
        public string ClientAssertion { get; set; }
    }
}
