using System;

namespace MyProject.Application.DTOs;

public record NotificationDto(
    int NotificationId,
    int UserId,
    string Title,
    string Message,
    string Type,
    int? RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt
);
