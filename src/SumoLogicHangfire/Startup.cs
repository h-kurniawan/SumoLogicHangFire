using System;
using Hangfire;
using Hangfire.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SumoLogicHangfire.Configurations;
using SumoLogicHangfire.Services;

namespace SumoLogicHangfire
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration) => Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(config =>
            {
                config.UseSqlServerStorage("Data Source=.;Initial Catalog=SumoLogicHangfire;Integrated Security=true");
                config.UseColouredConsoleLogProvider();
            });

            services.AddMemoryCache();

            services                
                .Configure<AppSettings>(Configuration)
                .AddMvcCore()
                .AddJsonFormatters()
                .AddDataAnnotations();

            services.UseConfigurationValidation();
            services.Configure<AppSettings>(Configuration);
            services.ConfigureValidatableSetting<SumoLogicSettings>(
                Configuration.GetSection($"{nameof(AppSettings)}:{nameof(SumoLogicSettings)}"));

            services.AddTransient<ISumoLogMining, SumoLogMining>();
            services.AddTransient<IApiCallService, SumoLogicApi>();
            services.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireDashboard();
            var options = new BackgroundJobServerOptions
            {
                Queues = new[] { "http", "callback" },
                WorkerCount = 4
                //SchedulePollingInterval = TimeSpan.FromMilliseconds(250)
            };
            app.UseHangfireServer(options);

            app.UseMvc();
        }
    }
}
