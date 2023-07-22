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
            return sharingMode switch
            {
                LSystemSharing.SharedCompiled => 
                    new SharedCompiledSteppingHandle(systemObject, associatedBehavior),
                LSystemSharing.SelfCompiledWithRuntimeParameters =>
                    new IndividuallyCompiledSteppingHandle(systemObject, associatedBehavior),
                _ => throw new NotImplementedException()
            };
        }

        public ISteppingHandle RehydratedFromSerializableObject(ISerializeableSteppingHandle serializedHandle, LSystemBehavior associatedBehavior)
        {
            return serializedHandle switch
            {
                SharedCompiledSteppingHandle.SavedData saved => new SharedCompiledSteppingHandle(saved,
                    associatedBehavior),
                IndividuallyCompiledSteppingHandle.SavedData saved => new IndividuallyCompiledSteppingHandle(saved,
                    associatedBehavior),
                _ => throw new NotImplementedException()
            };
        }
    }

}