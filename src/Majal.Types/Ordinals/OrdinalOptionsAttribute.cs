using System;

namespace Majal;

/// <summary>
/// Sets assembly-level defaults for <c>ReorderAsync</c> batching.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class OrdinalOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the <c>idsInOrder</c> count above which <c>ReorderAsync</c> splits its work
    /// into batches. Defaults to 1000, matching <see cref="BatchSize"/>, so no single generated
    /// SQL statement ever exceeds a proven-safe expression-tree depth; raise this only if your
    /// environment is verified to tolerate deeper nested CASE expressions in one statement.
    /// </summary>
    public int BatchThreshold { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the number of ids updated per batch once <see cref="BatchThreshold"/> is
    /// exceeded. Defaults to 1000.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
