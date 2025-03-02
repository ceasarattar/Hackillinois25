pub use crate::errors::GameErrorCode;
pub use crate::state::game_data::GameData;
use crate::state::player_data::PlayerData;
use anchor_lang::prelude::*;
use session_keys::{Session, SessionToken};

pub fn reset_game(mut ctx: Context<ResetGame>) -> Result<()> {
    ctx.accounts.game_data.reset_game()?;
    Ok(())
}

#[derive(Accounts, Session)]
#[instruction(level_seed: String)]
pub struct ResetGame<'info> {
    
    #[session(
        // The ephemeral key pair signing the transaction
        signer = signer,
        // The authority of the user account which must have created the session
        authority = player.authority.key()
    )]
    // Session Tokens are passed as optional accounts
    pub session_token: Option<Account<'info, SessionToken>>,

    #[account(
        mut,
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Box<Account<'info, PlayerData>>,

    #[account(mut)]
    pub game_data: Account<'info, GameData>,

    #[account(mut)]
    pub signer: Signer<'info>,
}