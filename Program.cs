using ErenTravel3API.Data;
using ErenTravel3API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options => options
    .AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod())
);

builder.Services.AddControllers();

// Custom Services - Dependency Injection
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPackageService, PackageService>();

// Configure DbContext with MySQL
var connectionString = "server=localhost;port=3306;user id=root;password=Shkoder2026.;database=ErenTravelDb;default command timeout=600;connect timeout=600;AllowZeroDateTime=True;ConvertZeroDateTime=True";
var serverVersion = new MySqlServerVersion(new Version(9, 2, 0));

builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    })
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment())
);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();