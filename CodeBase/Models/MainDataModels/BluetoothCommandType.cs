namespace CodeBase
{
    public enum BluetoothCommandType
    {
        Init,
        Deinit,
        Error,
        Scanning,
        DeviceDiscovered,
        Connection,
        Disconnection,
        WriteCommand,
        SubscribeCommand,
        ServiceDiscovered,
        CharacteristicDiscovered,
        DeviceSelected,
        
        Advertising,
        Unsubscribe,
        SubscribeNotification
    }
}