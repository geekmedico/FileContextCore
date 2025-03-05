// Copyright (c) morrisjdev. All rights reserved.
// Original copyright (c) .NET Foundation. All rights reserved.
// Modified version by morrisjdev
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileContextCore.Utilities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace FileContextCore.Storage.Internal
{
    /// <summary>
    ///     <para>
    ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///         any release. You should only use it directly in your code with extreme caution and knowing that
    ///         doing so can result in application failures when updating to a new Entity Framework Core release.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Scoped"/>. This means that each
    ///         <see cref="DbContext"/> instance will use its own instance of this service.
    ///         The implementation may depend on other services registered with any lifetime.
    ///         The implementation does not need to be thread-safe.
    ///     </para>
    /// </summary>
    public class FileContextDatabase : Database, IFileContextDatabase
    {
        private readonly IFileContextStore _store;
        private readonly IUpdateAdapterFactory _updateAdapterFactory;
        private readonly IDiagnosticsLogger<DbLoggerCategory.Update> _updateLogger;

    
        public FileContextDatabase(
            [NotNull] DatabaseDependencies dependencies,
            [NotNull] IFileContextStoreCache storeCache,
            [NotNull] IDbContextOptions options,
            [NotNull] IUpdateAdapterFactory updateAdapterFactory,
            [NotNull] IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger)
            : base(dependencies)
        {
            Check.NotNull(storeCache, nameof(storeCache));
            Check.NotNull(options, nameof(options));
            Check.NotNull(updateAdapterFactory, nameof(updateAdapterFactory));
            Check.NotNull(updateLogger, nameof(updateLogger));

            _store = storeCache.GetStore(options);
            _updateAdapterFactory = updateAdapterFactory;
            _updateLogger = updateLogger;
        }

    
        public virtual IFileContextStore Store => _store;

    
        public override int SaveChanges(IList<IUpdateEntry> entries)
            => _store.ExecuteTransaction(Check.NotNull(entries, nameof(entries)), _updateLogger);

    //TODO: Aqu� es donde se mandan a guardar los datos
        public override Task<int> SaveChangesAsync(
            IList<IUpdateEntry> entries,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_store.ExecuteTransaction(Check.NotNull(entries, nameof(entries)), _updateLogger));

    
        public virtual bool EnsureDatabaseCreated()
            => _store.EnsureCreated(_updateAdapterFactory, _updateLogger);
    }
}
