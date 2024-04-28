namespace CodeBase.Models.BraydenModels.Breath.Exhalation
{
    public class ExhalationModel : BleResultModel
    {
        public ExhalationVolume ExhalationVolume { get; private set; }
        
        public ExhalationModel(byte number) : base(number)
        {
        }

        public void UpdateVolume(ExhalationVolume exhalationVolume)
        {
            ExhalationVolume = exhalationVolume;
        }

        public override string ToString()
        {
            return $"#{Number}.\n" +
                   $"[Params]\n" +
                   $"{ExhalationVolume}";
        }
    }
}