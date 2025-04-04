using SyncoraBackend.Enums;

namespace SyncoraBackend.Models.DTOs.Users;

public record UserDTO(int Id, string Email, string UserName, string Role, DateTime CreationDate, string? ProfilePictureURL);