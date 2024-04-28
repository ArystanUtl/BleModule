namespace CodeBase.ReportModules.Models
{
    public class CycleResultModel
    {
        public CycleParameterModel ParameterModel { get; private set; }
        public CycleActionCountModel ActionCountModel { get; private set; }

        public CycleResultModel(CycleActionCountModel model)
        {
            ParameterModel = null;
            ActionCountModel = model;
        }

        public CycleResultModel(CycleParameterModel model)
        {
            ParameterModel = model;
            ActionCountModel = null;
        }
    }
}