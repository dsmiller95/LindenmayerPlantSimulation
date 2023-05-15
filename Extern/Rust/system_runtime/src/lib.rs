use crate::dynamic_expressions::indexes_in::IndexesIn;

mod dynamic_expressions;

#[no_mangle]
pub extern "C" fn double_input(input: i32) -> i32 {
    input * 2
}

#[no_mangle]
pub extern "C" fn triple_input(input: i32) -> i32 {
    input * 3
}

#[repr(C)]
#[derive(Copy, Clone)]
pub struct JaggedIndexing {
    pub index: i32,
    pub length: u16,
}

#[repr(u8)]
pub enum OperatorType
{
    // constants
    ConstantValue,
    ParameterValue,

    // binary ops
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

    // unary ops
    BooleanNot,
    NegateUnary,
}


#[repr(C)]
pub struct OperatorDefinition {
    pub operator_type: OperatorType,
    /// is set when the node has a constant value
    pub node_value: f32,
    /// used when operator is PARAMETER_VALUE. carries an index in input parameters
    ///     from which to pull the value for this operator
    pub parameter_index: i32,

    /// used when operator is unary or binary op. index of the right hand side value in the operator
    ///     data array
    pub rhs: u16,
    /// used when operator is binary op. index of the left hand side value in the operator
    ///     data array
    pub lhs: u16,
}

#[repr(C)]
pub union NodeProperty {
    /// is set when the node has a constant value
    pub node_value: f32,
    /// used when operator is PARAMETER_VALUE. carries an index in input parameters
    ///     from which to pull the value for this operator
    pub parameter_index: i32,
}

#[no_mangle]
pub extern "C" fn evaluate_expression(
    operation_data: *const OperatorDefinition,
    operation_space: *const JaggedIndexing,
    parameter_values: *const f32,
    parameter_space: *const JaggedIndexing,
    parameter_values_2: *const f32,
    parameter_space_2: *const JaggedIndexing,
) -> f32 {
    let (operations, param1, param2) = unsafe {
        (
            (*operation_space).to_slice(operation_data),
            (*parameter_space).to_slice(parameter_values),
            (*parameter_space_2).to_slice(parameter_values_2),
        )
    };
    dynamic_expressions::evaluate_expression(operations, param1, param2)
}

mod diffusion_job;

#[repr(C)]
pub struct DiffusionEdge{
    pub node_a_index: i32,
    pub node_b_index: i32,
}

#[repr(C)]
pub struct DiffusionNode{
    pub index_in_target: i32,
    pub target_parameters: JaggedIndexing,
    pub index_in_temp_amount_list: i32,

    pub total_resource_types: i32,
    pub diffusion_constant: f32
}

#[no_mangle]
pub extern "C" fn diffuse_between(
    edges: *const DiffusionEdge,
    edges_len: i32,
    nodes: *const DiffusionNode,
    node_capacities: *const f32,
    node_amount_list_a: *mut f32,
    node_amount_list_b: *mut f32,
    total_nodes: i32,
    
    diffusion_steps: i32,
    diffusion_global_multiplier: f32,
) -> bool {
    let (
        edges,
        nodes, 
        node_capacities,
        node_amounts_slice_a,
        node_amounts_slice_b) = unsafe {
        (
            std::slice::from_raw_parts(edges, edges_len as usize),
            std::slice::from_raw_parts(nodes, total_nodes as usize),
            std::slice::from_raw_parts(node_capacities, total_nodes as usize),
            std::slice::from_raw_parts_mut(node_amount_list_a, total_nodes as usize),
            std::slice::from_raw_parts_mut(node_amount_list_b, total_nodes as usize),
        )
    };
    
    let diffusion_job = diffusion_job::DiffusionJob {
        edges,
        nodes,
        node_max_capacities: node_capacities,
        
        diffusion_global_multiplier,
    };
    
    let double_buffered_amounts = & mut diffusion_job::DiffusionAmountData{
        node_amount_list_a: node_amounts_slice_a,
        node_amount_list_b: node_amounts_slice_b,
        latest_in_a: true,
    };
    
    diffusion_job.diffuse_between(
        double_buffered_amounts,
        diffusion_steps);
    
    double_buffered_amounts.latest_in_a
}