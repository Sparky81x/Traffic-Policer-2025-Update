using System;
using System.Linq;
using Albo1125.Common.CommonLibrary;
using static Albo1125.Common.CommonLibrary.ExtensionMethods;

namespace Traffic_Policer
{
    internal static class LicensePlateGenerator
    {
        private static  Random Rnd => TrafficPolicerHandler.rnd;

        public static PlateFormat GetRandomPlateFormat()
        {
            PlateFormat[] formats =
            {
                PlateFormat.Default,
                PlateFormat.Classic,
                PlateFormat.Gov,
                PlateFormat.EMS,
                PlateFormat.Police,
                PlateFormat.SAPlate,
                PlateFormat.CivilianShort,
                PlateFormat.Motorcycle,
                PlateFormat.Diplomat
            };

            return formats[Rnd.Next(formats.Length)];
        }

        public static string GenerateLicensePlate(PlateFormat format)
        {
            switch (format)
            {
                case PlateFormat.Classic:
                    return $"{RandomLetters(3)}{RandomDigits(4)}";
                case PlateFormat.Gov:
                    return $"SA GOV {RandomDigits(3)}";
                case PlateFormat.EMS:
                    return $"EMS {RandomDigits(4)}";
                case PlateFormat.Police:
                    return $"POL{RandomLetters(2)}{RandomDigits(3)}";
                case PlateFormat.SAPlate:
                    return $"{RandomLetters(3)} {RandomDigits(3)}";
                case PlateFormat.CivilianShort:
                    return $"{RandomLetters(2)}{RandomDigits(2)}{RandomLetters(2)}";
                case PlateFormat.Motorcycle:
                    return $"MC {RandomLetters(2)} {RandomDigits(3)}";
                case PlateFormat.Diplomat:
                    return $"CD {RandomDigits(4)}";
                default:
                    return $"{RandomLetters(3)}{RandomDigits(3)}";
            }
        }

        private static string RandomLetters(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Rnd.Next(s.Length)]).ToArray());
        }

        private static string RandomDigits(int length)
        {
            const string digits = "0123456789";
            return new string(Enumerable.Repeat(digits, length)
                .Select(s => s[Rnd.Next(s.Length)]).ToArray());
        }
    }
}

