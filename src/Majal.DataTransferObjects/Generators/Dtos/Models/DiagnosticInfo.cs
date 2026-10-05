using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

public readonly record struct DiagnosticInfo(
    string Id,
    string Title,
    string Message,
    DiagnosticSeverity Severity,
    Location? Location = null
);
