using System;
using System.Linq;

namespace RustDeriveDemo
{
    /// <summary>
    /// Core logic for analyzing Rust structs with respect to inlineability.
    /// </summary>
    public static class DeriveEngine
    {
        /// <summary>
        /// Determines whether a Rust struct can be safely inlined.
        /// Inlineability is defined as the presence of the Copy trait,
        /// or a combination of Clone + all fields also being trivially copyable.
        /// For this demo, we treat primitive types as trivially copyable.
        /// </summary>
        public static bool IsInlineable(RustStruct rustStruct)
        {
            if (rustStruct == null) throw new ArgumentNullException(nameof(rustStruct));

            // If the struct explicitly derives Copy, it is inlineable.
            if (rustStruct.Derives.HasFlag(DeriveTrait.Copy))
                return true;

            // If it derives Clone, we need to ensure all fields are primitive.
            if (rustStruct.Derives.HasFlag(DeriveTrait.Clone))
                return rustStruct.Fields.All(IsPrimitiveType);

            // Otherwise, not inlineable.
            return false;
        }

        private static bool IsPrimitiveType(RustField field)
        {
            // Simplified primitive detection.
            // In a real scenario, this would be far more exhaustive.
            string t = field.Type.Trim().ToLowerInvariant();
            return t switch
            {
                "u8" => true,
                "i8" => true,
                "u16" => true,
                "i16" => true,
                "u32" => true,
                "i32" => true,
                "u64" => true,
                "i64" => true,
                "usize" => true,
                "isize" => true,
                "bool" => true,
                "char" => true,
                "f32" => true,
                "f64" => true,
                _ => false,
            };
        }
    }
}
