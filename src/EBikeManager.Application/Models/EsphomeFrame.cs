using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

internal sealed record EsphomeFrame(EsphomeMessageType Type, byte[] Payload);
