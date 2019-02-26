﻿using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IMPL
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=IMPL;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            services.AddDbContext<Database>(dbContextOptionsBuilder =>
            {
                dbContextOptionsBuilder.UseSqlServer(connectionString);
            });
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IMovieQuery, MovieQuery>();
            services.AddCap(capOptions =>
            {
                capOptions.UseEntityFramework<Database>();
                capOptions.UseRabbitMQ("localhost");// http://localhost:15672
                capOptions.UseDashboard();// http://localhost:5000/cap
            });
            services.AddMediatR();
            services.AddMvc(mvcOptions =>
            {
                mvcOptions.Filters.Add<ActionFilter>();
                mvcOptions.Filters.Add<ExceptionFilter>();
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseMvc();
        }
    }
}

// Install-Package MediatR
// Install-Package MediatR.Extensions.Microsoft.DependencyInjection
// Install-Package System.Linq.Dynamic.Core
// Install-Package DotNetCore.CAP
// Install-Package DotNetCore.CAP.RabbitMQ
// Install-Package DotNetCore.CAP.SqlServer