namespace CodeBase
{
    public enum MannequinState
    {
        Disconnected, //if not connected
        Idle, //connected, but NOT activated for get bytes - "stop" state
        Sleep, //connect, but reset input data.
        Run //if connected and activated for get bytes
    }
}