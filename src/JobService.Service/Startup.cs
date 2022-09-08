namespace JobService.Service
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Components;
    using JobService.Components;
    using JobService.Components.Filters;
    using JobService.Service.Middleware;
    using MassTransit;
    using MassTransit.Configuration;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;


    public class Startup
    {
        static bool? _isRunningInContainer;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static bool IsRunningInContainer =>
            _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddHttpContextAccessor();
            services.AddScoped<ScopedObjectMiddleware>();
            services.AddScoped<ScopedObject>();

            var storageAccount = CloudStorageAccount.Parse(Configuration.GetConnectionString("AzureStorage"));
            var sagaTableClient = storageAccount.CreateCloudTableClient();

            var jobsTable = sagaTableClient.GetTableReference("MyJobs");
            jobsTable.CreateIfNotExists();

            var jobTypeTable = sagaTableClient.GetTableReference("MyJobTypes");
            jobTypeTable.CreateIfNotExists();

            var jobattemptTable = sagaTableClient.GetTableReference("MyJobAttemps");
            jobattemptTable.CreateIfNotExists();

            services.AddMassTransit(x =>
            {
                x.AddDelayedMessageScheduler();

                x.AddConsumer<ConvertVideoJobConsumer>(typeof(ConvertVideoJobConsumerDefinition));
                x.AddConsumer<VideoConvertedConsumer>();

                x.AddSagaRepository<JobSaga>()
                    .AzureTableRepository(cfg => cfg.ConnectionFactory(() => jobsTable));
                x.AddSagaRepository<JobTypeSaga>()
                    .AzureTableRepository(cfg => cfg.ConnectionFactory(() => jobTypeTable));
                x.AddSagaRepository<JobAttemptSaga>()
                    .AzureTableRepository(cfg => cfg.ConnectionFactory(() => jobattemptTable));

                x.AddRequestClient<ConvertVideo>();

                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                {
                    if (IsRunningInContainer)
                        cfg.Host("rabbitmq");

                    cfg.UseDelayedMessageScheduler();

                    cfg.UseMessageScope(context);
                    cfg.UseSendFilter(typeof(ForwardHeadersFilter<>), context);
                    cfg.UsePublishFilter(typeof(ForwardHeadersFilter<>), context);
                    cfg.UseConsumeFilter(typeof(OperationContextFilter<>), context);

                    var options = new ServiceInstanceOptions()
                        .SetEndpointNameFormatter(context.GetService<IEndpointNameFormatter>() ?? KebabCaseEndpointNameFormatter.Instance);

                    cfg.ServiceInstance(options, instance =>
                    {
                        instance.ConfigureJobServiceEndpoints(js =>
                        {
                            js.SagaPartitionCount = 1;
                            js.FinalizeCompleted = true;

                            js.ConfigureSagaRepositories(context);
                        });

                        instance.ConfigureEndpoints(context);
                    });
                });
            });

            services.AddOpenApiDocument(cfg =>
            {
                cfg.OperationProcessors.Add(new AddRequiredHeaderParameter());

                cfg.PostProcess = d =>
                {
                    d.Info.Title = "Convert Video Service";
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseMiddleware<ScopedObjectMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                    ResponseWriter = HealthCheckResponseWriter
                });

                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions {ResponseWriter = HealthCheckResponseWriter});

                endpoints.MapControllers();
            });
        }

        static Task HealthCheckResponseWriter(HttpContext context, HealthReport result)
        {
            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(entry => new JProperty(entry.Key, new JObject(
                    new JProperty("status", entry.Value.Status.ToString()),
                    new JProperty("description", entry.Value.Description),
                    new JProperty("data", JObject.FromObject(entry.Value.Data))))))));

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}