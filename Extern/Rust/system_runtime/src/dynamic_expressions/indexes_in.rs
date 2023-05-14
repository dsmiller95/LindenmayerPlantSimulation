use crate::JaggedIndexing;

pub trait IndexesIn<T>{
    unsafe fn to_slice(&self, pointer: *const T) -> &[T];
}

impl<T> IndexesIn<T> for JaggedIndexing {
    unsafe fn to_slice(&self, backing_data: *const T) -> &[T] {
        if self.index < 0 {
            // assume less than 0 indexes are invalid.
            //  -1 is the magic value for invalid indexing.
            &[]
        }else {
            let indexed_pointer = backing_data.add(self.index as usize);
            std::slice::from_raw_parts(indexed_pointer, self.length as usize)
        }
    }
}

