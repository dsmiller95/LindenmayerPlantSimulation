namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public interface ISteppingHandleFactory
    {
        public ISteppingHandle CreateSteppingHandle(
            LSystemObject systemObject,
            LSystemBehavior associatedBehavior,
            LSystemSharing sharingMode);
        
        public ISteppingHandle RehydratedFromSerializableObject(ISerializeableSteppingHandle serializedHandle, LSystemBehavior associatedBehavior);
    }

    public enum LSystemSharing
    {
        SharedCompiled,
        SelfCompiledWithRuntimeParameters,
    }
}