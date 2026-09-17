using UnityEngine;

namespace DroneMod.Util
{
    /// <summary>
    /// Small PID controller with integral clamping and derivative-on-measurement
    /// (so a step change in the setpoint does not produce a derivative spike).
    /// </summary>
    internal sealed class PidController
    {
        public float Kp;
        public float Ki;
        public float Kd;

        /// <summary>Absolute clamp on the integral term's contribution. Prevents wind-up during saturation.</summary>
        public float IntegralLimit;

        /// <summary>Absolute clamp on the controller output.</summary>
        public float OutputLimit;

        private float _integral;
        private float _lastMeasurement;
        private bool _primed;

        public PidController(float kp, float ki, float kd, float integralLimit, float outputLimit)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            IntegralLimit = integralLimit;
            OutputLimit = outputLimit;
        }

        public float Error { get; private set; }

        public void Reset()
        {
            _integral = 0f;
            _primed = false;
            Error = 0f;
        }

        public float Update(float setpoint, float measurement, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            Error = setpoint - measurement;

            _integral = Mathf.Clamp(_integral + Error * deltaTime, -IntegralLimit, IntegralLimit);

            float derivative = 0f;
            if (_primed)
            {
                // Negated because we differentiate the measurement, not the error.
                derivative = -(measurement - _lastMeasurement) / deltaTime;
            }

            _lastMeasurement = measurement;
            _primed = true;

            float output = Kp * Error + Ki * _integral + Kd * derivative;
            return Mathf.Clamp(output, -OutputLimit, OutputLimit);
        }
    }
}
