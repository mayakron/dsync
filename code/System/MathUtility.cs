namespace System
{
    public static class MathUtility
    {
        public static ulong Distance(ulong x, ulong y)
        {
            return y > x ? y - x : x - y;
        }
    }
}