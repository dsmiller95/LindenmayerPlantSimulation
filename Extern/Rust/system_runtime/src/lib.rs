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
pub struct JaggedIndexing {
    pub index: i32,
    pub length: u16,
}

#[repr(C)]
pub struct Expression {
    pub operation_data_slice: JaggedIndexing,
}

#[repr(u8)]
pub enum OperatorType
{
    // constants
    ConstantValue,
    ParameterValue,

    // binary ops
    MULTIPLY,
    DIVIDE,

    ADD,
    SUBTRACT,
    REMAINDER,
    EXPONENT,

    GreaterThan,
    LessThan,
    GreaterThanOrEq,
    LessThanOrEq,
    EQUAL,
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
    pub rhs: i16,
    /// used when operator is binary op. index of the left hand side value in the operator
    ///     data array
    pub lhs: i16,
}

#[no_mangle]
pub extern "C" fn evaluate_expression(
    expression: Expression,
    operation_data: *const OperatorDefinition,
    operation_space: JaggedIndexing,
    parameter_values: *const f32,
    parameter_space: JaggedIndexing,
    parameter_values_2: *const f32,
    parameter_space_2: JaggedIndexing,
) -> f32 {
    dynamic_expressions::evaluate_expression(
        expression,
        operation_data,
        operation_space,
        parameter_values,
        parameter_space,
        parameter_values_2,
        parameter_space_2,
    )
}
