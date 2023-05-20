use criterion::{criterion_group, criterion_main, Criterion, black_box, BenchmarkId};
use system_runtime_rustlib::diffusion::symbol_element_remap::{from_elements, SymbolElementOwned, SymbolStringOwned};
use system_runtime_rustlib::interop_extern::data::JaggedIndexing;

use system_runtime_rustlib::interop_extern::diffusion::{LSystemMatchErrorCode, LSystemSingleSymbolMatchData, perform_parallel_diffusion_internal};

fn get_diffuse_node_parameters(diffuse_constant: f32, amount: f32, max: f32, resources: u8) -> Vec<f32> {
    let mut params = vec![diffuse_constant];
    for _ in 0..resources {
        params.push(amount);
        params.push(max);
    }
    params
}

fn benchmark_diffusion_variant(c: &mut Criterion, depth: u8, resources_per_node: u8) {

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
        params: get_diffuse_node_parameters(0.5, 20.0, 1000.0, resources_per_node)
    }];

    for i in 0..depth{
        let next_node = SymbolElementOwned{
            symbol: diffusion_node_symbol,
            params: get_diffuse_node_parameters(0.5, i as f32 * 25.0, 1000.0, resources_per_node)
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
    
    let id = format!("diffusion_{}_deep_{}_resource", depth, resources_per_node);

    let mut group = c.benchmark_group(id);
    for diffuse_steps in [1, 10, 25].iter() {
        group.bench_with_input(BenchmarkId::from_parameter(diffuse_steps), diffuse_steps, |b, &diffuse_steps| {
            b.iter(|| {
                perform_parallel_diffusion_internal(
                    black_box(&source_symbol_string.borrow()),
                    black_box(&mut target_symbol_string.borrow_mut()),
                    black_box(&match_singleton_data),
                    black_box(diffusion_node_symbol),
                    black_box(diffusion_amount_symbol),
                    black_box(open_branch_symbol),
                    black_box(close_branch_symbol),
                    black_box(diffuse_steps),
                    black_box(1.0),
                );
                black_box(target_symbol_string.symbols[0]);
            });
        });
    }
    group.finish();
}

fn criterion_benchmark_diffusion(c: &mut Criterion) {
    benchmark_diffusion_variant(c, 10, 10);
    benchmark_diffusion_variant(c, 4, 10);
    benchmark_diffusion_variant(c, 10, 1);
}

criterion_group!(benches, criterion_benchmark_diffusion);
criterion_main!(benches);