use crate::diffusion::diffusion_job::DiffusionJob;
use crate::interop_extern::data::JaggedIndexing;
use crate::interop_extern::diffusion::LSystemSingleSymbolMatchData;

pub struct SymbolString<'a> {
    pub symbols: &'a [i32],
    pub param_indexing: &'a [JaggedIndexing],
    pub parameters: &'a [f32],
}

#[allow(dead_code)]
pub fn extract_edges_and_nodes<'a>(
    _source_symbols: SymbolString,
    _target_symbols: SymbolString,
    _match_singletons: &[LSystemSingleSymbolMatchData]) -> Option<DiffusionJob<'a>> {
    
    
    None
}