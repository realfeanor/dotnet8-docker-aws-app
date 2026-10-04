using Autofac;
using Autofac.Extensions.DependencyInjection;
using Business.DependencyResolvers.Autofac;
using Core.DependencyResolvers;
using Core.Extensions;
using Core.Utilities.IoC;
using Core.Utilities.Security.Encyption;
using Core.Utilities.Security.Jwt;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
                                            builder.RegisterModule(new AutofacBusinessModule()));
// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Field validation belongs to FluentValidation aspects in the business layer.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddCors();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

var configuration = (IConfiguration)builder.Configuration;
var tokenOptions = configuration.GetSection("TokenOptions").Get<TokenOptions>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = tokenOptions.Issuer,
        ValidAudience = tokenOptions.Audience,
        IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
        ClockSkew = TimeSpan.Zero
    };
});

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"ConnectionStrings:Default is missing for environment '{builder.Environment.EnvironmentName}'. " +
        "Set it in appsettings.Development.json, user secrets, or the ConnectionStrings__Default environment variable.");
}

builder.Services.AddDbContext<NorthwindContext>(options =>
	options.UseSqlServer(
		connectionString,
		sqlOptions =>
		{
			sqlOptions.EnableRetryOnFailure(
				maxRetryCount: 5,
				maxRetryDelay: TimeSpan.FromSeconds(10),
				errorNumbersToAdd: null);
		}));

builder.Services.AddDependencyResolvers(new ICoreModule[]
            {
                new CoreModule(),
            });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(swagger =>
{

    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = ".NET API",
        Description = ".NET 8 API"
    });

    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter ‘Bearer’ [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {{
                    new OpenApiSecurityScheme
                    {
                    Reference = new OpenApiReference
                    {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                    }
                    },
                    new string[] {}}
                    });

});

//builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 APPLY MIGRATIONS ONLY IN NON-PRODUCTION
if (!app.Environment.IsProduction())
{
	using var scope = app.Services.CreateScope();
	try
	{
		var db = scope.ServiceProvider.GetRequiredService<NorthwindContext>();
		db.Database.Migrate();
	}
	catch (Exception ex)
	{
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "An error occurred while applying migrations.");
		throw;
	}
}


Core.CrossCuttingConcerns.Logging.Log4Net.LoggerServiceBase.Configure(
    connectionString, Path.Combine(AppContext.BaseDirectory, "log4net.config"));
app.Lifetime.ApplicationStopped.Register(() => log4net.LogManager.Shutdown());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.UseSwagger();
app.UseSwaggerUI();

app.ConfigureCustomExceptionMiddleware();

app.UseCors(x => x.AllowAnyMethod().AllowAnyOrigin().AllowAnyHeader());

//app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}


app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok("OK"));

app.Run();
