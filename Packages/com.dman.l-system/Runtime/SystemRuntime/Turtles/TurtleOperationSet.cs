
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{


    public abstract class TurtleOperationSet : ScriptableObject, ITurtleNativeDataWritable
    {
        public virtual TurtleDataRequirements DataReqs => default;

        public virtual void InternalCacheOperations()
        {

        }
        public abstract void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer);
    }
}
