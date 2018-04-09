using System;

namespace RulEng.Helpers
{
    public static class GuidHelpers
    {
        /// <summary>
        /// XOrs two Guids together to create a new Guid
        /// </summary>
        /// <param name="guidA"></param>
        /// <param name="guidB"></param>
        /// <returns></returns>
        public static Guid Merge(this Guid guidA, Guid guidB)
        {
            var aba = guidA.ToByteArray();
            var bba = guidB.ToByteArray();
            var cba = new byte[aba.Length];

            for (var ix = 0; ix < cba.Length; ix++)
            {
                cba[ix] = (byte)(aba[ix] ^ bba[ix]);
            }

            return new Guid(cba);
        }
    }
}
