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

        [DllImport(__DllName, EntryPoint = "diffuse_between", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool diffuse_between(DiffusionEdge* edges, int edges_len, DiffusionNode* nodes, float* node_capacities, float* node_amount_list_a, float* node_amount_list_b, int total_nodes, int diffusion_steps, float diffusion_global_multiplier);

        [DllImport(__DllName, EntryPoint = "perform_parallel_diffusion", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool perform_parallel_diffusion(SymbolStringInterop* source_data, SymbolStringInteropMut* target_data, NativeArrayInteropLSystemSingleSymbolMatchData* match_singleton_data, int diffusion_node_symbol, int diffusion_amount_symbol, int branch_open_symbol, int branch_close_symbol, int diffusion_steps, float diffusion_global_multiplier);

        [DllImport(__DllName, EntryPoint = "perform_in_place_diffusion", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool perform_in_place_diffusion(SymbolStringInteropMut* source_data, int diffusion_node_symbol, int diffusion_amount_symbol, int branch_open_symbol, int branch_close_symbol, int diffusion_steps, float diffusion_global_multiplier);

        [DllImport(__DllName, EntryPoint = "evaluate_expression", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float evaluate_expression(OperatorDefinition* operation_data, JaggedIndexing* operation_space, float* parameter_values, JaggedIndexing* parameter_space, float* parameter_values_2, JaggedIndexing* parameter_space_2);

        [DllImport(__DllName, EntryPoint = "double_input", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int double_input(int input);

        [DllImport(__DllName, EntryPoint = "triple_input", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int triple_input(int input);


    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct JaggedIndexing
    {
        public int index;
        public ushort length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropi32Mut
    {
        public int* data;
        public int len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropi32
    {
        public int* data;
        public int len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropf32Mut
    {
        public float* data;
        public int len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropf32
    {
        public float* data;
        public int len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropJaggedIndexingMut
    {
        public JaggedIndexing* data;
        public int len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropJaggedIndexing
    {
        public JaggedIndexing* data;
        public int len;
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

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct SymbolStringInterop
    {
        public NativeArrayInteropi32 symbols;
        public NativeArrayInteropJaggedIndexing parameter_indexing;
        public NativeArrayInteropf32 parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct SymbolStringInteropMut
    {
        public NativeArrayInteropi32Mut symbols;
        public NativeArrayInteropJaggedIndexingMut parameter_indexing;
        public NativeArrayInteropf32Mut parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct LSystemSingleSymbolMatchData
    {
        public JaggedIndexing tmp_parameter_memory_space;
        [MarshalAs(UnmanagedType.U1)] public bool is_trivial;
        public byte matched_rule_index_in_possible;
        public byte selected_replacement_pattern;
        public JaggedIndexing replacement_symbol_indexing;
        public JaggedIndexing replacement_parameter_indexing;
        public LSystemMatchErrorCode error_code;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeArrayInteropLSystemSingleSymbolMatchData
    {
        public LSystemSingleSymbolMatchData* data;
        public int len;
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


    public enum LSystemMatchErrorCode : byte
    {
        None = 0,
        TooManyParameters = 1,
        TrivialSymbolNotIndicatedAtMatchTime = 2,
        TrivialSymbolNotIndicatedAtReplacementTime = 3,
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
    