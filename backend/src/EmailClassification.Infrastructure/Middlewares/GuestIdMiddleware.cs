using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure.Implement;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Middlewares
{
    public class GuestIdMiddleware
    {
        private readonly RequestDelegate _next;

        public GuestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context, IGuestContext guestContext)
        {
            if (context.Request.Headers.TryGetValue("guestId", out var guestId))
            {
                if (guestContext is GuestContext concrete)
                {
                    concrete.GuestId = guestId;
                }
                if (!string.IsNullOrEmpty(guestId))
                {
                    context.Items["guestId"] = guestId;
                }
            }
            await _next(context);
        }
    }
}
