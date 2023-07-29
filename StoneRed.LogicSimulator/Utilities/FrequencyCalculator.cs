using System;

namespace StoneRed.LogicSimulator.Utilities;

internal static class FrequencyCalculator
{
    public static string CalculateFrequency(double hz)
    {
        double khz = hz / 1000d;
        double mhz = khz / 1000d;
        double ghz = mhz / 1000d;

        if (ghz >= 1)
        {
            return $"{Math.Round(ghz)}GHz";
        }
        else if (mhz >= 1)
        {
            return $"{Math.Round(mhz)}MHz";
        }
        else if (khz >= 1)
        {
            return $"{Math.Round(khz)}kHz";
        }
        else
        {
            return $"{Math.Round(hz)}Hz";
        }
    }
}