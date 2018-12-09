﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RealtimeDatabase.Models.Auth;
using RealtimeDatabase.Models.Commands;
using RealtimeDatabase.Models.Responses;
using RealtimeDatabase.Websocket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeDatabase.Internal.CommandHandler
{
    class RenewCommandHandler : AuthCommandHandlerBase, ICommandHandler<RenewCommand>
    {
        private readonly JwtOptions jwtOptions;
        private readonly JwtIssuer jwtIssuer;
        private readonly AuthDbContextTypeContainer contextTypeContainer;

        public RenewCommandHandler(AuthDbContextAccesor authDbContextAccesor, JwtOptions jwtOptions, JwtIssuer jwtIssuer, AuthDbContextTypeContainer contextTypeContainer, IServiceProvider serviceProvider)
            : base(authDbContextAccesor, serviceProvider)
        {
            this.jwtOptions = jwtOptions;
            this.jwtIssuer = jwtIssuer;
            this.contextTypeContainer = contextTypeContainer;
        }

        public async Task Handle(WebsocketConnection websocketConnection, RenewCommand command)
        {
            if (String.IsNullOrEmpty(command.UserId) || String.IsNullOrEmpty(command.RefreshToken))
            {
                await SendMessage(websocketConnection, new RenewResponse()
                {
                    ReferenceId = command.ReferenceId,
                    Error = new Exception("Userid and refresh token cannot be empty")
                });
                return;
            }

            IRealtimeAuthContext context = GetContext();

            context.RefreshTokens.RemoveRange(context.RefreshTokens.Where(rt => rt.CreatedOn.Add(jwtOptions.ValidFor) < DateTime.UtcNow));
            context.SaveChanges();

            RefreshToken rT = context.RefreshTokens.FirstOrDefault(r => r.UserId == command.UserId && r.RefreshKey == command.RefreshToken);

            if (rT == null)
            {
                await SendMessage(websocketConnection, new RenewResponse()
                {
                    ReferenceId = command.ReferenceId,
                    Error = new Exception("Wrong refresh token")
                });
                return;
            }

            context.RefreshTokens.Remove(rT);

            object usermanager = serviceProvider.GetService(contextTypeContainer.UserManagerType);

            IdentityUser user = await (dynamic)contextTypeContainer.UserManagerType.GetMethod("FindByIdAsync").Invoke(usermanager, new object[] { command.UserId });

            if (user != null)
            {
                RefreshToken newrT = new RefreshToken()
                {
                    UserId = user.Id
                };

                context.RefreshTokens.Add(newrT);
                context.SaveChanges();

                RenewResponse renewResponse = new RenewResponse()
                {
                    ReferenceId = command.ReferenceId,
                    AuthToken = await jwtIssuer.GenerateEncodedToken(user),
                    ExpiresAt = jwtOptions.Expiration,
                    ValidFor = jwtOptions.ValidFor.TotalSeconds,
                    RefreshToken = newrT.RefreshKey
                };

                renewResponse.GenerateUserData(user);
                renewResponse.UserData["Roles"] =
                    await (dynamic)contextTypeContainer.UserManagerType.GetMethod("GetRolesAsync").Invoke(usermanager, new object[] { user });

                await SendMessage(websocketConnection, renewResponse);
                return;
            }

            await SendMessage(websocketConnection, new RenewResponse()
            {
                ReferenceId = command.ReferenceId,
                Error = new Exception("Renew failed")
            });
        }
    }
}