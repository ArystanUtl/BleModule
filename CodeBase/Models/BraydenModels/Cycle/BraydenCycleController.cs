using System.Collections.Generic;
using System.Linq;
using CodeBase.Decompression;
using CodeBase.Models.BraydenModels.Breath.Exhalation;
using CodeBase.Settings;

namespace CodeBase
{
    public class BraydenCycleController
    {
        public readonly List<BraydenCycle> Cycles;

        public BraydenCycle CurrentCycle { get; private set; }
        private readonly BraydenGlobalSettingsManager _settings;

        // ReSharper disable once ConvertConstructorToMemberInitializers
        public BraydenCycleController(BraydenGlobalSettingsManager settingsManager)
        {
            Cycles = new List<BraydenCycle>();
            _settings = settingsManager;
            CurrentCycle = new BraydenCycle(1, _settings);
        }
  
        public int RegisterCompression(CompressionModel model)
        {
            var isNewCycle = false;
            if (CurrentCycle.IsNeedFinishCycle)
            {  
                ActivateNextCycle();
                isNewCycle = true;
            }
            
            CurrentCycle.AddCompression(model);

            if (!isNewCycle)
            {
                return 0;
            }
            
            var cycleStatus = _settings.CycleManager.GetTransitionStatus(Cycles.Last(), CurrentCycle);
            CurrentCycle.UpdateTransition(cycleStatus);
           
            return Cycles.Count;
        }

        public void RegisterDecompression(DecompressionModel model)
        {
            CurrentCycle.AddDecompression(model);
        }

        public void RegisterInhalation(InhalationModel model)
        {
            CurrentCycle.AddInhalation(model);
        }
        
        public void RegisterExhalation(ExhalationModel model)
        {
            CurrentCycle.AddExhalation(model);
        }

        private void ActivateNextCycle()
        {
            Cycles.Add(CurrentCycle);

            var nextNumber = Cycles.Count + 1;
            CurrentCycle = new BraydenCycle(nextNumber, _settings);
        }
        
    }
}