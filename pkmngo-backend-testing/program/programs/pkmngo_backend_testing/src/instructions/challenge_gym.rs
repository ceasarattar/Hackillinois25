pub use crate::errors::GameErrorCode;
pub use crate::state::game_data::GameData;
use crate::state::player_data::PlayerData;
use anchor_lang::prelude::*;
use session_keys::{Session, SessionToken};

pub fn challenge_gym(mut ctx: Context<ChallengeGym>, counter: u16) -> Result<()> {
    let account: &mut &mut ChallengeGym<'_> = &mut ctx.accounts;

    account.player.last_id = counter;

    msg!("your combat level is {}", account.player.combat_lvl);
    msg!("gym boss power is {}", account.game_data.poke_gym.gym_boss_power);

    // check player power levels and determine if gym is captured
    if account.player.combat_lvl > account.game_data.poke_gym.gym_boss_power {
        // success handler
        // update gym data
        account.game_data.on_gym_captured(account.player.authority, account.player.combat_lvl)?;
    } else {
        // failure handler
        msg!("You lost the battle! Try again when you're stronger!");
    }
    Ok(())
}

#[derive(Accounts, Session)]
#[instruction(level_seed: String)]
pub struct ChallengeGym<'info> {
    #[session(
        // The ephemeral key pair signing the transaction
        signer = signer,
        // The authority of the user account which must have created the session
        authority = player.authority.key()
    )]
    // Session Tokens are passed as optional accounts
    pub session_token: Option<Account<'info, SessionToken>>,

    // There is one PlayerData account
    #[account(
        mut,
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,

    // There can be multiple levels the seed for the level is passed in the instruction
    // First player starting a new level will pay for the account in the current setup
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
    pub system_program: Program<'info, System>,
}
