use crate::diffusion::extract_graph::{DiffusionNode};

#[derive(Copy, Clone)]
pub struct DiffusionJob<'a> {
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
    
    pub fn get_latest_data(&self) -> &[f32] {
        if self.latest_in_a {
            self.node_amount_list_a
        }else {
            self.node_amount_list_b
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

            for node in self.nodes {
                self.diffuse_across_edge(node, source_amounts, target_amounts);
            }
        }
        false
    }

    fn diffuse_across_edge(self, node: &DiffusionNode, source_amounts: &[f32], target_amounts: &mut [f32]) {
        if node.parent_node_index < 0 {
            return;
        }
        let node_a = node;
        let node_b = &self.nodes[node.parent_node_index as usize];

        let diffusion_constant =
            self.diffusion_global_multiplier *
                (node_a.diffusion_constant + node_b.diffusion_constant) / 2.0;

        let blended_resource_num = node_a.total_resource_types.min(node_b.total_resource_types);
        
        let node_a_temp_amt_index = node_a.index_in_temp_amount_list as usize;
        let node_b_temp_amt_index = node_b.index_in_temp_amount_list as usize;
        
        for resource in 0..blended_resource_num as usize {
            let node_a_resource_index = node_a_temp_amt_index + resource;
            let node_b_resource_index = node_b_temp_amt_index + resource;
            
            let old_node_a_value = source_amounts[node_a_resource_index];
            let old_node_b_value = source_amounts[node_b_resource_index];
            
            let a_to_b_transferred_amount = diffusion_constant * (old_node_b_value - old_node_a_value);
            let is_towards_b = a_to_b_transferred_amount < 0.0;
            
            let node_b_value_cap = self.node_max_capacities[node_b_resource_index];
            if is_towards_b && old_node_b_value >= node_b_value_cap {
                // the direction of flow is towards node B, and also node B is above its value cap. skip updating the resource on this connection completely.
                continue;
            }
            let node_a_value_cap = self.node_max_capacities[node_a_resource_index];
            if !is_towards_b && old_node_a_value >= node_a_value_cap {
                // the direction of flow is towards node A, and also node A is above its value cap. skip updating the resource on this connection completely.
                continue;
            }
            
            target_amounts[node_a_resource_index] += a_to_b_transferred_amount;
            target_amounts[node_b_resource_index] -= a_to_b_transferred_amount;
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
