using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Interfaces;
using TriPla.Backend.Infrastructure.Identity;
using TriPla.Backend.Infrastructure.Persistence;
using TriPla.Backend.Infrastructure.Repositories;

namespace TriPla.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");

        services.AddSingleton<INpgsqlConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<DataSeeder>();

        services.AddSingleton<IMongoClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                throw new InvalidOperationException("MongoDb:ConnectionString is missing.");
            return new MongoClient(opts.ConnectionString);
        });
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return sp.GetRequiredService<IMongoClient>().GetDatabase(opts.Database);
        });

        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAttractionRepository, AttractionRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<ITripChangeLogRepository, MongoTripChangeLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        return services;
    }
}
