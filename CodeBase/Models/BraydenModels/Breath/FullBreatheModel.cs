using CodeBase.Models.BraydenModels.Breath.Exhalation;

namespace CodeBase.Models.BraydenModels.Breath
{
    public class FullBreatheModel
    {
        public int Number { get; set; }
        public InhalationModel Inhalation { get; set; }
        public ExhalationModel Exhalation { get; set; }

        public FullBreatheModel(int number)
        {
            Number = number;
        }

        public FullBreatheModel AddInhalation(InhalationModel model)
        {
            Inhalation = model;
            return this;
        }

        public FullBreatheModel AddExhalation(ExhalationModel model)
        {
            Exhalation = model;
            return this;
        }
    }
}