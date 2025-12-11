using FluentValidation;
using FluentValidation.AspNetCore;
using HabitRPG.Api.Validators;
using System.Reflection;

namespace HabitRPG.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

            services.AddFluentValidationAutoValidation(config =>
            {
                config.DisableDataAnnotationsValidation = true;
            });

            services.AddFluentValidationClientsideAdapters();

            return services;
        }
    }
}