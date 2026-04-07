using System;

namespace Majal;

/// <summary>
/// Marks a class as an aggregate and specifies the type of domain events it publishes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AggregateAttribute<TDomainEvent> : Attribute
{
}


/// <summary>
/// Marks a class as an aggregate.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AggregateAttribute : Attribute
{
}