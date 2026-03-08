using ECP.ProductService.Application.Behaviors;
using ECP.ProductService.Application.Validators;
using ECP.ProductService.Core.Interfaces.Cache;
using ECP.ProductService.Core.Interfaces.Repositories;
using ECP.ProductService.Infrastructure.Cache;
using ECP.ProductService.Infrastructure.GraphQL.DataLoaders;
using ECP.ProductService.Infrastructure.GraphQL.Filters;
using ECP.ProductService.Infrastructure.GraphQL.Mutations;
using ECP.ProductService.Infrastructure.GraphQL.Queries;
using ECP.ProductService.Infrastructure.GraphQL.Types;
using ECP.ProductService.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using MongoDB.Driver;
using StackExchange.Redis;

namespace ECP.ProductService.API.Extensions;

public static class ServiceCollectionExtensions
{
    // ── MongoDB ───────────────────────────────────────────────────────────────
    public static IServiceCollection AddMongoDB(
        this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg["MongoDB__ConnectionString"]
              ?? "mongodb://root:pass168@mongodb.ecp-dev.svc.cluster.local:27017";
        var db = cfg["MongoDB__Database"] ?? "product_service";

        services.AddSingleton<IMongoClient>(_ => new MongoClient(cs));
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(db));
        services.AddScoped<IProductRepository, MongoProductRepository>();

        return services;
    }

    // ── Redis ─────────────────────────────────────────────────────────────────
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg["Redis__ConnectionString"]
              ?? "redis.ecp-dev.svc.cluster.local:6379";

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(cs));
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    // ── Application layer (MediatR + FluentValidation) ────────────────────────
    public static IServiceCollection AddApplicationLayer(
        this IServiceCollection services)
    {
        // MediatR — discovers all handlers from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreateProductValidator>();
            // Pipeline order: logging → validation → handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation — auto-registers all IValidator<T> in assembly
        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

        return services;
    }

    // ── GraphQL ───────────────────────────────────────────────────────────────
    public static IServiceCollection AddGraphQL(
        this IServiceCollection services, IHostEnvironment env)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<ProductQueries>()
            .AddMutationType<ProductMutations>()
            .AddType<ProductType>()
            .AddType<ProductSummaryType>()
            .AddType<PagedProductResultType>()
            .AddDataLoader<ProductByIdDataLoader>()
            .AddErrorFilter<ProductErrorFilter>()
            .AddInMemorySubscriptions()
            .ModifyRequestOptions(opt =>
            {
                // Show exception details only in development
                opt.IncludeExceptionDetails = env.IsDevelopment();
            });

        return services;
    }

    // ── Health checks ─────────────────────────────────────────────────────────
    public static IServiceCollection AddServiceHealthChecks(
        this IServiceCollection services, IConfiguration cfg)
    {
        var mongo = cfg["MongoDB__ConnectionString"]
                 ?? "mongodb://root:pass168@mongodb.ecp-dev.svc.cluster.local:27017";
        var redis = cfg["Redis__ConnectionString"]
                 ?? "redis.ecp-dev.svc.cluster.local:6379";

        services
            .AddHealthChecks()
            .AddMongoDb(mongo, name: "mongodb", tags: ["db"])
            .AddRedis(redis,   name: "redis",   tags: ["cache"]);

        return services;
    }
}
