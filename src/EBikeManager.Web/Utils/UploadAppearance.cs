using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using MudBlazor;

namespace EBikeManager.Web.Utils;

public static class UploadAppearance
{
    public static Color ColorOf(RideExportDto export) =>
        export.Status != RideExportStatus.Uploaded ? Color.Error
        : export.Note != null ? Color.Warning
        : Color.Success;
}
