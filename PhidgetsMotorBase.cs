// MotorControlCommon.cs
using System;

namespace Phidgets2Prosim
{
    /// <summary>
    /// All tuning knobs used by both BLDC and DC controllers.
    /// Keep values conservative by default; override per-motor/axis as needed.
    /// </summary>
    public class MotorTuningOptions
    {
        public double? MaxVelocity { get; set; }            // 0..1 cap when error is large
        public double? MinVelocity { get; set; }            // small bias to overcome stiction
        public double? VelocityBand { get; set; }           // error distance where MaxVelocity is reached (units of feedback)
        public double? CurveGamma { get; set; }             // non-linear shaping of error→velocity
        public double? DeadbandEnter { get; set; }          // settle (stop) threshold (units of feedback)
        public double? DeadbandExit { get; set; }           // re-activate threshold (units of feedback)
        public double? MaxVelStepPerTick { get; set; }      // slew rate limit per tick
        public double? Kp { get; set; }                     // proportional gain
        public double? Ki { get; set; }                     // integral gain
        public double? Kd { get; set; }                     // derivative gain
        public double? IntegralLimit { get; set; }          // anti-windup clamp (velocity units)
        public double? PositionFilterAlpha { get; set; }    // low-pass on feedback (0..1)
        public int? TickMs { get; set; }                    // control loop period (ms)
    }

    internal static class ControlMath
    {
        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
