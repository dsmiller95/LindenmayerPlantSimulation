use crate::diffusion::extract_graph::{SymbolString, SymbolStringMut, SymbolStringRead};
use crate::interop_extern::data::{IndexesIn, JaggedIndexing};

pub struct SymbolElement<'a>{
    pub symbol: i32,
    pub params: &'a [f32],
}

#[derive(Clone)]
pub struct SymbolElementOwned{
    pub symbol: i32,
    pub params: Vec<f32>,
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

pub struct SymbolStringOwned {
    pub symbols: Vec<i32>,
    pub param_indexing: Vec<JaggedIndexing>,
    pub parameters: Vec<f32>,
}

impl SymbolStringOwned {
    pub fn borrow_mut(&mut self) -> SymbolStringMut {
        SymbolStringMut{
            symbols: &mut self.symbols,
            param_indexing: &mut self.param_indexing,
            parameters: &mut self.parameters,
        }
    }
    
    pub fn borrow(&self) -> SymbolString {
        SymbolString{
            symbols: &self.symbols,
            param_indexing: &self.param_indexing,
            parameters: &self.parameters,
        }
    }
}


pub fn from_elements(elements: Vec<SymbolElementOwned>) -> SymbolStringOwned {

    let mut symbols = Vec::with_capacity(elements.len());
    let mut param_indexing = Vec::with_capacity(elements.len());

    // this is an estimate, not guaranteed to be this size
    let mut parameters_packed: Vec<f32> = Vec::with_capacity(elements.len());

    for element in elements.iter() {
        symbols.push(element.symbol);
 
        param_indexing.push(
            JaggedIndexing{
                index: parameters_packed.len() as i32,
                length: element.params.len() as u16
            }
        );
        
        parameters_packed.extend(element.params.iter());
    }

    SymbolStringOwned {
        symbols,
        param_indexing,
        parameters: parameters_packed,
    }
}