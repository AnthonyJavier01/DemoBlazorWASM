using BlazorApp1;
using BlazorApp1.Commons;
using BlazorApp1.Commons.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
//
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
var environment = builder.HostEnvironment.Environment;
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: true);



//
builder.Services.AddFluentUIComponents();
builder.Services.AddWebDependencies(builder.Configuration);
builder.Services.AddGraphHttpClientServices(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(PolicyConstants.Administrator, policy =>
        policy.RequireClaim(ClaimConstants.AppRole, ClaimConstants.RoleAdministrator));

    options.AddPolicy(PolicyConstants.Developer, policy =>
        policy.RequireClaim(ClaimConstants.AppRole, ClaimConstants.RoleDeveloper));

    options.AddPolicy(PolicyConstants.AdministratorAndDeveloper, policy =>
        policy.RequireClaim(ClaimConstants.AppRole, ClaimConstants.RoleAdministrator, ClaimConstants.RoleDeveloper));
});

await builder.Build().RunAsync();
