using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Owin.Hosting;
using Nancy.Bootstrappers.Autofac;
using Newtonsoft.Json;
using Owin;

namespace Nancy.AutofacExample
{
    public static class Program
    {
        public static void Main()
        {
            using (WebApp.Start("http://localhost:8080/", Configure))
            {
                Console.WriteLine("Server started.");
                Console.ReadLine();
            }
        }

        private static void Configure(IAppBuilder app)
        {
            var builder = new ContainerBuilder();

            // Uncomment to switch IApplicationService implementation.
            //builder.RegisterType<ApplicationServiceOne>()
            //    .As<IApplicationService>()
            //    .SingleInstance();

            builder.RegisterType<ApplicationServiceTwo>()
                .As<IApplicationService>()
                .SingleInstance();

            // Uncomment to switch IRequestUtility implementation.
            //builder.RegisterType<RequestUtilityOne>()
            //    .As<IRequestUtility>()
            //    .InstancePerRequest();

            builder.RegisterType<RequestUtilityTwo>()
                .As<IRequestUtility>()
                .InstancePerRequest();

            builder.RegisterType<RequestService>()
                .As<IRequestService>()
                .InstancePerRequest();

            var container = builder.Build();

            var bootstrapper = new Bootstrapper(container);

            app.Use(async (context, next) =>
            {
                Console.WriteLine();
                Console.WriteLine("Request started.");

                await next();

                Console.WriteLine("Request ended.");
            });

            app.UseNancy(options => options.Bootstrapper = bootstrapper);
        }
    }

    #region Nancy Stuff

    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private readonly ILifetimeScope _lifetimeScope;

        public Bootstrapper(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        protected override ILifetimeScope GetApplicationContainer()
        {
            return _lifetimeScope;
        }
    }

    public class HomeModule : NancyModule
    {
        public HomeModule(IRequestService requestService)
        {
            Get["/"] = args =>
            {
                var serviceType = requestService.GetType();

                var properties = serviceType.GetRuntimeProperties();

                var propertyTypes = properties.Select(x => x.GetValue(requestService).GetType().Name).ToArray();

                return JsonConvert.SerializeObject(propertyTypes);
            };
        }
    }

    #endregion

    #region Request Utilities

    public interface IRequestUtility : IDisposable
    {
    }

    public class RequestUtilityOne : IRequestUtility
    {
        public RequestUtilityOne()
        {
            Console.WriteLine("RequestUtilityOne created.");
        }

        public void Dispose()
        {
            Console.WriteLine("RequestUtilityOne disposed.");
        }
    }

    public class RequestUtilityTwo : IRequestUtility
    {
        public RequestUtilityTwo()
        {
            Console.WriteLine("RequestUtilityTwo created.");
        }

        public void Dispose()
        {
            Console.WriteLine("RequestUtilityTwo disposed.");
        }
    }

    #endregion

    #region Request Services

    public interface IRequestService : IDisposable
    {
        IApplicationService ApplicationService { get; }
    }

    public class RequestService : IRequestService
    {
        public RequestService(IApplicationService applicationService, IRequestUtility utility)
        {
            ApplicationService = applicationService;
            Utility = utility;

            Console.WriteLine("RequestService created.");
        }

        public IApplicationService ApplicationService { get; private set; }

        public IRequestUtility Utility { get; private set; }

        public void Dispose()
        {
            Console.WriteLine("RequestService disposed.");
        }
    }

    #endregion

    #region Application Services

    public interface IApplicationService
    {
    }

    public class ApplicationServiceOne : IApplicationService
    {
        public ApplicationServiceOne()
        {
            Console.WriteLine("ApplicationServiceOne created.");
        }
    }

    public class ApplicationServiceTwo : IApplicationService
    {
        public ApplicationServiceTwo()
        {
            Console.WriteLine("ApplicationServiceTwo created.");
        }
    }

    #endregion
}
