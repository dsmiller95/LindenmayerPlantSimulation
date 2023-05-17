use crate::diffusion::diffusion_job::{DiffusionAmountData, DiffusionJob};
use crate::interop_extern::data::{JaggedIndexing, NativeArrayInteropf32, NativeArrayInteropi32, NativeArrayInteropJaggedIndexing};

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

    let diffusion_job = DiffusionJob {
        edges,
        nodes,
        node_max_capacities: node_capacities,

        diffusion_global_multiplier,
    };

    let double_buffered_amounts = & mut DiffusionAmountData{
        node_amount_list_a: node_amounts_slice_a,
        node_amount_list_b: node_amounts_slice_b,
        latest_in_a: true,
    };

    diffusion_job.diffuse_between(
        double_buffered_amounts,
        diffusion_steps);

    double_buffered_amounts.latest_in_a
}

#[repr(C)]
pub struct SymbolStringInterop{
    pub symbols: NativeArrayInteropi32,
    pub parameter_indexing: NativeArrayInteropJaggedIndexing,
    pub parameters: NativeArrayInteropf32,
}

#[repr(C)]
pub struct LSystemSingleSymbolMatchData{
    ////// #1 memory allocation step //////

    /// <summary>
    /// Indexing inside the captured parameter memory
    /// Step #2 will modify the index and true size based on the specific match which is selected
    /// </summary>
    pub tmp_parameter_memory_space: JaggedIndexing,

    /// <summary>
    /// Set to true if there are no rules which apply to this symbol.
    ///     Used to save time when batching over this symbol later on
    /// Can be set to true at several points through the batching process, at any point where
    ///     it is determined that there remain no rules which match this symbol
    /// </summary>
    pub is_trivial: bool,

    ////// #2 finding all potential match step //////

    /// <summary>
    /// the index of the rule inside the structured L-system rule structure,
    ///     after indexing by the symbol.
    ///     populated by step #3, and used to represent the index of the rule selected as the True Match
    /// </summary>
    pub matched_rule_index_in_possible: u8,
    ////// #3 selecting specific match step //////

    /// <summary>
    /// the ID of the stochastically selected replacement pattern. Populated by step #3, after
    ///     the single matched rule is identified
    /// </summary>
    pub selected_replacement_pattern: u8,
    /// <summary>
    /// the memory space reserved for replacement symbols
    ///     length is Populated by step #3 based on the specific rule selected.
    ///     index is populated by step #4, ensuring enough space for all replacement symbols
    /// </summary>
    pub replacement_symbol_indexing: JaggedIndexing,
    /// <summary>
    /// the memory space reserved for replacement parameters
    ///     length is Populated by step #3 based on the specific rule selected.
    ///     index is populated by step #4, ensuring enough space for all replacement parameters
    /// </summary>
    pub replacement_parameter_indexing: JaggedIndexing,
    pub error_code: LSystemMatchErrorCode,
}

#[repr(u8)]
pub enum LSystemMatchErrorCode
{
    None = 0,
    TooManyParameters = 1,
    TrivialSymbolNotIndicatedAtMatchTime = 2,
    TrivialSymbolNotIndicatedAtReplacementTime = 3
}

pub extern "C" fn perform_parallel_diffusion(
    _source_data: *mut SymbolStringInterop,
    _target_data: *mut SymbolStringInterop,
    _match_singleton_data: *mut LSystemSingleSymbolMatchData,
    _match_singleton_data_len: i32,
) -> bool{

    //let sum = add!(1, 2);

    false
}