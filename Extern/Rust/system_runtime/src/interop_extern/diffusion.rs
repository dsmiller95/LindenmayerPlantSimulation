use crate::diffusion::apply_results::apply_diffusion_results;
use crate::diffusion::diffusion_job::{DiffusionAmountData, DiffusionJob};
use crate::diffusion::extract_graph::{extract_edges_and_nodes_in_parallel, extract_edges_and_nodes_in_place, SymbolString, SymbolStringMut};
use crate::interop_extern::data::{JaggedIndexing, native_array_interop, NativeArrayInteropf32, NativeArrayInteropf32Mut, NativeArrayInteropi32, NativeArrayInteropi32Mut, NativeArrayInteropJaggedIndexing, NativeArrayInteropJaggedIndexingMut};

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
    pub parameters: NativeArrayInteropf32
}

impl<'a> SymbolStringInterop {
    pub fn to_symbol_str(&self) -> SymbolString<'a>{
        let (
            symbols,
            param_indexing,
            parameters) = (
                self.symbols.to_slice(),
                self.parameter_indexing.to_slice(),
                self.parameters.to_slice(),
        );
        
        SymbolString {
            symbols,
            param_indexing,
            parameters
        }
    }
}
#[repr(C)]
pub struct SymbolStringInteropMut{
    pub symbols: NativeArrayInteropi32Mut,
    pub parameter_indexing: NativeArrayInteropJaggedIndexingMut,
    pub parameters: NativeArrayInteropf32Mut
}
impl<'a> SymbolStringInteropMut {
    pub fn to_symbol_str(&self) -> SymbolStringMut<'a>{
        let (
            symbols,
            param_indexing,
            parameters) = (
            self.symbols.to_slice(),
            self.parameter_indexing.to_slice(),
            self.parameters.to_slice(),
        );

        SymbolStringMut {
            symbols,
            param_indexing,
            parameters
        }
    }
}

#[repr(C)]
#[derive(Debug, Clone)]
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

native_array_interop!(LSystemSingleSymbolMatchData, NativeArrayInteropLSystemSingleSymbolMatchData, NativeArrayInteropLSystemSingleSymbolMatchDataMut);

#[repr(u8)]
#[derive(Debug, Clone)]
pub enum LSystemMatchErrorCode
{
    None = 0,
    TooManyParameters = 1,
    TrivialSymbolNotIndicatedAtMatchTime = 2,
    TrivialSymbolNotIndicatedAtReplacementTime = 3
}

#[no_mangle]
pub extern "C" fn perform_parallel_diffusion(
    source_data: *mut SymbolStringInterop,
    target_data: *mut SymbolStringInteropMut,
    match_singleton_data: *mut NativeArrayInteropLSystemSingleSymbolMatchData,
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
    diffusion_steps: i32,
    diffusion_global_multiplier: f32,
) -> bool{

    let (
        source_data_safe,
        mut target_data_safe,
        match_singleton_data_safe) =
    unsafe {(
        source_data.as_ref().unwrap().to_symbol_str(),
        target_data.as_ref().unwrap().to_symbol_str(),
        match_singleton_data.as_ref().unwrap().to_slice()
        )};
    
    // TODO: doing this remap costs a lot, likely in alloc time.
    //  its more ergonomic, but not ready for doing this conversion yet.
    //let source_elements = to_elements(&source_data_safe);
    
    perform_parallel_diffusion_internal(
        &source_data_safe,
        &mut target_data_safe,
        &match_singleton_data_safe,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        branch_open_symbol,
        branch_close_symbol,
        diffusion_steps,
        diffusion_global_multiplier,
    )
}

pub fn perform_parallel_diffusion_internal(
    source_data: &SymbolString,
    target_data: &mut SymbolStringMut,
    match_singleton_data: &[LSystemSingleSymbolMatchData],
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
    diffusion_steps: i32,
    diffusion_global_multiplier: f32,
) -> bool {

    let (mut diffusion_config, mut diffusion_amounts) = extract_edges_and_nodes_in_parallel(
        source_data,
        target_data,
        match_singleton_data,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        branch_open_symbol,
        branch_close_symbol
    );
    diffusion_config.diffusion_global_multiplier = diffusion_global_multiplier;
    let diffuse_job_ref = diffusion_config.borrowed();
    let mut_diffuse_amount_data = &mut diffusion_amounts.borrowed_mut();

    diffuse_job_ref.diffuse_between(
        mut_diffuse_amount_data,
        diffusion_steps);

    apply_diffusion_results(
        diffuse_job_ref,
        mut_diffuse_amount_data,
        target_data,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        false);
    true
}


#[no_mangle]
pub extern "C" fn perform_in_place_diffusion(
    source_data: *mut SymbolStringInteropMut,
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
    diffusion_steps: i32,
    diffusion_global_multiplier: f32,
) -> bool{

    let (
        mut source_data_safe,) =
        unsafe {(
            source_data.as_ref().unwrap().to_symbol_str(),
        )};

    // TODO: doing this remap costs a lot, likely in alloc time.
    //  its more ergonomic, but not ready for doing this conversion yet.
    //let source_elements = to_elements(&source_data_safe);

    perform_in_place_diffusion_internal(
        &mut source_data_safe,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        branch_open_symbol,
        branch_close_symbol,
        diffusion_steps,
        diffusion_global_multiplier,
    )
}

pub fn perform_in_place_diffusion_internal(
    source_data: &mut SymbolStringMut,
    diffusion_node_symbol: i32,
    diffusion_amount_symbol: i32,
    branch_open_symbol: i32,
    branch_close_symbol: i32,
    diffusion_steps: i32,
    diffusion_global_multiplier: f32,
) -> bool {

    let (mut diffusion_config, mut diffusion_amounts) = extract_edges_and_nodes_in_place(
        source_data,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        branch_open_symbol,
        branch_close_symbol
    );
    diffusion_config.diffusion_global_multiplier = diffusion_global_multiplier;
    let diffuse_job_ref = diffusion_config.borrowed();
    let mut_diffuse_amount_data = &mut diffusion_amounts.borrowed_mut();

    diffuse_job_ref.diffuse_between(
        mut_diffuse_amount_data,
        diffusion_steps);

    apply_diffusion_results(
        diffuse_job_ref,
        mut_diffuse_amount_data,
        source_data,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        true);
    true
}