using Microsoft.EntityFrameworkCore;
using PharmacyChain.Infrastructure.Persistence;
using Platform.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext (InMemory for demonstration/testing)
builder.Services.AddDbContext<PlatformDbContext, PharmacyChainDbContext>(options =>
    options.UseInMemoryDatabase("PharmacyChainDb"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Seed generic data if needed (optional)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PharmacyChainDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
