
fn main() {
    csbindgen::Builder::default()
        .input_extern_file("expanded.rs.tmp")
        .csharp_dll_name("system_runtime_rustlib")
        .csharp_class_name("SystemRuntimeRust")
        .csharp_class_accessibility("public")
        .csharp_namespace("Dman.LSystem.Extern")
        .generate_csharp_file("target/dotnet/SystemRuntimeRust.g.cs")
        .unwrap();
}