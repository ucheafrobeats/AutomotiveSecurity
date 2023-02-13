namespace AutomotiveWorld.Models
{
    public class PsiSpec
    {
        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }

        public PsiSpec(int minValue, int maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}
