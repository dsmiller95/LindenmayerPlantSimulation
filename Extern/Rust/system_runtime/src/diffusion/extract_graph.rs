use std::collections::vec_deque::VecDeque;
use crate::diffusion::diffusion_job::{DiffusionAmountData, DiffusionJob};
use crate::interop_extern::data::JaggedIndexing;
use crate::interop_extern::diffusion::{ LSystemSingleSymbolMatchData};

pub trait SymbolStringRead {
    fn param_for(&self, param_index: JaggedIndexing, index_in_param: usize) -> f32;
    fn len(&self) -> usize;
    fn symbol_at(&self, index: usize) -> i32;
    fn take_slice(&self, param_index: JaggedIndexing) -> &[f32];
    fn take_param_slice(&self, symbol_index: usize) -> &[f32];
}

pub trait SymbolStringWrite {
    fn set_param_for(&mut self, param_index: JaggedIndexing, index_in_param: usize, new_value: f32) -> ();
    fn take_param_slice_mut(&mut self, symbol_index: usize) -> &mut [f32];
}

pub struct SymbolString<'a> {
    pub symbols: &'a [i32],
    pub param_indexing: &'a [JaggedIndexing],
    pub parameters: &'a [f32],
}
impl SymbolStringRead for SymbolString<'_>{
    fn param_for(&self, param_index: JaggedIndexing, index_in_param: usize) -> f32 {
        if index_in_param > param_index.length as usize {
            panic!("Index ({}) in param of len ({}) is out of bounds", index_in_param, param_index.length)
        }
        let true_index = param_index.index as usize + index_in_param;
        self.parameters[true_index]
    }
    fn len(&self) -> usize {
        self.symbols.len()
    }
    fn symbol_at(&self, index: usize) -> i32 {
        self.symbols[index]
    }
    fn take_slice(&self, param_index: JaggedIndexing) -> &[f32] {
        let true_index = param_index.index as usize;
        &self.parameters[true_index..true_index + param_index.length as usize]
    }
    fn take_param_slice(&self, symbol_index: usize) -> &[f32] {
        self.take_slice(self.param_indexing[symbol_index])
    }
}

pub struct SymbolStringMut<'a> {
    pub symbols: &'a mut [i32],
    pub param_indexing: &'a mut [JaggedIndexing],
    pub parameters: &'a mut [f32],
}

impl SymbolStringMut<'_> {
    pub fn borrowed(&self) -> SymbolString{
        SymbolString {
            symbols: self.symbols,
            param_indexing: self.param_indexing,
            parameters: self.parameters
        }
    }
}

impl SymbolStringRead for SymbolStringMut<'_>{
    fn param_for(&self, param_index: JaggedIndexing, index_in_param: usize) -> f32 {
        if index_in_param > param_index.length as usize {
            panic!("Index ({}) in param of len ({}) is out of bounds", index_in_param, param_index.length)
        }
        let true_index = param_index.index as usize + index_in_param;
        self.parameters[true_index]
    }

    fn len(&self) -> usize {
        self.symbols.len()
    }

    fn symbol_at(&self, index: usize) -> i32 {
        self.symbols[index]
    }
    fn take_slice(&self, param_index: JaggedIndexing) -> &[f32] {
        let true_index = param_index.index as usize;
        &self.parameters[true_index..true_index + param_index.length as usize]
    }
    fn take_param_slice(&self, symbol_index: usize) -> &[f32] {
        self.take_slice(self.param_indexing[symbol_index])
    }
}
impl SymbolStringWrite for SymbolStringMut<'_>{
    fn set_param_for(&mut self, param_index: JaggedIndexing, index_in_param: usize, new_value: f32) -> () {
        if index_in_param > param_index.length as usize {
            panic!("Index ({}) in param of len ({}) is out of bounds", index_in_param, param_index.length)
        }
        let true_index = param_index.index as usize + index_in_param;
        self.parameters[true_index] = new_value
    }
    fn take_param_slice_mut(&mut self, symbol_index: usize) -> &mut [f32] {
        let param_index = self.param_indexing[symbol_index];
        let true_index = param_index.index as usize;
        & mut self.parameters[true_index..true_index + param_index.length as usize]
    }
}



pub struct DiffusionNode{
    /// assume this is a acyclic tree graph. for L-systems, this will always be true.
    /// will be negative if there is no parent node
    pub parent_node_index: i32,
    
    pub index_in_target: i32,
    pub target_parameters: JaggedIndexing,
    pub index_in_temp_amount_list: i32,

    pub total_resource_types: i32,
    pub diffusion_constant: f32
}

pub struct DiffusionJobOwned {
    pub nodes: Vec<DiffusionNode>,
    pub node_max_capacities: Vec<f32>,
    pub diffusion_global_multiplier: f32,
}

impl DiffusionJobOwned {
    pub fn borrowed(&self) -> DiffusionJob{
        DiffusionJob {
            nodes: &self.nodes,
            node_max_capacities: &self.node_max_capacities,
            diffusion_global_multiplier: self.diffusion_global_multiplier,
        }
    }
}

pub struct DiffusionAmountDataOwned {
    pub node_amount_list_a: Vec<f32>,
    pub node_amount_list_b: Vec<f32>,
    pub latest_in_a: bool,
}

impl DiffusionAmountDataOwned {
    pub fn borrowed_mut(&mut self) -> DiffusionAmountData {
        DiffusionAmountData {
            node_amount_list_a: &mut self.node_amount_list_a,
            node_amount_list_b: &mut self.node_amount_list_b,
            latest_in_a: self.latest_in_a,
        }
    }
}

struct BranchEvent {
    pub _open_branch_symbol_index: i32,
    pub current_node_parent: usize
}

pub fn extract_edges_and_nodes_in_place(
    in_place_symbols: &mut SymbolStringMut,
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
) -> (DiffusionJobOwned, DiffusionAmountDataOwned) {

    let read_borrow = &in_place_symbols.borrowed();
    let graph_estimate = count_nodes_and_params(read_borrow, diffusion_node_symbol);

    
    extract_edges_and_nodes(
        &read_borrow,
        graph_estimate,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        branch_open_symbol,
        branch_close_symbol,
        |_| {},
        |symbol_index, param_indexing| {
            (symbol_index as i32, param_indexing)
        }
    )
}

pub fn extract_edges_and_nodes_in_parallel<'a>(
    source_symbols: &SymbolString,
    target_symbols: &mut SymbolStringMut,
    match_singletons: &[LSystemSingleSymbolMatchData],
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
) -> (DiffusionJobOwned, DiffusionAmountDataOwned) {
    
    let graph_estimate = count_nodes_and_params(source_symbols, diffusion_node_symbol);
    
    extract_edges_and_nodes(
        source_symbols,
        graph_estimate,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        branch_open_symbol,
        branch_close_symbol,
        |symbol_index| {

            let node_singleton = &match_singletons[symbol_index];
            let replacement_index = node_singleton.replacement_symbol_indexing.index as usize;
            target_symbols.param_indexing[replacement_index] = JaggedIndexing {
                index: node_singleton.replacement_parameter_indexing.index,
                length: 0
            };
            target_symbols.symbols[replacement_index] = diffusion_amount_symbol;
        },
        |symbol_index, param_indexing| {
            let node_singleton = &match_singletons[symbol_index];
            (
                node_singleton.replacement_symbol_indexing.index,
                JaggedIndexing{
                    index:node_singleton.replacement_parameter_indexing.index,
                    length: param_indexing.length,
                }
            )
        }
    )
}

struct GraphEstimate {
    pub node_count: usize,
    pub param_count: usize,
}

fn count_nodes_and_params(
    source_symbols: &SymbolString,
    diffusion_node_symbol: i32) -> GraphEstimate {

    let mut total_resource_need = 0 as u32;
    let mut total_nodes = 0 as u32;
    for (symbol, indexing) in source_symbols.symbols.iter().zip(source_symbols.param_indexing){
        let is_node = (*symbol == diffusion_node_symbol) as u32;
        total_resource_need += ((indexing.length - 1) / 2) as u32 * is_node;
        total_nodes += is_node;
    }
    
    GraphEstimate {
        node_count: total_nodes as usize,
        param_count: total_resource_need as usize,
    }
}

fn extract_edges_and_nodes<'a, FDiffusionAmountCaptured, FGetSymbolAndParamIndex>(
    source_symbols: &SymbolString,
    graph_estimate: GraphEstimate,
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
    mut on_diffusion_amount_captured: FDiffusionAmountCaptured,
    get_symbol_and_param_index: FGetSymbolAndParamIndex) -> (DiffusionJobOwned, DiffusionAmountDataOwned) 
where FDiffusionAmountCaptured: FnMut(usize) -> (), FGetSymbolAndParamIndex: Fn(usize, JaggedIndexing) -> (i32, JaggedIndexing){
    
    let mut nodes = Vec::with_capacity(graph_estimate.node_count);
    let mut node_capacities = Vec::with_capacity(graph_estimate.param_count);
    let mut node_amounts = Vec::with_capacity(graph_estimate.param_count);
    
    let mut branch_symbol_parent_stack = VecDeque::with_capacity(5);
    let mut current_node_parent: i32 = -1;

    for symbol_index in 0..source_symbols.len() {
        let symbol: i32 = source_symbols.symbol_at(symbol_index);

        if symbol == diffusion_node_symbol {
            let parent_node_index = current_node_parent;
            current_node_parent = nodes.len() as i32;

            let node_params = source_symbols.param_indexing[symbol_index];
            let params_slice = source_symbols.take_slice(node_params);
            let (symbol_in_target, param_in_target) = get_symbol_and_param_index(symbol_index, node_params);

            let new_node = DiffusionNode {
                parent_node_index,
                
                index_in_target: symbol_in_target,
                target_parameters: param_in_target,
                index_in_temp_amount_list: node_amounts.len() as i32,

                total_resource_types: ((param_in_target.length - 1) / 2) as i32,
                diffusion_constant: params_slice[0],
            };

            for i in 0..new_node.total_resource_types as usize {
                node_amounts   .push(params_slice[i * 2 + 1]);
                node_capacities.push(params_slice[i * 2 + 1 + 1]);
            }
            
            nodes.push(new_node);
            
        } else if symbol == diffusion_amount_symbol {
            let source_slice = source_symbols.take_param_slice(symbol_index);
            if source_slice.len() == 0 {
                continue;
            }

            on_diffusion_amount_captured(symbol_index);

            if current_node_parent < 0 {
                continue;
            }
            let modified_node = &mut nodes[current_node_parent as usize];
            
            let target_start_index = modified_node.index_in_temp_amount_list as usize;
            let target_end_index = target_start_index + modified_node.total_resource_types as usize;
            
            let target_slice = &mut node_amounts[target_start_index..target_end_index];
            for (target_amount, amount_in_source) in target_slice.iter_mut().zip(source_slice) {
                *target_amount += amount_in_source;
            }
        } else if symbol == branch_open_symbol {
            branch_symbol_parent_stack.push_back(BranchEvent {
                _open_branch_symbol_index: symbol_index as i32,
                current_node_parent: current_node_parent as usize,
            });
        } else if symbol == branch_close_symbol {
            if let Some(last_branch_state) = branch_symbol_parent_stack.pop_back() {
                current_node_parent = last_branch_state.current_node_parent as i32;
            }
        }
    }

    let amount_len = node_amounts.len();

    (
        DiffusionJobOwned {
            nodes,
            node_max_capacities: node_capacities,
            diffusion_global_multiplier: 1.0,
        },
        DiffusionAmountDataOwned {
            node_amount_list_a: node_amounts,
            node_amount_list_b: vec![0.0; amount_len],
            latest_in_a: true,
        }
    )
}