using Npgsql;
using PhysicianBooking.Services.Implementation;
using PhysicianBooking.Services.Interface;
using System.Data;

var builder = WebApplication.CreateBuilder(args);


// 1. Add CORS services
// Allowed origins come from configuration (Cors:AllowedOrigins) so Dev and Production
// can each allow their own frontend URL. No trailing slashes!
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddScoped<IDbConnection>(x =>
    new NpgsqlConnection(
        builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<PlaceRepository>();

// HTTP clients / services
builder.Services.AddHttpClient<GooglePlacesService>();
builder.Services.AddScoped<IPlaceRepository, PlaceRepository>();
builder.Services.AddScoped<ISpecialtiesRepository, SpecialtiesRepository>();
builder.Services.AddScoped<ILeadsRepository, LeadsRepository>();
builder.Services.AddScoped<IInquiryRepository, InquiryRepository>();
builder.Services.AddHttpClient<IEmailService, EmailJsService>();
// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// (previous custom AddOpenApi removed and replaced with Swashbuckle)

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PhysicianBooking API V1");
        // c.RoutePrefix = string.Empty; // Uncomment to serve Swagger UI at app root (/)
    });
}
app.UseCors("AllowAngularApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Used by the deployment pipeline to confirm the API started correctly.
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
