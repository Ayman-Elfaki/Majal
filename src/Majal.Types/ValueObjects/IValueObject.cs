
namespace Majal;

/// <summary>
/// Defines the base interface for a value object.
/// </summary>
public interface IValueObject;


/// <summary>
/// Defines the base interface for a value object with a single value.
/// </summary>
public interface IValueObject<out TValue> 
{
    /// <summary>
    /// Gets the value of the value object.
    /// </summary>
    TValue Value { get; }
}