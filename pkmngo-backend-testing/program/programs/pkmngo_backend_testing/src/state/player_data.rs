use crate::constants::*;
use anchor_lang::prelude::*;
// use rand::Rng;
use crate::globl_funcs::*;

#[account]
pub struct PlayerData {
    pub authority: Pubkey,
    pub name: String,
    pub level: u8,
    pub xp: u64,
    pub wood: u64,
    pub energy: u64,
    pub last_login: i64,
    pub last_id: u16,

    // adding stuff for pokemon game
    pub pokemon_count: u64,

    // pokemon collected by player
    // bit 0-3: pokemon id
    // bit 4-14: level
    // bit 15: is shiny
    pub pokemon_collection: [u16; MAX_POKEMON_COLLECTION as usize],
}

impl PlayerData {
    pub fn print(&mut self) -> Result<()> {
        // Note that logging costs a lot of compute. So don't use it too much.
        msg!(
            "Authority: {} Wood: {} Pokemon: {} Energy: {}",
            self.authority,
            self.wood,
            self.pokemon_count,
            self.energy
        );
        for i in 0..self.pokemon_count as usize {
            msg!("Pokemon {}: {}", i, self.pokemon_collection[i]);
        }
        Ok(())
    }

    pub fn reset_player(&mut self) -> Result<()> {
        self.wood = 0;
        self.energy = MAX_ENERGY;
        self.last_login = Clock::get()?.unix_timestamp;
        self.pokemon_count = 0;

        for i in 0..MAX_POKEMON_COLLECTION {
            self.pokemon_collection[i as usize] = 0;
        }

        msg!("Player reset successfully!");

        Ok(())
    }

    pub fn update_energy(&mut self) -> Result<()> {
        // Get the current timestamp
        let current_timestamp = Clock::get()?.unix_timestamp;

        // Calculate the time passed since the last login
        let mut time_passed: i64 = current_timestamp - self.last_login;

        // Calculate the time spent refilling energy
        let mut time_spent = 0;

        while time_passed >= TIME_TO_REFILL_ENERGY && self.energy < MAX_ENERGY {
            self.energy += 1;
            time_passed -= TIME_TO_REFILL_ENERGY;
            time_spent += TIME_TO_REFILL_ENERGY;
        }

        if self.energy >= MAX_ENERGY {
            self.last_login = current_timestamp;
        } else {
            self.last_login += time_spent;
        }

        Ok(())
    }

    pub fn chop_tree(&mut self, amount: u64) -> Result<()> {
        match self.wood.checked_add(amount) {
            Some(v) => {
                self.wood = v;
            }
            None => {
                msg!("Total wood reached!");
            }
        };
        match self.energy.checked_sub(amount) {
            Some(v) => {
                self.energy = v;
            }
            None => {
                self.energy = 0;
            }
        };
        Ok(())
    }

    pub fn catch_pokemon(&mut self, amount: u64) -> Result<()> {

        // use solana_program::{
        //     sysvar::{clock::Clock, Sysvar},
        //     account_info::AccountInfo,
        // };

        // getting solana clock or smth
        let clock = Clock::get();
        let seed = clock.unwrap().slot as u64;



        // rolling random pokemon and adding to collection
        let poke_id: u16 = random_in_range(seed, 0, 15) as u16;
        let poke_level: u16 = random_in_range(seed, 0, 2047) as u16;
        let poke_shiny_decider = random_in_range(seed, 0, 100);
        let mut poke_shiny: u16 = 0;
        if poke_shiny_decider < 10 {
            poke_shiny = 1;
        }

        // final pokemon entry
        let poke_data: u16 = poke_id | (poke_level << 4) | (poke_shiny << 15);

        if self.pokemon_count >= MAX_POKEMON_COLLECTION {
            msg!("You have reached the maximum number of pokemon you can have!");
            return Ok(());
        }
        self.pokemon_collection[self.pokemon_count as usize] = poke_data;

        match self.pokemon_count.checked_add(amount) {
            Some(v) => {
                self.pokemon_count = v;
            }
            None => {
                msg!("You can never have enough pokemon :)");
            }
        };
        match self.energy.checked_sub(amount) {
            Some(v) => {
                self.energy = v;
            }
            None => {
                self.energy = 0;
            }
        };
        Ok(())
    }
}
