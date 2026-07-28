using UnityEngine;

namespace GameYT.Warmup
{
    public readonly struct PunchContext
    {
        public PunchContext(Vector3 point, Vector3 direction, float strength)
        {
            Point = point;
            Direction = direction;
            Strength = strength;
        }

        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float Strength { get; }
    }

    public interface IPunchable
    {
        void ReceivePunch(PunchContext context);
    }
}
