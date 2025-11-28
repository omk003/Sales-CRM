using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SCRM_dev.Services;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using scrm_dev_mvc.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("ProductionConnection");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    }));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IGmailService, GmailService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<ICallService, CallService>();

builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IGmailService, GmailService>(); 
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>(); 

builder.Services.AddScoped<IDealService, DealService>();
builder.Services.AddScoped<ITaskService, TaskService>();

builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>(); 
 

builder.Services.AddHostedService<GmailPollingHostedService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

try
{
    var app = builder.Build();

    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging();
    if (!app.Environment.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }
    
    app.UseHttpsRedirection();


    app.UseStaticFiles();

    //app.Use(async (context, next) =>
    //{
    //    Log.Information("HTTP {Method} {Path} invoked by {IP}",
    //        context.Request.Method,
    //        context.Request.Path,
    //        context.Connection.RemoteIpAddress);

    //    await next.Invoke();

    //    Log.Information("Response {StatusCode} for {Path}",
    //        context.Response.StatusCode, context.Request.Path);
    //});

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseRouting();
    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=LandingPage}/{id?}");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Critical error starting the app: {ex.Message}");
    Log.Fatal(ex, "Critical error starting the app");
}
