using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQExcelCreate.DbContext;
using RabbitMQExcelCreate.Hubs;
using RabbitMQExcelCreate.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(new ConnectionFactory()
{
    Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!)
});
builder.Services.AddSingleton<RabbitMQClientService>();
builder.Services.AddSingleton<RabbitMQPublisher>();

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
{
    opt.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    context.Database.Migrate();

    if (!context.UserFiles.Any())
    {
        var user1 = new IdentityUser { UserName = "deneme", Email = "deneme@gmail.com" };
        var user2 = new IdentityUser { UserName = "deneme2", Email = "deneme2@gmail.com" };

        var result1 = await userManager.CreateAsync(user1, "Password12*");
        if (!result1.Succeeded)
        {
            foreach (var error in result1.Errors)
            {
                Console.WriteLine($"User1 error: {error.Code} - {error.Description}");
            }
        }

        var result2 = await userManager.CreateAsync(user2, "Password12*");
        if (!result2.Succeeded)
        {
            foreach (var error in result2.Errors)
            {
                Console.WriteLine($"User2 error: {error.Code} - {error.Description}");
            }
        }
    }
}

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
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<MyHub>("/MyHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
