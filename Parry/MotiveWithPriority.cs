namespace Parry
{
    /// <summary>
    /// Couples a motive with a value indicating priority, where higher values
    /// depict a greater priority.
    /// </summary>
    public struct MotiveWithPriority
    {
        public Constants.Motives motive;
        public int priority;
    }
}
