﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Onova.Exceptions;
using Onova.Internal;

namespace Onova.Services
{
    /// <summary>
    /// Resolves packages using multiple other package resolvers.
    /// </summary>
    public class AggregatePackageResolver : IPackageResolver
    {
        private readonly IReadOnlyList<IPackageResolver> _resolvers;

        /// <summary>
        /// Initializes an instance of <see cref="AggregatePackageResolver"/>.
        /// </summary>
        public AggregatePackageResolver(IReadOnlyList<IPackageResolver> resolvers)
        {
            _resolvers = resolvers.GuardNotNull(nameof(resolvers));
        }

        /// <summary>
        /// Initializes an instance of <see cref="AggregatePackageResolver"/>.
        /// </summary>
        public AggregatePackageResolver(params IPackageResolver[] resolvers)
            : this((IReadOnlyList<IPackageResolver>) resolvers)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Version>> GetPackageVersionsAsync()
        {
            var aggregateVersions = new HashSet<Version>();

            // Get unique package versions provided by all resolvers
            foreach (var resolver in _resolvers)
            {
                var versions = await resolver.GetPackageVersionsAsync();
                aggregateVersions.AddRange(versions);
            }

            return aggregateVersions.ToArray();
        }

        private async Task<IPackageResolver> GetResolverForPackageAsync(Version version)
        {
            // Try to find the first resolver that has this package version
            foreach (var resolver in _resolvers)
            {
                var versions = await resolver.GetPackageVersionsAsync();
                if (versions.Contains(version))
                    return resolver;
            }

            // Return null if none of the resolvers provide this package version
            return null;
        }

        /// <inheritdoc />
        public async Task DownloadPackageAsync(Version version, string destFilePath,
            IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            version.GuardNotNull(nameof(version));
            destFilePath.GuardNotNull(nameof(destFilePath));

            // Find a resolver that has this package version
            var resolver = await GetResolverForPackageAsync(version);
            if (resolver == null)
                throw new PackageNotFoundException(version);

            // Download package
            await resolver.DownloadPackageAsync(version, destFilePath, progress, cancellationToken);
        }
    }
}