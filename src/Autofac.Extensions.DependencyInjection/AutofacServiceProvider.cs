﻿// This software is part of the Autofac IoC container
// Copyright © 2015 Autofac Contributors
// https://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Microsoft.Extensions.DependencyInjection;

namespace Autofac.Extensions.DependencyInjection
{
    /// <summary>
    /// Autofac implementation of the ASP.NET Core <see cref="IServiceProvider"/>.
    /// </summary>
    /// <seealso cref="System.IServiceProvider" />
    /// <seealso cref="Microsoft.Extensions.DependencyInjection.ISupportRequiredService" />
    public partial class AutofacServiceProvider : IServiceProvider, ISupportRequiredService, IDisposable, IAsyncDisposable
    {
        private readonly ILifetimeScope _lifetimeScope;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacServiceProvider"/> class.
        /// </summary>
        /// <param name="lifetimeScope">
        /// The lifetime scope from which services will be resolved.
        /// </param>
        public AutofacServiceProvider(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// Gets service of type <paramref name="serviceType" /> from the
        /// <see cref="AutofacServiceProvider" /> and requires it be present.
        /// </summary>
        /// <param name="serviceType">
        /// An object that specifies the type of service object to get.
        /// </param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.
        /// </returns>
        /// <exception cref="Autofac.Core.Registration.ComponentNotRegisteredException">
        /// Thrown if the <paramref name="serviceType" /> isn't registered with the container.
        /// </exception>
        /// <exception cref="Autofac.Core.DependencyResolutionException">
        /// Thrown if the object can't be resolved from the container.
        /// </exception>
        public object GetRequiredService(Type serviceType)
        {
            var scope = GetPerRequestScope();
            return scope.Resolve(serviceType);
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">
        /// An object that specifies the type of service object to get.
        /// </param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />; or <see langword="null" />
        /// if there is no service object of type <paramref name="serviceType" />.
        /// </returns>
        public object GetService(Type serviceType)
        {
            var scope = GetPerRequestScope();
            return scope.ResolveOptional(serviceType);
        }

        /// <summary>
        /// Gets the underlying instance of <see cref="ILifetimeScope" />.
        /// </summary>
        public ILifetimeScope LifetimeScope => _lifetimeScope;

        private ILifetimeScope GetPerRequestScope()
        {
            var perRequestScope = _lifetimeScope.BeginLifetimeScope(builder =>
            {
                var perRequestModuleAccessor = _lifetimeScope.ResolveOptional<IPerRequestModuleAccessor>();
                if (perRequestModuleAccessor.Modules != null && perRequestModuleAccessor.Modules.Any())
                {
                    foreach (var module in perRequestModuleAccessor.Modules)
                    {
                        builder.RegisterModule(module);
                    }
                }
            });
            return perRequestScope;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources;
        /// <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _lifetimeScope.Dispose();
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs a dispose operation asynchronously.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA1816:Dispose methods should call SuppressFinalize",
            Justification = "DisposeAsync should also call SuppressFinalize (see various .NET internal implementations).")]
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await _lifetimeScope.DisposeAsync().ConfigureAwait(false);
                GC.SuppressFinalize(this);
            }
        }
    }
}
