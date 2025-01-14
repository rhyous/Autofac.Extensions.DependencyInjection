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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Autofac.Extensions.DependencyInjection
{
    /// <summary>
    /// Autofac implementation of the ASP.NET Core <see cref="IServiceScope"/>.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.DependencyInjection.IServiceScope" />
    internal class AutofacServiceScope : IServiceScope, IAsyncDisposable
    {
        private bool _disposed;
        private readonly AutofacServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacServiceScope"/> class.
        /// </summary>
        /// <param name="lifetimeScope">
        /// The lifetime scope from which services should be resolved for this service scope.
        /// </param>
        public AutofacServiceScope(ILifetimeScope lifetimeScope)
        {
            _serviceProvider = new AutofacServiceProvider(lifetimeScope);
        }

        /// <summary>
        /// Gets an <see cref="IServiceProvider" /> corresponding to this service scope.
        /// </summary>
        /// <value>
        /// An <see cref="IServiceProvider" /> that can be used to resolve dependencies from the scope.
        /// </value>
        public IServiceProvider ServiceProvider
        {
            get
            {
                return _serviceProvider;
            }
        }

        /// <summary>
        /// Disposes of the lifetime scope and resolved disposable services.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    _serviceProvider.Dispose();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await _serviceProvider.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
