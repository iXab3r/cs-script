using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace CSScriptLib
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetadataReferencesCache
    {
        /// <summary>
        /// Gets metadata by Assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        MetadataReference Get(Assembly assembly);

        /// <summary>
        /// Gets metadata for all assemblies in current appdomain
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<MetadataReference> GetDomainReferences();
    }
}