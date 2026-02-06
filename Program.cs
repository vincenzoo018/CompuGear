using Microsoft.EntityFrameworkCore;
using CompuGear.Models;
using CompuGear.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Suppress automatic 400 response for invalid model states
        // This allows us to handle validation errors in the controllers
        options.SuppressModelStateInvalidFilter = true;
    });

// Add Entity Framework DbContext
builder.Services.AddDbContext<CompuGearDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support (for user sessions)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed test users on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CompuGearDbContext>();
    await SeedTestUsersAsync(context);
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

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Seed test users for development
async Task SeedTestUsersAsync(CompuGearDbContext context)
{
    try
    {
        var salt = "CompuGearSalt2024";
        var passwordHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("password123" + salt)));

        var usersToSeed = new List<(string username, string email, string firstName, string lastName, int roleId)>
        {
            ("admin", "admin@compugear.com", "System", "Administrator", 1),
            ("company.admin", "companyadmin@compugear.com", "Company", "Admin", 2),
            ("sarah.johnson", "sales@compugear.com", "Sarah", "Johnson", 3),
            ("mike.chen", "support@compugear.com", "Mike", "Chen", 4),
            ("emily.brown", "marketing@compugear.com", "Emily", "Brown", 5),
            ("john.doe", "billing@compugear.com", "John", "Doe", 6),
            ("james.wilson", "inventory@compugear.com", "James", "Wilson", 8)  // RoleId 8 = Inventory Staff
        };

        foreach (var (username, email, firstName, lastName, roleId) in usersToSeed)
        {
            // Check by email first
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (existingUser != null)
            {
                // Update existing user with correct hash and role
                existingUser.PasswordHash = passwordHash;
                existingUser.Salt = salt;
                existingUser.RoleId = roleId;
                existingUser.IsActive = true;
                existingUser.IsEmailVerified = true;
                existingUser.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Check if username already exists
                var existingByUsername = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (existingByUsername != null)
                {
                    // Update existing user with correct email and hash
                    existingByUsername.Email = email;
                    existingByUsername.PasswordHash = passwordHash;
                    existingByUsername.Salt = salt;
                    existingByUsername.RoleId = roleId;
                    existingByUsername.IsActive = true;
                    existingByUsername.IsEmailVerified = true;
                    existingByUsername.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new user
                    context.Users.Add(new User
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        FirstName = firstName,
                        LastName = lastName,
                        RoleId = roleId,
                        CompanyId = 1,
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("✓ Test users seeded successfully!");
        Console.WriteLine("  Login credentials: [email]@compugear.com / password123");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ User seeding skipped: {ex.Message}");
        // Don't throw - just log and continue
    }
}
