
using ApplicationsToolsTechnologies.Data;
using ApplicationsToolsTechnologies.Repositories;
using ApplicationsToolsTechnologies.Repositories.Interfaces;
using ApplicationsToolsTechnologies.Services;
using ApplicationsToolsTechnologies.Services.Interfaces;
using Microsoft.Data.SqlClient;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ApplicationsToolsTechnologies.Data.WorkbookDataStore>();
builder.Services.AddScoped<SqlConnection>(_ =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("ConsilioDb")
        ?? throw new InvalidOperationException(
            "ConsilioDb connection string is missing.");

    return new SqlConnection(connectionString);
});

builder.Services.AddScoped<DatabaseConnectionTest>();
builder.Services.AddScoped<SqlDataAccess>();

builder.Services.AddScoped<IApplicationsRepository, ApplicationsRepository>();
builder.Services.AddScoped<IApplicationsService, ApplicationsService>();

builder.Services.AddScoped<ITechnologiesRepository, TechnologiesRepository>();
builder.Services.AddScoped<ITechnologiesService, TechnologiesService>();

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

app.Run();
