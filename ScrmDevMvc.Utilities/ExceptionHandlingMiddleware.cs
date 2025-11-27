using Microsoft.AspNetCore.Http;
using Serilog;
using System.Net;
using System.Threading.Tasks;


public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unhandled exception occurred while processing the request");

            
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "An unexpected error occurred. Please try again later. Check If contacts company and updated company is same."
                });
            }
            else
            {
                
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}