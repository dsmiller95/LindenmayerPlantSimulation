use crate::interop_extern::expressions::{OperatorDefinition, OperatorType};

pub fn evaluate_expression(
    operation_data: &[OperatorDefinition],
    parameter_values: &[f32],
    parameter_values_2: &[f32],
) -> f32 {
    let evaluator = DynamicExpressionEvaluator {
        operation_data,
        parameter_values,
        parameter_values_2,
    };

    evaluator.evaluate(0)
}

pub struct DynamicExpressionEvaluator<'a> {
    operation_data: &'a [OperatorDefinition],
    parameter_values: &'a [f32],
    parameter_values_2: &'a [f32],
}

impl DynamicExpressionEvaluator<'_> {
    pub fn evaluate(&self, index: usize) -> f32 {
        let operation = &self.operation_data[index];

        match operation.operator_type {
            OperatorType::ConstantValue => {
                operation.node_value
            }
            OperatorType::ParameterValue => {
                let parameter_index = operation.parameter_index as usize;
                if parameter_index >= self.parameter_values.len() as usize {
                    let parameter_index = parameter_index - self.parameter_values.len() as usize;
                    let parameter = self.parameter_values_2[parameter_index];
                    parameter
                } else {
                    let parameter = self.parameter_values[parameter_index];
                    parameter
                }
            }
            OperatorType::Multiply => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs * rhs
            }
            OperatorType::Divide => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs / rhs
            }
            OperatorType::Add => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs + rhs
            }
            OperatorType::Subtract => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs - rhs
            }
            OperatorType::Remainder => {
                let lhs = self.evaluate(operation.lhs as usize);
                let rhs = self.evaluate(operation.rhs as usize);
                lhs % rhs
            }
            OperatorType::Exponent => {
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
            OperatorType::Equal => {
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
                if rhs > 0.1 { 0.0 } else { 1.0 }
            }
            OperatorType::NegateUnary => {
                let rhs = self.evaluate(operation.rhs as usize);
                -rhs
            }
        }
    }
}