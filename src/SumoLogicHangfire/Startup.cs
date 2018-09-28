using Hangfire;
using Hangfire.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SumoLogicHangfire.Services;

namespace SumoLogicHangfire
{
    public class Startup
    {
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
                .AddMvcCore()
                .AddJsonFormatters()
                .AddDataAnnotations();

            services.AddTransient<ISumoLogic, SumoLogic>();
            services.AddTransient<IApiCallService, ApiCallService>();
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
                Queues = new[] { "http", "callback" }
            };
            app.UseHangfireServer(options);

            app.UseMvc();
        }
    }
}
