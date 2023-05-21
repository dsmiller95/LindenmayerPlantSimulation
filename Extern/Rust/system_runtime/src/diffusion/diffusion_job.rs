use crate::interop_extern::diffusion::{DiffusionEdge, DiffusionNode};

#[derive(Copy, Clone)]
pub struct DiffusionJob<'a> {
    pub edges: &'a [DiffusionEdge],
    pub nodes: &'a [DiffusionNode],
    pub node_max_capacities: &'a [f32],
    pub diffusion_global_multiplier: f32,
}

pub struct DiffusionAmountData<'a> {
    pub node_amount_list_a: &'a mut [f32],
    pub node_amount_list_b: &'a mut [f32],
    pub latest_in_a: bool,
    
}

impl DiffusionAmountData<'_> {
    pub fn take_buffer_pair(&mut self) -> (&[f32], &mut [f32]) {
        if self.latest_in_a {
            self.latest_in_a = false;
            (self.node_amount_list_a, self.node_amount_list_b)
        }else {
            self.latest_in_a = true;
            (self.node_amount_list_b, self.node_amount_list_a)
        }
    }
}

impl DiffusionJob<'_> {
    pub fn diffuse_between(
        self,
        double_buffered_data: &mut DiffusionAmountData,
        diffuse_steps: i32) -> bool {
        for _ in 0..diffuse_steps
        {
            let (source_amounts, target_amounts) = double_buffered_data.take_buffer_pair();

            (*target_amounts).copy_from_slice(source_amounts);

            for edge in self.edges {
                self.diffuse_across_edge(edge, source_amounts, target_amounts);
            }
        }
        false
    }

    fn diffuse_across_edge(self, edge: &DiffusionEdge, source_amounts: &[f32], target_amounts: &mut [f32]) {
        let node_a = &self.nodes[edge.node_a_index as usize];
        let node_b = &self.nodes[edge.node_b_index as usize];


        let diffusion_constant =
            self.diffusion_global_multiplier *
                (node_a.diffusion_constant + node_b.diffusion_constant) / 2.0;

        let blended_resource_num = node_a.total_resource_types.min(node_b.total_resource_types);
        
        // let source_slice_a = node_a.get_resource_slice(source_amounts);
        // let source_slice_b = node_b.get_resource_slice(source_amounts);
        // 
        // let capacities_slice_a = node_a.get_resource_slice(self.node_max_capacities);
        // let capacities_slice_b = node_b.get_resource_slice(self.node_max_capacities);
        // 
        // let blended_resource_num = source_slice_a.len().min(source_slice_b.len());
        
        for resource in 0..blended_resource_num as usize {
            // let old_node_a_value = source_slice_a[resource];
            // let old_node_b_value = source_slice_b[resource];
            // let node_a_value_cap = capacities_slice_a[resource];
            // let node_b_value_cap = capacities_slice_b[resource];

            let old_node_a_value = source_amounts[node_a.index_in_temp_amount_list as usize + resource];
            let node_a_value_cap = self.node_max_capacities[node_a.index_in_temp_amount_list as usize + resource];

            let old_node_b_value = source_amounts[node_b.index_in_temp_amount_list as usize + resource];
            let node_b_value_cap = self.node_max_capacities[node_b.index_in_temp_amount_list as usize + resource];
            
            let a_to_b_transferred_amount = diffusion_constant * (old_node_b_value - old_node_a_value);
        
            if a_to_b_transferred_amount == 0.0 {
                continue;
            }
            if a_to_b_transferred_amount < 0.0 && old_node_b_value >= node_b_value_cap {
                // the direction of flow is towards node B, and also node B is above its value cap. skip updating the resource on this connection completely.
                continue;
            }
            if a_to_b_transferred_amount > 0.0 && old_node_a_value >= node_a_value_cap {
                // the direction of flow is towards node A, and also node A is above its value cap. skip updating the resource on this connection completely.
                continue;
            }

            target_amounts[node_a.index_in_temp_amount_list as usize + resource] += a_to_b_transferred_amount;
            target_amounts[node_b.index_in_temp_amount_list as usize + resource] -= a_to_b_transferred_amount;
        }
    }
}

impl DiffusionNode {
    pub fn get_resource_slice<'a>(&'a self, data: &'a [f32]) -> &[f32] {
        let index_in_list = self.index_in_temp_amount_list as usize;
        &data[index_in_list..index_in_list + self.total_resource_types as usize]
    }
    pub fn get_resource_slice_mut<'a>(&'a self, data: &'a mut [f32]) -> &mut [f32] {
        let index_in_list = self.index_in_temp_amount_list as usize;
        &mut data[index_in_list..index_in_list + self.total_resource_types as usize]
    }
}

fn _get_exclusive_slices<'a>(node_a: &DiffusionNode, node_b: &DiffusionNode, data: &'a mut [f32]) -> (&'a mut [f32], &'a mut [f32]) {
    let a_range = node_a.index_in_temp_amount_list as usize..node_a.index_in_temp_amount_list as usize + node_a.total_resource_types as usize; 
    let b_range = node_b.index_in_temp_amount_list as usize..node_b.index_in_temp_amount_list as usize + node_b.total_resource_types as usize;
    
    // we assume no overlapping ranges
    let a_is_lower = a_range.end < b_range.end;
    let (lower, higher) = if a_is_lower { (a_range, b_range) } else { (b_range, a_range) };
    let (lower_slice, higher_slice) = data.split_at_mut(lower.end);

    let trunc_higher = &mut higher_slice[higher.start - lower.end..higher.end - lower.end];
    let trunc_lower = &mut lower_slice[lower];
    
    
    if a_is_lower {
        (trunc_lower, trunc_higher)
    }else {
        (trunc_higher, trunc_lower)
    }
}
