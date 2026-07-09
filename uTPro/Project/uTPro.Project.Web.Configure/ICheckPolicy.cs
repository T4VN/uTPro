using Microsoft.AspNetCore.Http;

namespace uTPro.Project.Web.Configure
{
    public interface ICheckPolicy
    {
        string Check(HttpContext httpContext);
    }
}
