using System.Collections.Generic;
using System.Linq;
using Modules.Books;
using Modules.Common;
using Org.BouncyCastle.Crypto.Generators;

namespace CodeBase
{
    public class ScanFilterManager
    {
        private const string SCAN_TIME_KEY = "ScanTime";
        private const string SCAN_FILTER_KEY = "FilterName";
        private const int DEFAULT_SCAN_TIME_SECONDS = 5;
        
        
        public int ScanTime { get; private set; }
        
        private string _filterName;
        
        public ScanFilterManager(BookDatabase bookDatabase)
        {
            var braydenBook = bookDatabase.BraydenBook;
            _filterName = braydenBook.BleScanningParametersByID[SCAN_FILTER_KEY].ParameterValue;
            SetFilterByName(_filterName);

            var scanTimeParameter = braydenBook.BleScanningParametersByID[SCAN_TIME_KEY].ParameterValue;

            ScanTime = int.TryParse(scanTimeParameter, out var scanTime)
                ? scanTime
                : DEFAULT_SCAN_TIME_SECONDS;
        }
        
        private void SetFilterByName(string name)
        {
            _filterName = name?.ToLower();
        }

        public List<ScanDevice> FilterDevicesByName(IReadOnlyCollection<ScanDevice> devices)
        {
            if (_filterName.IsNullOrEmpty())
                return devices.ToList();

            var results = new List<ScanDevice>();

            foreach (var device in devices)
            {
                var deviceName = device.Name;
                
                if (deviceName.IsNullOrEmpty())
                    continue;

                if (deviceName.ToLower().Contains(_filterName))
                    results.Add(device);
            }

            return results;
        }
    }
}