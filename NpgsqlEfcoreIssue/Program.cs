using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace NpgsqlEfcoreIssue
{
    internal class ApplicationDbContext : DbContext
    {
        public DbSet<TestModel> TestModels { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }

    internal class TestModel
    {
        public int TestModelId { get; set; }

        public Instant Created { get; set; }
    }

    class Program
    {
        private readonly ApplicationDbContext _db;
        private readonly IClock _clock;

        static Task<int> Main(string[] args)
                    => CreateHostBuilder(args).RunCommandLineApplicationAsync<Program>(args);

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureServices((hostContext, services) =>
                   {
                       ConfigureServices(services);
                   });

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IClock>(SystemClock.Instance);

            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseNpgsql("Host=localhost;Database=npgsqlefcoreissue;Username=npgsqlefcoreissue;Password=npgsqlefcoreissue;", o =>
                    {
                        o.UseNodaTime();
                        o.MigrationsHistoryTable("__ef_migrations_history");
                    })
                    .UseSnakeCaseNamingConvention());
        }

        public Program(
            ApplicationDbContext db,
            IClock clock)
        {
            _db = db;
            _clock = clock;
        }

        private async Task OnExecute()
        {
            await _db.Database.EnsureDeletedAsync();
            await _db.Database.EnsureCreatedAsync();

            var m = new TestModel()
            {
                Created = _clock.GetCurrentInstant()
            };
            _db.TestModels.Add(m);
            await _db.SaveChangesAsync();
            Console.ReadKey();
        }

    }
}
