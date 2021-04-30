
using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public abstract class TurtleOperationSet : ScriptableObject
    {
        public virtual int TotalOrganSpaceNeeded => 0;

        public abstract IEnumerable<KeyValuePair<int, TurtleOperation>> GetOperators(NativeArrayBuilder<TurtleEntityPrototypeOrganTemplate> organWriter);
    }
}
