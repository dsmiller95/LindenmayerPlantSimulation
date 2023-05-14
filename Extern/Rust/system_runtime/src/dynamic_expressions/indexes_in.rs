use crate::JaggedIndexing;

pub trait IndexesIn<T>{
    unsafe fn index_in(&self, pointer: *const T, index: usize) -> * const T;
    unsafe fn to_slice(&self, pointer: *const T) -> &[T];
}

impl<T> IndexesIn<T> for JaggedIndexing {
     unsafe fn index_in(&self, pointer: *const T, index: usize) -> * const T {
        pointer.add(self.index as usize + index)
    }
    
    unsafe fn to_slice(&self, backing_data: *const T) -> &[T] {
        std::slice::from_raw_parts(backing_data.add(self.index as usize), self.length as usize)
    }
}

