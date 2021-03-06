﻿#region copyright
// Copyright 2016 Habart Thierry
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.Scim.Core.Stores;
using SimpleIdentityServer.Scim.Db.EF.Helpers;
using SimpleIdentityServer.Scim.Db.EF.Stores;
using System;

namespace SimpleIdentityServer.Scim.Db.EF
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryDb(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            RegisterServices(services);
            services.AddEntityFramework()
                 .AddDbContext<ScimDbContext>(options => options.UseInMemoryDatabase().ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            return services;
        }

        public static IServiceCollection AddSqlServerDb(this IServiceCollection services, string connectionString, string migrationAssembly)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(migrationAssembly))
            {
                throw new ArgumentNullException(nameof(migrationAssembly));
            }

            RegisterServices(services);
            services.AddEntityFramework()
                .AddDbContext<ScimDbContext>(options => options.UseSqlServer(connectionString, opts => opts.MigrationsAssembly(migrationAssembly)));
            return services;
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<ITransformers, Transformers>();
            services.AddTransient<IRepresentationStore, RepresentationStore>();
            services.AddTransient<ISchemaStore, SchemaStore>();
        }
    }
}
