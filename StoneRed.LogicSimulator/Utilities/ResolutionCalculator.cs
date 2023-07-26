using System.Collections.Generic;
using System.Linq;

namespace StoneRed.LogicSimulator.Utilities;

internal static class ResolutionCalculator
{
    public static List<Resolution> GetResolutions(Resolution baseResolution)
    {
        List<Resolution> resolutions = new List<Resolution>();

        int width = baseResolution.Width;
        int height = baseResolution.Height;

        int largestDimension = width > height ? width : height;
        int counter = 0;
        int to = largestDimension;

        while (true)
        {
            to++;
            double scale = (double)largestDimension / to;
            if (width % scale == 0 && height % scale == 0)
            {
                counter++;

                resolutions.Add(new Resolution((int)(width / scale), (int)(height / scale)));
            }
            if (counter >= 4)
            {
                break;
            }
        }

        for (to = largestDimension; to > 0; to--)
        {
            double scale = (double)largestDimension / to;
            if (width % scale == 0 && height % scale == 0 && (int)(width / scale) > 600 && (int)(height / scale) > 600)
            {
                resolutions.Add(new Resolution((int)(width / scale), (int)(height / scale)));
            }
        }

        return resolutions
            .OrderByDescending(r => r.Width)
            .ToList();
    }
}

internal class Resolution
{
    public int Width { get; }

    public int Height { get; }

    public Resolution(int width, int height)
    {
        Width = width;
        Height = height;
    }
}