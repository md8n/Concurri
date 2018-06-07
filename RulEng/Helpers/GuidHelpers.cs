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

        /// <summary>
        /// GUID version types
        /// </summary>
        private enum GuidVersion
        {
            TimeBased = 0x01,
            Reserved = 0x02,
            NameBased = 0x03,
            Random = 0x04
        }

        /// <summary>
        /// Ensure that the supplied Guid is non empty
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static Guid NonEmptyUuid(this Guid guid, DateTime? dateTime = null)
        {
            return guid == Guid.Empty ? NewTimeUuid(dateTime) : guid;
        }

        /// <summary>
        /// offset to move from 1/1/0001, which is 0-time for .NET, to gregorian 0-time of 10/15/1582
        /// </summary>
        private static readonly DateTimeOffset GregorianCalendarStart = new DateTimeOffset(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Build a time based uuid
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static Guid NewTimeUuid(DateTime? dateTime = null)
        {
            // number of bytes in uuid
            const int byteArraySize = 16;

            if (!dateTime.HasValue)
            {
                dateTime = DateTime.UtcNow;
            }

            // indexes within the uuid array for certain boundaries
            const byte timeStampByte = 0;
            const byte versionByte = 7;
            const byte guidClockSequenceByte = 8;
            const byte variantByte = 8 - guidClockSequenceByte;
            const byte nodeByte = 10;

            // declare the whole thing
            Span<byte> guid = new byte[byteArraySize];

            // multiplex version info
            const byte versionByteMask = 0x0f;
            const byte versionByteShift = 4;

            // copy timestamp and set version info
            Span<byte> timeStampSpan = guid.Slice(timeStampByte, guidClockSequenceByte);
            var ticks = dateTime.Value.Ticks - GregorianCalendarStart.Ticks;
            Console.WriteLine(ticks);
            BitConverter.GetBytes(ticks).CopyTo(timeStampSpan);
            if (BitConverter.IsLittleEndian)
            {
                timeStampSpan.Reverse();
            }
            // set the version
            timeStampSpan[versionByte] &= versionByteMask;
            timeStampSpan[versionByte] |= (byte)GuidVersion.TimeBased << versionByteShift;

            // multiplex variant info
            const byte variantByteMask = 0x3f;
            const byte variantByteOr = 0x80;

            // copy clock sequence and set the variant
            Span<byte> clockSequenceSpan = guid.Slice(guidClockSequenceByte, nodeByte - guidClockSequenceByte);
            BitConverter.GetBytes(Convert.ToInt16(Environment.TickCount % short.MaxValue)).CopyTo(clockSequenceSpan);
            if (BitConverter.IsLittleEndian)
            {
                clockSequenceSpan.Reverse();
            }
            // set the variant
            clockSequenceSpan[variantByte] &= variantByteMask;
            clockSequenceSpan[variantByte] |= variantByteOr;

            // Generates a random value for the node.
            Span<byte> node = guid.Slice(nodeByte, 6);
            var random = new Random();
            random.NextBytes(node);

            return new Guid(guid);
        }
    }
}
