use crate::dynamic_expressions;
use crate::interop_extern::data::{IndexesIn, JaggedIndexing};

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
