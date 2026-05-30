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
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMongoDb(IConfiguration cfg)
        {
            var cs = cfg["MongoDB__ConnectionString"]
                     ?? "mongodb://root:pass168@mongodb.ecp-prod.svc.cluster.local:27017";
            var db = cfg["MongoDB__Database"] ?? "products";

            // ✅ Explicit settings — short server selection timeout prevents hanging
            var settings = MongoClientSettings.FromConnectionString(cs);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout         = TimeSpan.FromSeconds(5);
            settings.SocketTimeout          = TimeSpan.FromSeconds(10);

            services.AddSingleton<IMongoClient>(_ => new MongoClient(settings));
            services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(db));
            services.AddScoped<IProductRepository, MongoProductRepository>();

            return services;
        }

        // public IServiceCollection AddRedisCache(IConfiguration cfg)
        // {
        //     var cs = cfg["Redis__ConnectionString"] ?? "redis.ecp-dev.svc.cluster.local:6379";
        //
        //     services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cs));
        //     services.AddSingleton<ICacheService, RedisCacheService>();
        //
        //     return services;
        // }
        
        public IServiceCollection AddRedisCache(IConfiguration cfg)
        {
            var cs = cfg["Redis__ConnectionString"]
                     ?? "redis.ecp-prod.svc.cluster.local:6379,abortConnect=false";

            // ✅ Explicit options — AbortOnConnectFail=false prevents startup crash
            var options = ConfigurationOptions.Parse(cs);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout     = 5000;
            options.SyncTimeout        = 5000;
            options.ConnectRetry       = 5;

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(options)
            );
            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }

        public IServiceCollection AddApplicationLayer()
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<CreateProductValidator>();
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

            return services;
        }

        public IServiceCollection AddGraphQlServices(IHostEnvironment env)
        {
            services
                .AddGraphQLServer()
        
                // ── Root operation types (required by HC schema builder) ──────────
                .AddQueryType<RootQueryType>()
                .AddMutationType<RootMutationType>()

                // ── Type extensions — merged into the root types above ────────────
                .AddTypeExtension<ProductQueries>()
                .AddTypeExtension<ProductMutations>()

                // ── Output types ──────────────────────────────────────────────────
                .AddType<ProductType>()
                .AddType<ProductSummaryType>()
                .AddType<PagedProductSummaryResultType>()
                .AddType<ProductSpecType>()

                // ── DataLoader ────────────────────────────────────────────────────
                .AddDataLoader<ProductByIdDataLoader>()

                // ── Error handling ────────────────────────────────────────────────
                .AddErrorFilter<ProductErrorFilter>()

                // ── Subscriptions ─────────────────────────────────────────────────
                .AddInMemorySubscriptions()

                .ModifyRequestOptions(opt =>
                {
                    opt.IncludeExceptionDetails = env.IsDevelopment();
                });

            return services;
        }

        public void AddServiceHealthChecks(IConfiguration cfg)
        {
            // ✅ Reuse existing singletons — no new connections created
            services
                .AddHealthChecks()
                .AddMongoDb(
                    sp => sp.GetRequiredService<IMongoClient>(),
                    name: "mongodb",
                    tags: ["db"]
                )
                .AddRedis(
                    sp => sp.GetRequiredService<IConnectionMultiplexer>(),
                    name: "redis",
                    tags: ["cache"]
                );
        }
    }
}
