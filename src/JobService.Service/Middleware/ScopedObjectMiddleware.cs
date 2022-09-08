using JobService.Components;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace JobService.Service.Middleware
{
    public class ScopedObjectMiddleware : IMiddleware
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ScopedObject _scopedObject;

        public ScopedObjectMiddleware(
            IHttpContextAccessor httpContextAccessor,
            ScopedObject scopedObject)
        {
            _httpContextAccessor = httpContextAccessor;
            _scopedObject = scopedObject;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.MyValueKey, out var value))
            {
                _scopedObject.Value = value;
            }

            return next(context);
        }
    }
}
