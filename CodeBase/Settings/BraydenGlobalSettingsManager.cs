using Modules.Books;

namespace CodeBase.Settings
{
    public class BraydenGlobalSettingsManager
    {
        public CompressionManager CompressionManager { get; }
        public BreatheManager BreatheManager { get; }
        public BraydenUnitConvertManager UnitConvertManager { get; }
        public DecompressionManager DecompressionManager { get; }
        public BraydenConfigManager ConfigManager { get; }
        public CycleManager CycleManager { get; }
        
        public ScanFilterManager ScanManager { get; }
        public BraydenGlobalSettingsManager(BookDatabase bookDatabase)
        {
            ConfigManager = new BraydenConfigManager(bookDatabase);
            ScanManager = new ScanFilterManager(bookDatabase);
            
            UnitConvertManager = new BraydenUnitConvertManager(ConfigManager);
            
            CompressionManager = new CompressionManager(ConfigManager, UnitConvertManager);
            DecompressionManager = new DecompressionManager(ConfigManager, UnitConvertManager);
            
            BreatheManager = new BreatheManager(ConfigManager, UnitConvertManager);
            
            CycleManager = new CycleManager(ConfigManager);
        }
    }
}