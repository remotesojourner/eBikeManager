using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

internal sealed record OutgoingMessage(MessageKind Kind, string Text);
