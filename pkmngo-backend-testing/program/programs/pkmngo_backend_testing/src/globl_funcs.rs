pub fn generate_pseudo_random(seed: u64) -> u64 {
    const A: u64 = 1664525;
    const C: u64 = 1013904223;
    const M: u64 = 2u64.pow(32);
    
    (A.wrapping_mul(seed).wrapping_add(C)) % M
}

pub fn random_in_range(seed: u64, min: u64, max: u64) -> u64 {
    let random = generate_pseudo_random(seed);
    min + (random % (max - min + 1))
}