
#[no_mangle]
pub extern "C" fn double_input(input: i32) -> i32 {
    input * 2
}

mod test_mod {

    #[no_mangle]
    pub extern "C" fn triple_input(input: i32) -> i32
    {
        input * 3
    }
}