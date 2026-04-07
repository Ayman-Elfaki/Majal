using System;

namespace Majal;

/// <summary>
/// Marks a class as auditable, enabling tracking of creation and update timestamps.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditableAttribute : Attribute
{
}