namespace EBikeManager.Application.Enums;

internal enum EsphomeMessageType
{
    HelloRequest = 1,
    HelloResponse = 2,
    AuthenticationRequest = 3,
    DisconnectRequest = 5,
    DisconnectResponse = 6,
    PingRequest = 7,
    PingResponse = 8,
    DeviceInfoRequest = 9,
    DeviceInfoResponse = 10,
    ListEntitiesRequest = 11,
    ListEntitiesBinarySensorResponse = 12,
    ListEntitiesSensorResponse = 16,
    ListEntitiesDoneResponse = 19,
    SubscribeStatesRequest = 20,
    BinarySensorStateResponse = 21,
    SensorStateResponse = 25,
    GetTimeRequest = 36,
    GetTimeResponse = 37
}
