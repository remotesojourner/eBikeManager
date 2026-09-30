namespace EBikeManager.TestSupport;

internal static class BoschSamples
{
    public const string PictureUrl = "https://cdn.example.test/bikes/tenways.png";

    public const string PhotoFileId = "a4cd7f5d-8b90-4065-960d-8a226ec271a3";

    public const string InvoiceFileId = "157ab102-78d9-4246-84be-295600fe961e";

    public static byte[] Pdf { get; } = "%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF\n"u8.ToArray();

    public static byte[] Picture { get; } = ReadPicture();

    public static string Profile(string bikeId, string brand = "TENWAYS", string driveUnit = "Performance Line") => $$"""
        {
          "id": "{{bikeId}}",
          "brandName": "{{brand}}",
          "mediaAssets": { "bike_picture_url": "{{PictureUrl}}" },
          "driveUnit": {
            "productName": "{{driveUnit}}",
            "softwareVersion": "20.27.0",
            "serialNumber": "63908-0000-00-000-00-0001",
            "partNumber": "0275007000",
            "totalDistanceTraveled": 36445.0,
            "maxAssistanceSpeed": 27.4,
            "rearWheelCircumference": { "defaultValue": 2299.0, "isUserAdjusted": false, "userValue": 2299.0 },
            "powerOnTime": { "total": 9, "withMotorSupport": 8 },
            "lock": { "isEnabled": true, "isLocked": null },
            "walkAssist": { "isEnabled": true },
            "driveUnitAssistModes": [
              { "id": "0", "reachableRange": 0.0, "statistics": { "consumedEnergy": 0.0, "distance": 0.0 } },
              { "id": "A100M40040", "reachableRange": 33.0, "statistics": { "consumedEnergy": 0.0, "distance": 0.0 } },
              { "id": "A100E3AUTO", "reachableRange": 25.0, "statistics": { "consumedEnergy": 116.0, "distance": 14010.0 } },
              { "id": "A100M40020", "reachableRange": 16.0, "statistics": { "consumedEnergy": 142.0, "distance": 18618.0 } },
              { "id": "A100M40010", "reachableRange": 12.0, "statistics": { "consumedEnergy": 31.0, "distance": 3817.0 } }
            ]
          },
          "batteries": [
            {
              "productName": "PowerTube 540",
              "softwareVersion": "20.2.0",
              "serialNumber": "64024-0000-00-000-00-0002",
              "batteryLevel": null,
              "totalEnergy": 535.6,
              "deliveredWhOverLifetime": 305,
              "numberOfFullChargeCycles": { "total": 0.9, "onBike": 0.9, "offBike": 0.0 }
            }
          ],
          "headUnit": { "productName": "Kiox 300", "softwareVersion": "20.11.0", "serialNumber": "58003-0000-00-000-00-0003" },
          "remoteControl": { "productName": "LED Remote", "softwareVersion": "20.9.0", "serialNumber": "63802-0000-00-000-00-0004", "serviceDue": { "date": null, "totalDistance": null } },
          "connectedModule": null,
          "antiLockBrakeSystem": null
        }
        """;

    public static string BikePass(string bikeId) => $$"""
        {
          "bikeId": "{{bikeId}}",
          "frameNumber": "WTEN123456789",
          "frameNumberPosition": "Seat stem",
          "description": "",
          "files": [
            { "bikeId": "{{bikeId}}", "fileId": "{{PhotoFileId}}", "fileType": "BIKE_IMAGE", "link": "https://bike-pass.example.test/v1/files/{{bikeId}}/{{PhotoFileId}}", "createdAt": "2026-08-13T20:31:20Z", "updatedAt": "2026-08-13T20:31:21Z" },
            { "bikeId": "{{bikeId}}", "fileId": "{{PhotoFileId}}", "fileType": "BIKE_IMAGE", "link": "https://bike-pass.example.test/v1/files/{{bikeId}}/{{PhotoFileId}}", "createdAt": "2026-08-13T20:31:20Z", "updatedAt": "2026-08-13T20:31:22Z" },
            { "bikeId": "{{bikeId}}", "fileId": "{{InvoiceFileId}}", "fileType": "BIKE_INVOICE", "link": "https://bike-pass.example.test/v1/files/{{bikeId}}/{{InvoiceFileId}}", "createdAt": "2026-08-13T21:00:42Z", "updatedAt": "2026-08-13T21:00:43Z" }
          ]
        }
        """;

    public const string StateOfCharge = """
        { "stateOfCharge": 76, "chargingActive": false, "chargerConnected": false, "remainingEnergyForRider": 402, "reachableRange": [61, 44, 30, 22], "odometer": 36500 }
        """;

    public const string Location = """
        { "latitude": 51.507351, "longitude": -0.127758, "horizontalAccuracy": 12.0, "detectedAt": "2026-09-29T18:21:00Z" }
        """;

    public static string RideSummary(string title) => $$"""
        {
          "title": "{{title}}",
          "maximumSpeed": 32.9,
          "averageCadence": 70.0,
          "maximumCadence": 96.0,
          "maximumRiderPower": 390.0,
          "elevationLoss": 118,
          "co2EmissionsGrams": 60.1,
          "co2EmissionsCarEquivalentGrams": 3320.0,
          "assistModeUsage": [
            { "assistModeConfigId": "0", "assistModeUsage": 0, "color": 4278190080, "name": "OFF" },
            { "assistModeConfigId": "A100M40040", "assistModeUsage": 120, "color": 4286103072, "name": "ECO" },
            { "assistModeConfigId": "A100M40020", "assistModeUsage": 0, "color": 4288037869, "name": "SPORT" },
            { "assistModeConfigId": "A100M40010", "assistModeUsage": 1656, "color": 4293001237, "name": "TURBO" },
            { "assistModeConfigId": "A100E3AUTO", "assistModeUsage": 0, "color": 4278232536, "name": "AUTO" }
          ]
        }
        """;

    private static byte[] ReadPicture()
    {
        using var stream = typeof(BoschSamples).Assembly.GetManifestResourceStream("EBikeManager.TestSupport.Samples.bike.png")
            ?? throw new InvalidOperationException("The sample bike picture isn't embedded.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
