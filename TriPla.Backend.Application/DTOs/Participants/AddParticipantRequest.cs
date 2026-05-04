using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Application.DTOs.Participants;

public record AddParticipantRequest(
    string Email,
    ParticipantRole Role = ParticipantRole.Member);

public record ChangeRoleRequest(ParticipantRole Role);
