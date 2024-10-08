﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trade.Common.Settings;

namespace Trade.Common.Identity
{
    public class ConfigureJwtBearerOptions: IConfigureNamedOptions<JwtBearerOptions>
    {
        private const string AccessTokenParametr = "access_token";
        private const string MessageHubPath = "/messageHub";

        private readonly IConfiguration _configuration;

        public ConfigureJwtBearerOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(string? name, JwtBearerOptions options)
        {
            if (name == JwtBearerDefaults.AuthenticationScheme)
            {
                var serviceSettigns = _configuration.GetSection(nameof(ServiceSettings))
                    .Get<ServiceSettings>();

                options.Authority = serviceSettigns.Authority;
                options.Audience = serviceSettigns.ServiceName;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query[AccessTokenParametr];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments(MessageHubPath))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        }

        public void Configure(JwtBearerOptions options)
        {
            Configure(Options.DefaultName, options);
        }
    }
}
