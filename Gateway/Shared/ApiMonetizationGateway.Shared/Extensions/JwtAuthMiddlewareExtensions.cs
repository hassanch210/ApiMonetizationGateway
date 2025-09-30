using ApiMonetizationGateway.Shared.Middleware;
using Microsoft.AspNetCore.Builder;

namespace ApiMonetizationGateway.Shared.Extensions
{
    public static class JwtAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtAuthMiddleware>();
        }
    }
}