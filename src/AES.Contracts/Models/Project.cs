namespace AES.Contracts.Models;

public record Project(
    Guid Id,
    string Name,
    string Description,
    string RootPath,
    DateTime CreatedAt,
    DateTime UpdatedAt
);