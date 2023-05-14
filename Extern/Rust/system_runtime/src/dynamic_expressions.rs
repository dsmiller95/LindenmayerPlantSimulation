use crate::{Expression, JaggedIndexing, OperatorDefinition, OperatorType};
use crate::dynamic_expressions::struct_expression::Indexes;

pub mod struct_expression;

pub fn evaluate_expression(
    expression: Expression,
    operation_data: *const OperatorDefinition,
    operation_space: JaggedIndexing,
    parameter_values: *const f32,
    parameter_space: JaggedIndexing,
    parameter_values_2: *const f32,
    parameter_space_2: JaggedIndexing,
) -> f32 {
    let evaluator = DynamicExpressionEvaluator {
        expression,
        operation_data,
        operation_data_index: operation_space,
        parameter_values,
        parameter_values_index: parameter_space,
        parameter_values_2,
        parameter_values_index_2: parameter_space_2,
    };

    unsafe { evaluator.evaluate(0) }
}

pub struct DynamicExpressionEvaluator {
    expression: Expression,
    operation_data: *const OperatorDefinition,
    operation_data_index: JaggedIndexing,
    parameter_values: *const f32,
    parameter_values_index: JaggedIndexing,
    parameter_values_2: *const f32,
    parameter_values_index_2: JaggedIndexing,

}

impl DynamicExpressionEvaluator {
    pub unsafe fn evaluate(&self, index: usize) -> f32 {
        let operation = &*self.operation_data_index.index_in(self.operation_data, index);

        match operation.operator_type {
            OperatorType::ConstantValue => {
                operation.node_value
            }
            OperatorType::ParameterValue => {
                let parameter_index = operation.parameter_index as usize;
                if parameter_index >= self.parameter_values_index.length as usize {
                    let parameter_index = parameter_index - self.parameter_values_index.length as usize;
                    let parameter = self.parameter_values_index_2.index_in(self.parameter_values_2, parameter_index);
                    *parameter
                } else {
                    let parameter = self.parameter_values_index.index_in(self.parameter_values, parameter_index);
                    *parameter
                }
            }
            OperatorType::MULTIPLY => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs * rhs
            }
            OperatorType::DIVIDE => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs / rhs
            }
            OperatorType::ADD => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs + rhs
            }
            OperatorType::SUBTRACT => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs - rhs
            }
            OperatorType::REMAINDER => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs % rhs
            }
            OperatorType::EXPONENT => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs.powf(rhs)
            }
            OperatorType::GreaterThan => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if lhs > rhs { 1.0 } else { 0.0 }
            }
            OperatorType::LessThan => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if lhs < rhs { 1.0 } else { 0.0 }
            }
            OperatorType::GreaterThanOrEq => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if lhs >= rhs { 1.0 } else { 0.0 }
            }
            OperatorType::LessThanOrEq => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if lhs <= rhs { 1.0 } else { 0.0 }
            }
            OperatorType::EQUAL => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if lhs == rhs { 1.0 } else { 0.0 }
            }
            OperatorType::NotEqual => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if lhs != rhs { 1.0 } else { 0.0 }
            }
            OperatorType::BooleanAnd => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                if (lhs > 0.1) && (rhs > 0.1) { 1.0 } else { 0.0 }
            }
            OperatorType::BooleanOr => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs + rhs
            }
            OperatorType::BooleanNot => {
                let rhs = self.evaluate(operation.rhs as usize);
                if rhs > 0.1 { 1.0 } else { 0.0 }
            }
            OperatorType::NegateUnary => {
                let rhs = self.evaluate(operation.rhs as usize);
                -rhs
            }
        }
    }
}