using Microsoft.Extensions.Configuration;
using System;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using StackExchange.Redis;

namespace RedisConsoleApp
{
    class Program
    {
        public static IConfigurationRoot Configuration;

        static int Main(string[] args)
        {
            // Initialize serilog logger
            Log.Logger = new LoggerConfiguration().CreateLogger();

            try
            {
                MainAsync(args).Wait();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        static async Task MainAsync(string[] args)
        {
            // Create service collection
            Log.Information("Creating service collection");
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Log.Information("Building service provider");
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                Log.Information("Starting service");
                await serviceProvider.GetService<RedisService>().Run();
                Log.Information("Ending service");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running service");
                throw ex;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            services.AddSingleton<IConfigurationRoot>(Configuration);
            services.AddTransient<RedisService>();

            var redisConfig = Configuration.GetConnectionString("Redis");
            var redisDefaultDb = Configuration.GetConnectionString("RedisDefaultDb");

            Console.WriteLine($"Redis config {redisConfig}, Redis default DB {redisDefaultDb}");

            var isValidRedisDb = int.TryParse(redisDefaultDb, out var redisDb);
            if (isValidRedisDb == false)
            {
                Console.WriteLine("Cannot parse Redis Default DB value");
                throw new InvalidCastException("Cannot parse Redis Default DB value");
            }

            var redis = ConnectionMultiplexer.Connect($"{redisConfig},defaultDatabase={redisDb}");
            services.AddSingleton<IConnectionMultiplexer>(redis);
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = $"{redisConfig},defaultDatabase={redisDb}";
                options.InstanceName = Configuration.GetConnectionString("RedisInstance");
            });
        }
    }
}
