namespace StoneRed.LogicSimulator.Api.Utilities;

#pragma warning disable S112 // General exceptions should never be thrown

internal static class BitHelper
{
    public static void SetBit(this ref int input, int value, int index)
    {
        if (index >= 32)
        {
            throw new IndexOutOfRangeException();
        }
        if (value == 1)
        {
            input |= 1 << index;
        }
        else
        {
            input &= ~(1 << index);
        }
    }

    public static int GetBit(this int input, int index)
    {
        if (index >= 32)
        {
            throw new IndexOutOfRangeException();
        }

        return (input >> index) & 1;
    }
}

#pragma warning restore S112 // General exceptions should never be thrown