// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;

using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Xamarin.Interactive.NuGet
{
    public sealed class InteractivePackageDescription
    {
        public string PackageId { get; }
        public string Version { get; }
        public InteractivePackageSource Source { get; }

        [JsonConstructor]
        public InteractivePackageDescription (
            string packageId,
            string version = null,
            InteractivePackageSource source = null)
        {
            PackageId = packageId
                ?? throw new ArgumentNullException (nameof (packageId));

            Version = version;
            Source = source;
        }

        public SourceRepository GetSourceRepository ()
            => null;

        internal static InteractivePackageDescription FromInteractivePackage (InteractivePackage package)
            => new InteractivePackageDescription (
                package.Identity.Id,
                package.Identity.HasVersion
                    ? package.Identity.Version.ToString ()
                    : null);

        internal InteractivePackage ToInteractivePackage ()
            => new InteractivePackage (ToPackageIdentity ());

        internal PackageIdentity ToPackageIdentity ()
            => new PackageIdentity (
                PackageId,
                Version == null
                ? null
                : NuGetVersion.Parse (Version));

        internal static InteractivePackageDescription FromPackageViewModel (PackageViewModel package)
            => new InteractivePackageDescription (
                package.Package.Id,
                package.Package.HasVersion
                    ? package.Package.Version.ToString ()
                    : null,
                InteractivePackageSource.FromPackageSource (package.SourceRepository?.PackageSource));

        internal PackageViewModel ToPackageViewModel ()
            => new PackageViewModel (ToPackageIdentity ());
    }
}