using System;

using UnityEngine;

namespace Unity.Kinematica
{
    /// <summary>
    /// Attribute used to annotate traits.
    /// </summary>
    /// <remarks>
    /// Traits are user-defined characteristics that can be associated to tags or markers.
    /// Users can define own custom data by using C# structs. These structs
    /// will then show up in the Kinematica builder tool and allow tags or markers
    /// to be created carrying specific instances of the corresponding traits.
    /// A trait itself wraps the actual payload (the instance of the user-defined struct).
    /// <example>
    /// <code>
    /// [Trait]
    /// public struct Anchor
    /// {
    ///     public AffineTransform transform;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Binary.Tag"/>
    /// <seealso cref="Binary.Marker"/>
    /// <seealso cref="Query"/>
    [AttributeUsage(AttributeTargets.Struct)]
    public class TraitAttribute : Attribute
    {
        static TraitAttribute GetAttribute(Type type)
        {
            var attributes =
                type.GetCustomAttributes(
                    typeof(TraitAttribute), false);

            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes[0] as TraitAttribute;
        }
    }
}
