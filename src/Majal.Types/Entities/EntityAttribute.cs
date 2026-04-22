using System;

namespace Majal;

/// <summary>
/// Marks a class as an entity and specifies the type of its unique identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityAttribute<TId> : Attribute;


/// <summary>
/// Marks a class as an entity.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityAttribute : Attribute;