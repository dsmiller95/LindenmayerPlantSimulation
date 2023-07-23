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
            
            return sharingMode switch
            {
                LSystemSharing.SharedCompiled => 
                    new SharedCompiledSteppingHandle(systemObject, associatedBehavior, compilationStrategy),
                LSystemSharing.SelfCompiledWithRuntimeParameters =>
                    new IndividuallyCompiledSteppingHandle(systemObject, associatedBehavior, compilationStrategy),
                _ => throw new NotImplementedException()
            };
        }

        public ISteppingHandle RehydratedFromSerializableObject(
            ISerializeableSteppingHandle serializedHandle,
            LSystemBehavior associatedBehavior)
        {
            switch(serializedHandle)
            {
                case SharedCompiledSteppingHandle.SavedData saved:
                    
                    return new SharedCompiledSteppingHandle(saved, associatedBehavior);
                case IndividuallyCompiledSteppingHandle.SavedData saved:
                    return new IndividuallyCompiledSteppingHandle(saved, associatedBehavior);
                default:
                    throw new NotImplementedException();
            }
        }
    }

}