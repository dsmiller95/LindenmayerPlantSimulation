use crate::diffusion::diffusion_job::{DiffusionAmountData, DiffusionJob};
use crate::diffusion::extract_graph::{SymbolStringMut, SymbolStringRead, SymbolStringWrite};

pub fn apply_diffusion_results<'a>(
    diffusion_job: DiffusionJob,
    double_buffered_data: &DiffusionAmountData,
    target_symbols: &mut SymbolStringMut,
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    clear_amounts: bool,
) -> Option<()>{
    
    let amount_data = if double_buffered_data.latest_in_a {
        &double_buffered_data.node_amount_list_a
    } else {
        &double_buffered_data.node_amount_list_b
    };

    for node_index in 0..diffusion_job.nodes.len() {
        let node = &diffusion_job.nodes[node_index];

        let node_index_in_target = node.index_in_target as usize;
        target_symbols.symbols[node_index_in_target] = diffusion_node_symbol;
        target_symbols.param_indexing[node_index_in_target] = node.target_parameters;
        
        let param_slice = target_symbols.take_param_slice_mut(node_index_in_target);
        param_slice[0] = node.diffusion_constant;
        
        for resource_type in 0..node.total_resource_types {
            let partial_param = (resource_type * 2 + 1) as usize;
            let node_index = (node.index_in_temp_amount_list + resource_type) as usize;
            param_slice[partial_param] = amount_data[node_index];
            param_slice[partial_param + 1] = diffusion_job.node_max_capacities[node_index];
        }
    }
    if clear_amounts {
        for symbol_index in 0..target_symbols.symbols.len() {
            let symbol: i32 = target_symbols.symbols[symbol_index];
            if symbol == diffusion_amount_symbol {
                target_symbols.param_indexing[symbol_index].length = 0;
            }
        }   
    }

    Some(())
}