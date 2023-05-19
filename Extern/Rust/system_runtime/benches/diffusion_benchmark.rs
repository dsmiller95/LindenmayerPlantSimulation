use criterion::{criterion_group, criterion_main, Criterion, black_box};
use system_runtime_rustlib::diffusion::symbol_element_remap::{from_elements, SymbolElementOwned, SymbolStringOwned};
use system_runtime_rustlib::interop_extern::data::JaggedIndexing;

use system_runtime_rustlib::interop_extern::diffusion::{LSystemMatchErrorCode, LSystemSingleSymbolMatchData, perform_parallel_diffusion_internal};

fn run_diffusion_at_depth(depth: u8){

    let open_branch_symbol = 0;
    let close_branch_symbol = 1;
    let diffusion_node_symbol = 2;
    let diffusion_amount_symbol = 3;

    // open branch
    let b = SymbolElementOwned {
        symbol: open_branch_symbol,
        params: vec![]
    };
    // close branch
    let d = SymbolElementOwned {
        symbol: close_branch_symbol,
        params: vec![]
    };

    let mut initial_state = vec![SymbolElementOwned {
        symbol: diffusion_node_symbol,
        params: vec![0.5, 20.0, 1000.0]
    }];

    for i in 0..depth{
        let next_node = SymbolElementOwned{
            symbol: diffusion_node_symbol,
            params: vec![0.5, i as f32 * 25.0, 1000.0]
        };
        let mut next_state = vec![next_node, b.clone()];
        next_state.extend(initial_state);
        next_state.push(d.clone());
        initial_state = next_state;
    }

    let source_symbol_string = from_elements(initial_state);

    let mut target_symbol_string = SymbolStringOwned {
        symbols: vec![0; source_symbol_string.symbols.len()],
        param_indexing: vec![JaggedIndexing{
            length: 0,
            index: 0
        }; source_symbol_string.param_indexing.len()],
        parameters: vec![0.0; source_symbol_string.parameters.len()]
    };

    let mut match_singleton_data = Vec::with_capacity(source_symbol_string.symbols.len());
    for i in 0..source_symbol_string.symbols.len(){
        match_singleton_data.push(LSystemSingleSymbolMatchData{
            is_trivial: true,
            replacement_symbol_indexing: JaggedIndexing{
                index: i as i32,
                length: 0,
            },
            replacement_parameter_indexing: JaggedIndexing{
                index: source_symbol_string.param_indexing[i].index,
                length: 0,
            },
            tmp_parameter_memory_space: JaggedIndexing{
                index: 0,
                length: 0,
            },
            matched_rule_index_in_possible: 0,
            selected_replacement_pattern: 0,
            error_code: LSystemMatchErrorCode::None
        });
    }

    perform_parallel_diffusion_internal(
        &source_symbol_string.borrow(),
        black_box(&mut target_symbol_string.borrow_mut()),
        &match_singleton_data,
        diffusion_node_symbol,
        diffusion_amount_symbol,
        open_branch_symbol,
        close_branch_symbol,
        10,
        1.0,
    );
    
}

fn criterion_benchmark_diffusion(c: &mut Criterion) {

    let open_branch_symbol = 0;
    let close_branch_symbol = 1;
    let diffusion_node_symbol = 2;
    let diffusion_amount_symbol = 3;

    // open branch
    let b = SymbolElementOwned {
        symbol: open_branch_symbol,
        params: vec![]
    };
    // close branch
    let d = SymbolElementOwned {
        symbol: close_branch_symbol,
        params: vec![]
    };

    let mut initial_state = vec![SymbolElementOwned {
        symbol: diffusion_node_symbol,
        params: vec![0.5, 20.0, 1000.0]
    }];

    for i in 0..10{
        let next_node = SymbolElementOwned{
            symbol: diffusion_node_symbol,
            params: vec![0.5, i as f32 * 25.0, 1000.0]
        };
        let mut next_state = vec![next_node];
        next_state.push(b.clone());
        next_state.extend(initial_state.clone());
        next_state.push(d.clone());
        next_state.push(b.clone());
        next_state.extend(initial_state);
        next_state.push(d.clone());
        initial_state = next_state;
    }

    let source_symbol_string = from_elements(initial_state);

    let mut target_symbol_string = SymbolStringOwned {
        symbols: vec![0; source_symbol_string.symbols.len()],
        param_indexing: vec![JaggedIndexing{
            length: 0,
            index: 0
        }; source_symbol_string.param_indexing.len()],
        parameters: vec![0.0; source_symbol_string.parameters.len()]
    };

    let mut match_singleton_data = Vec::with_capacity(source_symbol_string.symbols.len());
    for i in 0..source_symbol_string.symbols.len(){
        match_singleton_data.push(LSystemSingleSymbolMatchData{
            is_trivial: true,
            replacement_symbol_indexing: JaggedIndexing{
                index: i as i32,
                length: 0,
            },
            replacement_parameter_indexing: JaggedIndexing{
                index: source_symbol_string.param_indexing[i].index,
                length: 0,
            },
            tmp_parameter_memory_space: JaggedIndexing{
                index: 0,
                length: 0,
            },
            matched_rule_index_in_possible: 0,
            selected_replacement_pattern: 0,
            error_code: LSystemMatchErrorCode::None
        });
    }
    
    c.bench_function("diffuse deep", |b| b.iter(|| {
        perform_parallel_diffusion_internal(
            black_box(&source_symbol_string.borrow()),
            black_box(&mut target_symbol_string.borrow_mut()),
            black_box(&match_singleton_data),
            black_box(diffusion_node_symbol), 
            black_box(diffusion_amount_symbol),
            black_box(open_branch_symbol),
            black_box(close_branch_symbol),
            black_box(10),
            black_box(1.0),
        );
        black_box(target_symbol_string.symbols[0]);
        // run_diffusion_at_depth(10);
        // let b = black_box(23);
        // let d = black_box(0.3);
        // let c = b as f32 + d;
        // c
    }));
}


criterion_group!(benches_two, criterion_benchmark_diffusion);
criterion_main!(benches_two);