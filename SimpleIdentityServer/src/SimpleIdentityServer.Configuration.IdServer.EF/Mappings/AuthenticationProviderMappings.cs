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

using Microsoft.EntityFrameworkCore;
using SimpleIdentityServer.Configuration.IdServer.EF.Models;

namespace SimpleIdentityServer.Configuration.IdServer.EF.Mappings
{
    internal static class AuthenticationProviderMappings
    {
        #region Public static methods

        public static void AddAuthenticationProviderMappings(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthenticationProvider>()
                .ToTable("AuthenticationProviders")
                .HasKey(a => a.Name);
            modelBuilder.Entity<AuthenticationProvider>()
                .HasMany(a => a.Options)
                .WithOne(o => o.AuthenticationProvider)
                .HasForeignKey(o => o.AuthenticationProviderId);
        }

        #endregion
    }
}
