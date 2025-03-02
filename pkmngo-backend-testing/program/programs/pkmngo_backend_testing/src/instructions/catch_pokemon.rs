pub use crate::errors::GameErrorCode;
pub use crate::state::game_data::GameData;
use crate::state::player_data::PlayerData;
use anchor_lang::prelude::*;
use session_keys::{Session, SessionToken};

pub fn catch_pokemon(mut ctx: Context<CatchPokemon>, counter: u16, amount: u64) -> Result<()> {
    let account: &mut &mut CatchPokemon<'_> = &mut ctx.accounts;

    // Check if the player is the gym boss and the gym is active
    if account.game_data.poke_gym.gym_payable && account.signer.key() == account.game_data.poke_gym.gym_boss {
        return err!(GameErrorCode::GymBossCannotCatchPokemon);
    }

    // Existing logic continues...
    account.player.update_energy()?;
    account.player.print()?;

    if account.player.energy < amount {
        return err!(GameErrorCode::NotEnoughEnergy);
    }

    // Pay fee to gym boss if gym is payable and player is not the gym boss
    if account.game_data.poke_gym.gym_payable && account.signer.key() != account.game_data.poke_gym.gym_boss {
        let fee_per_pokemon = 10000; // 10000 lamports = 0.00001 SOL
        let total_fee = amount.checked_mul(fee_per_pokemon).ok_or(GameErrorCode::ArithmeticError)?;
        anchor_lang::system_program::transfer(
            CpiContext::new(
                account.system_program.to_account_info(),
                anchor_lang::system_program::Transfer {
                    from: account.signer.to_account_info(),
                    to: account.gym_boss_account.to_account_info(),
                },
            ),
            total_fee,
        )?;
    }

    account.player.last_id = counter;
    account.player.catch_pokemon(1)?;       // replacing amount with 1 here, repurposing amount
    account.game_data.on_pokemon_caught(1)?;    // for amount of energy (pokeballs)

    msg!(
        "You caught a pokemon! You have {} pokemon and {} energy left.",
        account.player.pokemon_count,
        account.player.energy
    );

    Ok(())
}

#[derive(Accounts, Session)]
#[instruction(level_seed: String)]
pub struct CatchPokemon<'info> {
    #[session(
        signer = signer,
        authority = player.authority.key()
    )]
    pub session_token: Option<Account<'info, SessionToken>>,

    #[account(
        mut,
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,

    #[account(
        init_if_needed,
        payer = signer,
        space = 1000,
        seeds = [level_seed.as_ref()],
        bump,
    )]
    pub game_data: Account<'info, GameData>,

    #[account(mut)]
    pub signer: Signer<'info>,

    // Add the gym boss's account
    ///CHECK: Add the gym boss's account (buzz off warning pls)
    #[account(mut, address = game_data.poke_gym.gym_boss)]
    pub gym_boss_account: AccountInfo<'info>,

    pub system_program: Program<'info, System>,
}
