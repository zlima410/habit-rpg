using FluentValidation;
using FluentValidation.AspNetCore;
using HabitRPG.Api.Validators;
using HabitRPG.Api.Repositories;
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

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IHabitRepository, HabitRepository>();
            services.AddScoped<ICompletionLogRepository, CompletionLogRepository>();

            return services;
        }
    }
}