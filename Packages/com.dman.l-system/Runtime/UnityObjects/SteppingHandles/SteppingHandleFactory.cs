using System;

namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public class SteppingHandleFactory : ISteppingHandleFactory
    {
        public ISteppingHandle CreateSteppingHandle(
            LSystemObject systemObject,
            LSystemBehavior associatedBehavior,
            LSystemSharing sharingMode)
        {
            ILSystemCompilationStrategy compilationStrategy = sharingMode switch
            {
                LSystemSharing.SharedCompiled => new SharedCompilationStrategy(systemObject),
                LSystemSharing.SelfCompiledWithRuntimeParameters => new IndividuallyCompiledStrategy(systemObject),
                _ => throw new NotImplementedException()
            };

            return new SteppingHandle(systemObject, associatedBehavior, compilationStrategy);
        }

        public ISteppingHandle RehydratedFromSerializableObject(
            ISerializeableSteppingHandle serializedHandle,
            LSystemBehavior associatedBehavior)
        {
            return serializedHandle switch
            {
                SteppingHandle.SavedData saved => new SteppingHandle(saved, associatedBehavior),
                _ => throw new NotImplementedException()
            };
        }
    }

}