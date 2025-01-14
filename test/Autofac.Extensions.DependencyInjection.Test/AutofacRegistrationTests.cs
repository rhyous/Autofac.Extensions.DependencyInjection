﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Autofac.Extensions.DependencyInjection.Test
{
    public class AutofacRegistrationTests
    {
        [Fact]
        public void PopulateRegistersServiceProvider()
        {
            var builder = new ContainerBuilder();
            builder.Populate(Enumerable.Empty<ServiceDescriptor>());
            var container = builder.Build();

            container.AssertRegistered<IServiceProvider>();
        }

        [Fact]
        public void CorrectServiceProviderIsRegistered()
        {
            var builder = new ContainerBuilder();
            builder.Populate(Enumerable.Empty<ServiceDescriptor>());
            var container = builder.Build();

            container.AssertImplementation<IServiceProvider, AutofacServiceProvider>();
        }

        [Fact]
        public void ServiceProviderInstancesAreNotTracked()
        {
            var builder = new ContainerBuilder();
            builder.Populate(Enumerable.Empty<ServiceDescriptor>());
            var container = builder.Build();

            container.AssertOwnership<IServiceProvider>(InstanceOwnership.ExternallyOwned);
        }

        [Fact]
        public void PopulateRegistersServiceScopeFactory()
        {
            var builder = new ContainerBuilder();
            builder.Populate(Enumerable.Empty<ServiceDescriptor>());
            var container = builder.Build();

            container.AssertRegistered<IServiceScopeFactory>();
        }

        [Fact]
        public void ServiceScopeFactoryIsRegistered()
        {
            var builder = new ContainerBuilder();
            builder.Populate(Enumerable.Empty<ServiceDescriptor>());
            var container = builder.Build();

            container.AssertImplementation<IServiceScopeFactory, AutofacServiceScopeFactory>();
        }

        [Fact]
        public void CanRegisterTransientService()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IService), typeof(Service), ServiceLifetime.Transient);
            builder.Populate(new ServiceDescriptor[] { descriptor });
            var container = builder.Build();

            container.AssertLifetime<IService, CurrentScopeLifetime>();
            container.AssertSharing<IService>(InstanceSharing.None);
            container.AssertOwnership<IService>(InstanceOwnership.OwnedByLifetimeScope);
        }

        [Fact]
        public void CanRegisterSingletonService()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IService), typeof(Service), ServiceLifetime.Singleton);
            builder.Populate(new ServiceDescriptor[] { descriptor });
            var container = builder.Build();

            container.AssertLifetime<IService, RootScopeLifetime>();
            container.AssertSharing<IService>(InstanceSharing.Shared);
            container.AssertOwnership<IService>(InstanceOwnership.OwnedByLifetimeScope);
        }

        [Fact]
        public void CanRebaseSingletonServiceToNamedLifetimeScope()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IService), typeof(Service), ServiceLifetime.Singleton);
            builder.Populate(new ServiceDescriptor[] { descriptor }, "MY_SCOPE");
            var container = builder.Build();

            container.AssertLifetime<IService, MatchingScopeLifetime>();
            container.AssertSharing<IService>(InstanceSharing.Shared);
            container.AssertOwnership<IService>(InstanceOwnership.OwnedByLifetimeScope);
        }

        [Fact]
        public void CanRegisterScopedService()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IService), typeof(Service), ServiceLifetime.Scoped);
            builder.Populate(new ServiceDescriptor[] { descriptor });
            var container = builder.Build();

            container.AssertLifetime<IService, CurrentScopeLifetime>();
            container.AssertSharing<IService>(InstanceSharing.Shared);
            container.AssertOwnership<IService>(InstanceOwnership.OwnedByLifetimeScope);
        }

        [Fact]
        public void CanRegisterGenericService()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IList<>), typeof(List<>), ServiceLifetime.Scoped);
            builder.Populate(new ServiceDescriptor[] { descriptor });
            var container = builder.Build();

            container.AssertRegistered<IList<IService>>();
        }

        [Fact]
        public void CanRegisterFactoryService()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IService), sp => new Service(), ServiceLifetime.Transient);
            builder.Populate(new ServiceDescriptor[] { descriptor });
            var container = builder.Build();

            container.AssertRegistered<Func<IServiceProvider, IService>>();
        }

        [Fact]
        public void CanResolveOptionsFromChildScopeProvider()
        {
            // Issue #32: Registering options in a child scope fails to resolve IOptions<T>.
            var container = new ContainerBuilder().Build();
            var scope = container.BeginLifetimeScope(b =>
            {
                var services = new ServiceCollection();
                services
                    .AddOptions()
                    .Configure<TestOptions>(opt =>
                    {
                        opt.Value = 6;
                    });
                b.Populate(services);
            });

            using var provider = new AutofacServiceProvider(scope);
            var options = provider.GetRequiredService<IOptions<TestOptions>>();
            Assert.Equal(6, options.Value.Value);
        }

        [Fact]
        public void CanGenerateFactoryService()
        {
            var builder = new ContainerBuilder();
            var descriptor = new ServiceDescriptor(typeof(IService), typeof(Service), ServiceLifetime.Transient);
            builder.Populate(new ServiceDescriptor[] { descriptor });
            var container = builder.Build();

            container.AssertRegistered<Func<IService>>();
        }

        [Fact]
        public void ServiceCollectionConfigurationIsRetainedInRootContainer()
        {
            var collection = new ServiceCollection();
            collection.AddOptions();
            collection.Configure<TestOptions>(options =>
            {
                options.Value = 5;
            });

            var builder = new ContainerBuilder();
            builder.Populate(collection);
            var container = builder.Build();

            var resolved = container.Resolve<IOptions<TestOptions>>();
            Assert.NotNull(resolved.Value);
            Assert.Equal(5, resolved.Value.Value);
        }

        [Fact]
        public void RegistrationsAddedAfterPopulateComeLastWhenResolvedWithIEnumerable()
        {
            const string s1 = "s1";
            const string s2 = "s2";
            const string s3 = "s3";
            const string s4 = "s4";

            var collection = new ServiceCollection();
            collection.AddTransient(provider => s1);
            collection.AddTransient(provider => s2);
            var builder = new ContainerBuilder();
            builder.Populate(collection);
            builder.Register(c => s3);
            builder.Register(c => s4);
            var container = builder.Build();

            var resolved = container.Resolve<IEnumerable<string>>().ToArray();

            Assert.Equal(resolved, new[] { s1, s2, s3, s4 });
        }

        [Fact]
        public void RegistrationsAddedBeforePopulateComeFirstWhenResolvedWithIEnumerable()
        {
            const string s1 = "s1";
            const string s2 = "s2";
            const string s3 = "s3";
            const string s4 = "s4";

            var builder = new ContainerBuilder();
            builder.Register(c => s1);
            builder.Register(c => s2);
            var collection = new ServiceCollection();
            collection.AddTransient(provider => s3);
            collection.AddTransient(provider => s4);
            builder.Populate(collection);
            var container = builder.Build();

            var resolved = container.Resolve<IEnumerable<string>>().ToArray();

            Assert.Equal(resolved, new[] { s1, s2, s3, s4 });
        }

        private class Service : IService
        {
        }

        private interface IService
        {
        }

        [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via dependency injection.")]
        private class TestOptions
        {
            public int Value { get; set; }
        }
    }
}
