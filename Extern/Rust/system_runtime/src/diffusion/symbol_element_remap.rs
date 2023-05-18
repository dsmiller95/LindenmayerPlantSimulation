use crate::diffusion::extract_graph::{SymbolString, SymbolStringRead};
use crate::interop_extern::data::IndexesIn;

pub struct SymbolElement<'a>{
    pub symbol: i32,
    pub params: &'a [f32],
}

pub fn to_elements<'a>(symbol_string: &'a SymbolString) -> Vec<SymbolElement<'a>>{
    let mut elements = Vec::with_capacity(symbol_string.symbols.len());
    for symbol_index in 0..symbol_string.len(){
        let symbol = symbol_string.symbols[symbol_index];
        let param_indexing = symbol_string.param_indexing[symbol_index];
        let params = param_indexing.to_slice_ref(symbol_string.parameters);
        elements.push(SymbolElement{
            symbol,
            params,
        });
    }
    elements
}