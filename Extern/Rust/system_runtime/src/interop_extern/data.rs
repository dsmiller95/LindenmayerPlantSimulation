
#[repr(C)]
#[derive(Copy, Clone)]
pub struct JaggedIndexing {
    pub index: i32,
    pub length: u16,
}

pub trait IndexesIn<T>{
    unsafe fn to_slice(&self, pointer: *const T) -> &[T];
}

impl<T> IndexesIn<T> for JaggedIndexing {
    unsafe fn to_slice(&self, backing_data: *const T) -> &[T] {
        if self.index < 0 {
            // assume less than 0 indexes are invalid.
            //  -1 is the magic value for invalid indexing.
            &[]
        } else {
            let indexed_pointer = backing_data.add(self.index as usize);
            std::slice::from_raw_parts(indexed_pointer, self.length as usize)
        }
    }
}

macro_rules! native_array_interop {
    ($typ:ident, $struct_name:ident) => {
        #[repr(C)]
        pub struct $struct_name{
            pub data: *mut $typ,
            pub len: i32,
        }
        impl $struct_name{
            pub fn to_slice(&self) -> &[$typ]{
                unsafe{
                    std::slice::from_raw_parts(self.data, self.len as usize)
                }
            }
        }
    };
}

native_array_interop!(i32, NativeArrayInteropi32);
native_array_interop!(f32, NativeArrayInteropf32);
native_array_interop!(JaggedIndexing, NativeArrayInteropJaggedIndexing);
