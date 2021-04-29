using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemRuntime.DynamicExpressions
{

    [StructLayout(LayoutKind.Explicit)]
    public struct OperatorDefinition
    {
        [FieldOffset(0)] public OperatorType operatorType;
        /// <summary>
        /// is set when the node has a constant value
        /// </summary>
        [FieldOffset(1)] public float nodeValue;
        /// <summary>
        /// used when operator is PARAMETER_VALUE. carries an index in input parameters
        ///     from which to pull the value for this operator
        /// </summary>
        [FieldOffset(1)] public int parameterIndex;
        /// <summary>
        /// used when operator is unary or binary op. index of the right hand side value in the operator
        ///     data array
        /// </summary>
        [FieldOffset(1)] public ushort rhs;
        /// <summary>
        /// used when operator is binary op. index of the left hand side value in the operator
        ///     data array
        /// </summary>
        [FieldOffset(3)] public ushort lhs;
    }

    public enum OperatorType: byte
    {
        // constants
        CONSTANT_VALUE,
        PARAMETER_VALUE,

        // binary ops
        MULTIPLY,
        DIVIDE,
        ADD,
        SUBTRACT,
        REMAINDER,
        EXPONENT,

        GREATER_THAN,
        LESS_THAN,
        GREATER_THAN_OR_EQ,
        LESS_THAN_OR_EQ,
        EQUAL,
        NOT_EQUAL,

        BOOLEAN_AND,
        BOOLEAN_OR,

        // unary ops
        BOOLEAN_NOT,
        NEGATE_UNARY
    }
}
