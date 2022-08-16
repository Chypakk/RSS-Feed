using TaskTwo.Models;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Configuration.AddXmlFile("config.xml");

builder.Services.Configure<MyConfig>(options => builder.Configuration.GetSection("RSS").Bind(options));

var config = new MyConfig();
builder.Configuration.GetSection("RSS").Bind(config);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(IPAddress.Parse(config.ipAddress));
    options.ForwardedForHeaderName = "X-Forwarded-For-My-Custom-Header-Name";
    
});

var app = builder.Build();

app.UseForwardedHeaders();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
