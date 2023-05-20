use crate::diffusion::diffusion_job::{DiffusionAmountData, DiffusionJob};
use crate::diffusion::extract_graph::{SymbolStringMut, SymbolStringWrite};

pub fn apply_diffusion_results<'a>(
    diffusion_job: DiffusionJob,
    double_buffered_data: &DiffusionAmountData,
    target_symbols: &mut SymbolStringMut,
    diffusion_node_symbol: i32,
) -> Option<()>{
    
    let amount_data = if double_buffered_data.latest_in_a {
        &double_buffered_data.node_amount_list_a
    } else {
        &double_buffered_data.node_amount_list_b
    };

    for node_index in 0..diffusion_job.nodes.len() {
        let node = &diffusion_job.nodes[node_index];

        target_symbols.symbols[node.index_in_target as usize] = diffusion_node_symbol;

        target_symbols.param_indexing[node.index_in_target as usize] = node.target_parameters;

        target_symbols.set_param_for(node.target_parameters, 0, node.diffusion_constant);
        
        for resource_type in 0..node.total_resource_types {
            target_symbols.set_param_for(
                node.target_parameters,
                (resource_type * 2 + 1) as usize,
                amount_data[(node.index_in_temp_amount_list + resource_type) as usize]);
            target_symbols.set_param_for(
                node.target_parameters,
                (resource_type * 2 + 2) as usize,
                diffusion_job.node_max_capacities[(node.index_in_temp_amount_list + resource_type) as usize]);
        }
    }

    Some(())
}