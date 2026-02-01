using System.Reflection;
using Microsoft.OpenApi.Models;

namespace TransactionAggregation.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Transaction Aggregation API",
                Version = "v1",
                Description = "API for aggregating customer transaction data with categorization and fraud detection",
                Contact = new OpenApiContact
                {
                    Name = "Fiskani Chirwa"
                }
            });

            // Include XML comments from API project
            var apiXmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
            if (File.Exists(apiXmlPath))
            {
                options.IncludeXmlComments(apiXmlPath);
            }

            // Include XML comments from Models project
            var modelsXmlFile = "TransactionAggregation.Models.xml";
            var modelsXmlPath = Path.Combine(AppContext.BaseDirectory, modelsXmlFile);
            if (File.Exists(modelsXmlPath))
            {
                options.IncludeXmlComments(modelsXmlPath);
            }

            // Add api-version header parameter globally
            options.AddSecurityDefinition("ApiVersion", new OpenApiSecurityScheme
            {
                Description = "API Version (use '1.0')",
                Name = "api-version",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiVersion"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.DisplayRequestDuration();
        });

        return app;
    }
}