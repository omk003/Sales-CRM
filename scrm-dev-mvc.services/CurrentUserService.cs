using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using scrm_dev_mvc.services.Interfaces;
using System.Security.Claims;

namespace scrm_dev_mvc.services
{
    
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                // This can happen in background services, return empty or throw
                return Guid.Empty;
            }

            var value = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(value) ? Guid.Parse(value) : Guid.Empty;
        }

        public bool IsAuthenticated()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public bool IsInRole(string role)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.IsInRole(role) ?? false;
        }
    }
}
