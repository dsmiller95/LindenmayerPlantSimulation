use std::collections::vec_deque::VecDeque;
use std::io::SeekFrom::End;
use crate::diffusion::diffusion_job::{DiffusionAmountData, DiffusionJob};
use crate::interop_extern::data::JaggedIndexing;
use crate::interop_extern::diffusion::{DiffusionEdge, DiffusionNode, LSystemSingleSymbolMatchData};

pub trait SymbolStringRead {
    fn param_for(&self, param_index: JaggedIndexing, index_in_param: usize) -> f32;
    fn len(&self) -> usize;
    fn symbol_at(&self, index: usize) -> i32;
}

pub trait SymbolStringWrite {
    fn set_param_for(&mut self, param_index: JaggedIndexing, index_in_param: usize, new_value: f32) -> ();
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
}

pub struct SymbolStringMut<'a> {
    pub symbols: &'a mut [i32],
    pub param_indexing: &'a mut [JaggedIndexing],
    pub parameters: &'a mut [f32],
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
}
impl SymbolStringWrite for SymbolStringMut<'_>{
    fn set_param_for(&mut self, param_index: JaggedIndexing, index_in_param: usize, new_value: f32) -> () {
        if index_in_param > param_index.length as usize {
            panic!("Index ({}) in param of len ({}) is out of bounds", index_in_param, param_index.length)
        }
        let true_index = param_index.index as usize + index_in_param;
        self.parameters[true_index] = new_value
    }
}

pub struct DiffusionJobOwned {
    pub edges: Vec<DiffusionEdge>,
    pub nodes: Vec<DiffusionNode>,
    pub node_max_capacities: Vec<f32>,
    pub diffusion_global_multiplier: f32,
}

impl DiffusionJobOwned {
    pub fn borrowed(&self) -> DiffusionJob{
        DiffusionJob {
            edges: &self.edges,
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
    pub current_node_parent: i32 
}

pub fn extract_edges_and_nodes<'a>(
    source_symbols: &SymbolString,
    target_symbols: &mut SymbolStringMut,
    match_singletons: &[LSystemSingleSymbolMatchData],
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32) -> (DiffusionJobOwned, DiffusionAmountDataOwned) {
    
    let mut edges = Vec::new();
    let mut nodes = Vec::new();
    let mut node_capacities = Vec::new();
    let mut node_amounts = Vec::new();
    
    let mut branch_symbol_parent_stack = VecDeque::with_capacity(5);
    let mut current_node_parent = -1;

    for symbol_index in 0..source_symbols.len() {
        let symbol: i32 = source_symbols.symbol_at(symbol_index);

        if symbol == diffusion_node_symbol {
            if current_node_parent >= 0 {
                let new_edge = DiffusionEdge {
                    node_a_index: current_node_parent,
                    node_b_index: nodes.len() as i32,
                };
                edges.push(new_edge);
            }
            current_node_parent = nodes.len() as i32;

            let node_params = source_symbols.param_indexing[symbol_index];
            let node_singleton = &match_singletons[symbol_index];

            let new_node = DiffusionNode {
                index_in_target: node_singleton.replacement_symbol_indexing.index,
                target_parameters: JaggedIndexing {
                    index: node_singleton.replacement_parameter_indexing.index,
                    length: node_params.length
                },
                index_in_temp_amount_list: node_amounts.len() as i32,

                total_resource_types: ((node_params.length - 1) / 2) as i32,
                diffusion_constant: source_symbols.param_for(node_params, 0),
            };

            node_amounts.reserve(new_node.total_resource_types as usize);
            node_capacities.reserve(new_node.total_resource_types as usize);
            for resource_type in 0..new_node.total_resource_types {
                let current_amount =
                    source_symbols.param_for(node_params, (resource_type * 2 + 1) as usize);
                let max_capacity =
                    source_symbols.param_for(node_params, (resource_type * 2 + 1 + 1) as usize);
                node_amounts.push(current_amount);
                node_capacities.push(max_capacity);
            }
            nodes.push(new_node);
            
        } else if symbol == diffusion_amount_symbol {
            if current_node_parent < 0 {
                continue;
            }

            let modified_node = &mut nodes[current_node_parent as usize];
            let amount_parameters = source_symbols.param_indexing[symbol_index];
            if amount_parameters.length == 0 {
                continue;
            }

            let node_singleton = &match_singletons[symbol_index];
            target_symbols.param_indexing[node_singleton.replacement_symbol_indexing.index as usize] =
                JaggedIndexing {
                    index: node_singleton.replacement_parameter_indexing.index,
                    length: 0,
                };
            target_symbols.symbols[node_singleton.replacement_symbol_indexing.index as usize] =
                diffusion_amount_symbol;

            if current_node_parent < 0 {
                continue;
            }
            
            let resource_number = modified_node.total_resource_types.min(amount_parameters.length as i32) as usize;
            let start_index = modified_node.index_in_temp_amount_list as usize;
            let end_index = start_index + resource_number;
            
            for (resource_type, resource_amount) in node_amounts[start_index..end_index].iter_mut().enumerate() {
                *resource_amount += source_symbols.param_for(amount_parameters, resource_type);
            }
        } else if symbol == branch_open_symbol {
            branch_symbol_parent_stack.push_back(BranchEvent {
                _open_branch_symbol_index: symbol_index as i32,
                current_node_parent,
            });
        } else if symbol == branch_close_symbol {
            if let Some(last_branch_state) = branch_symbol_parent_stack.pop_back() {
                current_node_parent = last_branch_state.current_node_parent;
            }
        }
    }

    let amount_len = node_amounts.len();

    (
        DiffusionJobOwned {
            edges,
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