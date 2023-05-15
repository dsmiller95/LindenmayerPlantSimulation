// <auto-generated>
// This code is generated by csbindgen.
// DON'T CHANGE THIS DIRECTLY.
// </auto-generated>
#pragma warning disable CS8500
#pragma warning disable CS8981
using System;
using System.Runtime.InteropServices;

namespace Dman.LSystem.Extern
{
    public static unsafe partial class SystemRuntimeRust
    {
        const string __DllName = "system_runtime_rustlib";

        [DllImport(__DllName, EntryPoint = "double_input", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int double_input(int input);

        [DllImport(__DllName, EntryPoint = "triple_input", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int triple_input(int input);

        [DllImport(__DllName, EntryPoint = "evaluate_expression", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float evaluate_expression(OperatorDefinition* operation_data, JaggedIndexing* operation_space, float* parameter_values, JaggedIndexing* parameter_space, float* parameter_values_2, JaggedIndexing* parameter_space_2);

        [DllImport(__DllName, EntryPoint = "diffuse_between", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool diffuse_between(DiffusionEdge* edges, int edges_len, DiffusionNode* nodes, float* node_capacities, float* node_amount_list_a, float* node_amount_list_b, int total_nodes, int diffusion_steps, float diffusion_global_multiplier);


    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct JaggedIndexing
    {
        public int index;
        public ushort length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct OperatorDefinition
    {
        public OperatorType operator_type;
        public float node_value;
        public int parameter_index;
        public ushort rhs;
        public ushort lhs;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct DiffusionEdge
    {
        public int node_a_index;
        public int node_b_index;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct DiffusionNode
    {
        public int index_in_target;
        public JaggedIndexing target_parameters;
        public int index_in_temp_amount_list;
        public int total_resource_types;
        public float diffusion_constant;
    }


    public enum OperatorType : byte
    {
        ConstantValue,
        ParameterValue,
        Multiply,
        Divide,
        Add,
        Subtract,
        Remainder,
        Exponent,
        GreaterThan,
        LessThan,
        GreaterThanOrEq,
        LessThanOrEq,
        Equal,
        NotEqual,
        BooleanAnd,
        BooleanOr,
        BooleanNot,
        NegateUnary,
    }


}
    