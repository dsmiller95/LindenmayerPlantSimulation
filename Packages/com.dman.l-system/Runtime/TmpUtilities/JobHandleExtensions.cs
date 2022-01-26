using Unity.Jobs;

namespace Dman.LSystem
{
    public struct JobHandleWrapper
    {
        public JobHandle Handle { get; private set; }
        public JobHandleWrapper(JobHandle jh) { Handle = jh; }

        public static implicit operator JobHandleWrapper(JobHandle jh)
        {
            return new JobHandleWrapper(jh);
        }

        public static implicit operator JobHandle(JobHandleWrapper jh)
        {
            return jh.Handle;
        }

        public static JobHandleWrapper operator +(JobHandleWrapper a, JobHandleWrapper b)
        {
            return JobHandle.CombineDependencies(a, b);
        }
    }
}