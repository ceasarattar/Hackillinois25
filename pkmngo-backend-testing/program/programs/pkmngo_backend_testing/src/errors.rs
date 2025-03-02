use anchor_lang::error_code;

#[error_code]
pub enum GameErrorCode {
    #[msg("Not enough energy")]
    NotEnoughEnergy,
    #[msg("Wrong Authority")]
    WrongAuthority,
    #[msg("Arithmetic operation failed")]
    ArithmeticError,
    #[msg("Gym boss cannot catch Pokémon")]
    GymBossCannotCatchPokemon,
}
