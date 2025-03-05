// Copyright (c) morrisjdev. All rights reserved.
// Original copyright (c) .NET Foundation. All rights reserved.
// Modified version by morrisjdev
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FileContextCore.Internal;
using FileContextCore.ValueGeneration.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;

namespace FileContextCore.Storage.Internal
{

    public class FileContextStore : IFileContextStore
    {
        private readonly IFileContextTableFactory _tableFactory;
        private readonly bool _useNameMatching;

        private readonly object _lock = new object();

        private Dictionary<object, IFileContextTable> _tables;

    
        public FileContextStore([NotNull] IFileContextTableFactory tableFactory)
            : this(tableFactory, useNameMatching: false)
        {
        }

    
        public FileContextStore(
            [NotNull] IFileContextTableFactory tableFactory,
            bool useNameMatching)
        {
            _tableFactory = tableFactory;
            _useNameMatching = useNameMatching;
        }

    
        public virtual FileContextIntegerValueGenerator<TProperty> GetIntegerValueGenerator<TProperty>(
            IProperty property)
        {
            lock (_lock)
            {
                var entityType = property.DeclaringEntityType;
                var key = _useNameMatching ? (object)entityType.Name : entityType;

                return EnsureTable(key, entityType).GetIntegerValueGenerator<TProperty>(property);
            }
        }

    
        public virtual bool EnsureCreated(
            IUpdateAdapterFactory updateAdapterFactory,
            IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger)
        {
            lock (_lock)
            {
                var valuesSeeded = _tables == null;
                if (valuesSeeded)
                {
                    // ReSharper disable once AssignmentIsFullyDiscarded
                    _tables = CreateTables();

                    var updateAdapter = updateAdapterFactory.CreateStandalone();
                    var entries = new List<IUpdateEntry>();
                    foreach (var entityType in updateAdapter.Model.GetEntityTypes())
                    {
                        foreach (var targetSeed in entityType.GetSeedData())
                        {
                            var entry = updateAdapter.CreateEntry(targetSeed, entityType);
                            entry.EntityState = EntityState.Added;
                            entries.Add(entry);
                        }
                    }

                    ExecuteTransaction(entries, updateLogger);
                }

                return valuesSeeded;
            }
        }

    
        public virtual bool Clear()
        {
            lock (_lock)
            {
                if (_tables == null)
                {
                    return false;
                }

                _tables = null;

                return true;
            }
        }

        private static Dictionary<object, IFileContextTable> CreateTables()
            => new Dictionary<object, IFileContextTable>();

    
        public virtual IReadOnlyList<FileContextTableSnapshot> GetTables(IEntityType entityType)
        {
            var data = new List<FileContextTableSnapshot>();
            lock (_lock)
            {
                foreach (var et in entityType.GetDerivedTypesInclusive().Where(et => !et.IsAbstract()))
                {
                    var key = _useNameMatching ? (object)et.Name : et;
                    var table = EnsureTable(key, et);
                    data.Add(new FileContextTableSnapshot(et, table.SnapshotRows()));
                }
            }

            return data;
        }

    
        public virtual int ExecuteTransaction(
            IList<IUpdateEntry> entries,
            IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger)
        {
            var rowsAffected = 0;

            lock (_lock)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var entityType = entry.EntityType;

                    Debug.Assert(!entityType.IsAbstract());

                    var key = _useNameMatching ? (object)entityType.Name : entityType;
                    var table = EnsureTable(key, entityType);

                    if (entry.SharedIdentityEntry != null)
                    {
                        if (entry.EntityState == EntityState.Deleted)
                        {
                            continue;
                        }

                        table.Delete(entry);
                    }

                    switch (entry.EntityState)
                    {
                        case EntityState.Added:
                            table.Create(entry);
                            break;
                        case EntityState.Deleted:
                            table.Delete(entry);
                            break;
                        case EntityState.Modified:
                            table.Update(entry);
                            break;
                    }

                    rowsAffected++;
                }

                foreach (KeyValuePair<object, IFileContextTable> table in _tables)
                {
                    table.Value.Save();
                }
            }

            updateLogger.ChangesSaved(entries, rowsAffected);

            return rowsAffected;
        }

        //TODO: CAda que hay una entrada nueva aqui pasan a verificar que la tabla esta creada

        // Must be called from inside the lock
        private IFileContextTable EnsureTable(object key, IEntityType entityType)
        {
            if (_tables == null)
            {
                _tables = CreateTables();
            }

            if (!_tables.TryGetValue(key, out var table))
            {
                _tables.Add(key, table = _tableFactory.Create(entityType));
            }

            return table;
        }
    }
}
