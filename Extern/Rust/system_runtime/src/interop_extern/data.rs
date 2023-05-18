#[repr(C)]
#[derive(Copy, Clone, Debug)]
pub struct JaggedIndexing {
    pub index: i32,
    pub length: u16,
}

pub trait IndexesIn<T>{
    unsafe fn to_slice(&self, pointer: *const T) -> &[T];
    fn to_slice_ref<'a>(&self, backing_data: &'a [T]) -> &'a [T];
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
    fn to_slice_ref<'a>(&self, backing_data: &'a [T]) -> &'a [T] {
        if self.index < 0 {
            // assume less than 0 indexes are invalid.
            //  -1 is the magic value for invalid indexing.
            &[]
        } else {
            return backing_data[self.index as usize..(self.index + self.length as i32) as usize].as_ref();
        }
    }
}

macro_rules! native_array_interop {
    ($typ:ident, $struct_name:ident, $struct_name_mut:ident) => {
        #[repr(C)]
        pub struct $struct_name_mut{
            pub data: *mut $typ,
            pub len: i32,
        }
        impl<'a> $struct_name_mut{
            pub fn to_slice(&self) -> &'a mut [$typ]{
                unsafe{
                    std::slice::from_raw_parts_mut(self.data, self.len as usize)
                }
            }
        }
        #[repr(C)]
        pub struct $struct_name{
            pub data: *const $typ,
            pub len: i32,
        }
        impl<'a> $struct_name{
            pub fn to_slice(&self) -> &'a [$typ]{
                unsafe{
                    std::slice::from_raw_parts(self.data, self.len as usize)
                }
            }
        }
    };
}

pub(crate) use native_array_interop;

native_array_interop!(i32, NativeArrayInteropi32, NativeArrayInteropi32Mut);
native_array_interop!(f32, NativeArrayInteropf32, NativeArrayInteropf32Mut);
native_array_interop!(JaggedIndexing, NativeArrayInteropJaggedIndexing, NativeArrayInteropJaggedIndexingMut);
