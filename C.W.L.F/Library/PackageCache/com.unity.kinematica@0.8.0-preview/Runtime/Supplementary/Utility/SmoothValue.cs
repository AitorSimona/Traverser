using Unity.Mathematics;

namespace Unity.Kinematica.Supplementary
{
    internal struct SmoothValue
    {
        public static SmoothValue Create(float currentValue)
        {
            return new SmoothValue(currentValue);
        }

        public SmoothValue(float currentValue)
        {
            m_currentValue = currentValue;

            m_targetValue = 0.0f;
            m_currentRate = 0.0f;
        }

        public float CurrentValue
        {
            get { return m_currentValue; }
            set { m_currentValue = value; }
        }

        public float TargetValue
        {
            get { return m_targetValue; }
        }

        public float CriticallyDampedSpring(float targetValue, float timeDelta, float timeScale)
        {
            m_targetValue = targetValue;

            if (timeScale > 0.0f)
            {
                float y = 2.0f / timeScale;
                float x = y * timeDelta;
                float exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
                float change = m_currentValue - targetValue;
                float temp = (m_currentRate + (change * y)) * timeDelta;
                m_currentRate = (m_currentRate - (temp * y)) * exp;
                m_currentValue = targetValue + (change + temp) * exp;
            }
            else if (timeDelta > 0.0f)
            {
                m_currentRate = (targetValue - m_currentValue) / timeDelta;
                m_currentValue = targetValue;
            }

            return m_currentValue;
        }

        public float ExponentialDecay(float targetValue, float timeDelta, float timeScale)
        {
            m_targetValue = targetValue;

            // Exponential decay to target
            if (timeScale > 0.0f)
            {
                // Smooths value towards the target to with a timestep of timeDelta, smoothing over timeScale.
                // This avoids the cost of evaluating the exponential function by a second order expansion,
                // and is stable and accurate (for all practical purposes) for all timeDelta values.
                float lambda = timeDelta / timeScale;
                m_currentValue = targetValue + (m_currentValue - targetValue) / (1.0f + lambda + 0.5f * lambda * lambda);
            }
            else if (timeDelta > 0.0f)
            {
                m_currentValue = targetValue;
            }

            return m_currentValue;
        }

        private float m_targetValue;
        private float m_currentValue;
        private float m_currentRate;
    }

    internal struct SmoothValue2
    {
        public static SmoothValue2 Create(float2 currentValue)
        {
            return new SmoothValue2(currentValue);
        }

        public SmoothValue2(float2 currentValue)
        {
            m_currentValue = currentValue;

            m_targetValue = float2.zero;
            m_currentRate = float2.zero;
        }

        public float2 CurrentValue
        {
            get { return m_currentValue; }
            set { m_currentValue = value; }
        }

        public float2 TargetValue
        {
            get { return m_targetValue; }
        }

        public float2 CriticallyDampedSpring(float2 targetValue, float timeDelta, float timeScale)
        {
            m_targetValue = targetValue;

            if (timeScale > 0.0f)
            {
                float y = 2.0f / timeScale;
                float x = y * timeDelta;
                float exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
                float2 change = m_currentValue - targetValue;
                float2 temp = (m_currentRate + (change * y)) * timeDelta;
                m_currentRate = (m_currentRate - (temp * y)) * exp;
                m_currentValue = targetValue + (change + temp) * exp;
            }
            else if (timeDelta > 0.0f)
            {
                m_currentRate = (targetValue - m_currentValue) / timeDelta;
                m_currentValue = targetValue;
            }

            return m_currentValue;
        }

        public float2 ExponentialDecay(float2 targetValue, float timeDelta, float timeScale)
        {
            m_targetValue = targetValue;

            // Exponential decay to target
            if (timeScale > 0.0f)
            {
                // Smooths value towards the target to with a timestep of timeDelta, smoothing over timeScale.
                // This avoids the cost of evaluating the exponential function by a second order expansion,
                // and is stable and accurate (for all practical purposes) for all timeDelta values.
                float lambda = timeDelta / timeScale;
                m_currentValue = targetValue + (m_currentValue - targetValue) / (1.0f + lambda + 0.5f * lambda * lambda);
            }
            else if (timeDelta > 0.0f)
            {
                m_currentValue = targetValue;
            }

            return m_currentValue;
        }

        private float2 m_targetValue;
        private float2 m_currentValue;
        private float2 m_currentRate;
    }

    internal struct SmoothValue3
    {
        public static SmoothValue3 Create(float3 currentValue)
        {
            return new SmoothValue3(currentValue);
        }

        public SmoothValue3(float3 currentValue)
        {
            m_currentValue = currentValue;

            m_targetValue = float3.zero;
            m_currentRate = float3.zero;
        }

        public float3 CurrentValue
        {
            get { return m_currentValue; }
            set { m_currentValue = value; }
        }

        public float3 TargetValue
        {
            get { return m_targetValue; }
        }

        public float3 CriticallyDampedSpring(float3 targetValue, float timeDelta, float timeScale)
        {
            m_targetValue = targetValue;

            if (timeScale > 0.0f)
            {
                float y = 2.0f / timeScale;
                float x = y * timeDelta;
                float exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
                float3 change = m_currentValue - targetValue;
                float3 temp = (m_currentRate + (change * y)) * timeDelta;
                m_currentRate = (m_currentRate - (temp * y)) * exp;
                m_currentValue = targetValue + (change + temp) * exp;
            }
            else if (timeDelta > 0.0f)
            {
                m_currentRate = (targetValue - m_currentValue) / timeDelta;
                m_currentValue = targetValue;
            }

            return m_currentValue;
        }

        public float3 ExponentialDecay(float3 targetValue, float timeDelta, float timeScale)
        {
            m_targetValue = targetValue;

            // Exponential decay to target
            if (timeScale > 0.0f)
            {
                // Smooths value towards the target to with a timestep of timeDelta, smoothing over timeScale.
                // This avoids the cost of evaluating the exponential function by a second order expansion,
                // and is stable and accurate (for all practical purposes) for all timeDelta values.
                float lambda = timeDelta / timeScale;
                m_currentValue = targetValue + (m_currentValue - targetValue) / (1.0f + lambda + 0.5f * lambda * lambda);
            }
            else if (timeDelta > 0.0f)
            {
                m_currentValue = targetValue;
            }

            return m_currentValue;
        }

        private float3 m_targetValue;
        private float3 m_currentValue;
        private float3 m_currentRate;
    }
}
