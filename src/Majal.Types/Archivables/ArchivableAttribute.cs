using System;

namespace Majal;

/// <summary>
/// Marks a class as archivable, enabling soft-delete functionality.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ArchivableAttribute : Attribute;