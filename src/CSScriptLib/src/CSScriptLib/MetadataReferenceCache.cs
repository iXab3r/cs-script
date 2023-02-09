using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace CSScriptLib
{
    /// <summary>
    /// Holds MetadataReference for all known assemblies
    /// </summary>
    public sealed class MetadataReferencesCache : MetadataReferenceResolver, IMetadataReferencesCache
    {
        private static readonly ConcurrentDictionary<Assembly, MetadataReference> MetadataReferencesByAssembly = new ConcurrentDictionary<Assembly, MetadataReference>();
        
        /// <summary>
        /// Gets metadata by Assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public MetadataReference Get(Assembly assembly)
        {
            return MetadataReferencesByAssembly.GetOrAdd(assembly, ResolveMetadata);
        }

        /// <summary>
        /// Gets metadata for all assemblies in current appdomain
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<MetadataReference> GetDomainReferences()
        {
            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            return domainAssemblies.Select(Get).ToArray();
        }
        
        /// <inheritdoc />
        public override bool ResolveMissingAssemblies => false;

        /// <inheritdoc />
        public override bool Equals(object other) => ReferenceEquals(this, other);

        /// <inheritdoc />
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        /// <inheritdoc />
        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        /// <inheritdoc />
        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            return null;
        }

        private static MetadataReference ResolveMetadata(Assembly assembly)
        {
            unsafe
            {
                if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location))
                {
                    return MetadataReference.CreateFromFile(assembly.Location);
                }
                
                if (assembly.TryGetRawMetadata(out var blob, out var blobLength))
                {
                    var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr) blob, blobLength);
                    var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    var reference = assemblyMetadata.GetReference();
                    return reference;
                }

                // should never happen in real-world scenarios
                throw new ArgumentException($"Failed to create reference for assembly: {assembly}");
            }
        }
    }
}