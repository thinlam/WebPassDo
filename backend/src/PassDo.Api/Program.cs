using Microsoft.OpenApi.Models;
using PassDo.Api.Hubs;
using PassDo.Api.Middleware;
using PassDo.Api.Realtime;
using PassDo.Application;
using PassDo.Application.Common.Interfaces;
using PassDo.Infrastructure;
using PassDo.Infrastructure.Options;
using PassDo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new PassDo.Api.Serialization.UtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new PassDo.Api.Serialization.UtcNullableDateTimeJsonConverter());
    });

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PassDo API",
        Version = "v1",
        Description = "API for PassDo - personal item resale marketplace"
    });

    options.DescribeAllParametersInCamelCase();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {access_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

var enableSwagger = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue("Swagger:Enabled", false);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PassDo API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

var fileStorageOptions = app.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
    ?? new FileStorageOptions();

var uploadRoot = Path.IsPathRooted(fileStorageOptions.RootPath)
    ? fileStorageOptions.RootPath
    : Path.Combine(app.Environment.ContentRootPath, fileStorageOptions.RootPath);

Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadRoot),
    RequestPath = fileStorageOptions.RequestPath
});

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<PresenceHub>("/hubs/presence");
app.MapControllers();
app.MapHealthChecks("/health");

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

public partial class Program;
