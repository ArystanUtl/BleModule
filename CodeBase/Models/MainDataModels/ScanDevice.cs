public struct ScanDevice
{
    public string Name;
    public string MacAddress;

    public ScanDevice(string name, string mac)
    {
        Name = name;
        MacAddress = mac;
    }

    public override string ToString()
    {
        return $"Name: {Name} Mac: {MacAddress}";
    }
}