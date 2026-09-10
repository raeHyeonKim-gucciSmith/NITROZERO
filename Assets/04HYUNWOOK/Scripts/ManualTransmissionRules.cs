using System;

/// <summary>Gear ratios expressed as road speed at redline; independent of rendering and input.</summary>
public static class ManualTransmissionRules
{
    public static int NormalizeGear(int gear) => gear < 0 ? -1 : Math.Max(1, Math.Min(6, gear));

    public static int NextGear(int current, int direction)
    {
        if (current < 1) return current; // Q/E do not enter or leave reverse.
        current = NormalizeGear(current);
        if (direction == 0) return current;
        if (direction > 0) return Math.Min(6, current + 1);
        return Math.Max(1, current - 1);
    }

    public struct PedalState
    {
        public int Gear;
        public bool Throttle, Brake;
        public PedalState(int gear, bool throttle, bool brake)
        { Gear = gear; Throttle = throttle; Brake = brake; }
    }

    public static PedalState ResolvePedals(int gear, double signedSpeedKmh, bool forward, bool reverse, bool brake)
    {
        if (brake || (forward && reverse)) return new PedalState(gear, false, true);
        if (forward)
            return signedSpeedKmh < -1 ? new PedalState(gear, false, true)
                : new PedalState(gear < 0 ? 1 : gear, true, false);
        if (reverse)
            return signedSpeedKmh > 1 ? new PedalState(gear, false, true)
                : new PedalState(-1, true, false);
        return new PedalState(gear, false, false);
    }

    public static double CoupledRpm(double speedKmh, double gearSpeedLimit, double idle, double redline)
        => Math.Max(idle, Math.Min(redline, Math.Abs(speedKmh) / Math.Max(1, gearSpeedLimit) * redline));

    public static bool CanSelectGear(int gear, double signedSpeedKmh, double gearSpeedLimit, double redline)
    {
        if (gear < -1 || gear == 0 || gear > 6) return false;
        if (gear < 0 && Math.Abs(signedSpeedKmh) > 1) return false;
        if (gear > 0 && signedSpeedKmh < -1) return false;
        return Math.Abs(signedSpeedKmh) / Math.Max(1, gearSpeedLimit) * redline <= redline;
    }

    public static double TorqueRatio(double firstGearLimit, double currentGearLimit)
        => Math.Max(0.1, Math.Min(1, firstGearLimit / Math.Max(1, currentGearLimit)));
}
