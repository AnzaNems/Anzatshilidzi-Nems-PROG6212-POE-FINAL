var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session keeps the logged-in user's JWT token, role, id and name - that is
// ALL the MVC app stores. The API remains the single source of truth.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Named HttpClient for every call to the RaceDay API. The base URL lives in
// appsettings.json ("Api:BaseUrl") and is overridden by the Api__BaseUrl
// environment variable inside the Docker container.
builder.Services.AddHttpClient("Api", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:7123";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();          // must run before the controllers execute

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
