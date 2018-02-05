using System;

namespace RulEng.Helpers
{
    public static class DefaultHelpers
    {
        public static DateTime DefDate()
        {
            return new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
