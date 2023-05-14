use crate::JaggedIndexing;



pub trait Indexes<T>{
    unsafe fn index_in(&self, pointer: *const T, index: usize) -> * const T;
}

impl<T> Indexes<T> for JaggedIndexing {
     unsafe fn index_in(&self, pointer: *const T, index: usize) -> * const T {
        pointer.add(self.index as usize + index)
    }
}

