use anchor_lang::prelude::*;

use crate::constants::MAX_WOOD_PER_TREE;
use crate::constants::MAX_POKEMON_IN_WORLD;

#[account]
pub struct GameData {
    pub total_wood_collected: u64,
    pub total_pokemon_in_world: u64,
}

impl GameData {
    pub fn on_tree_chopped(&mut self, amount_chopped: u64) -> Result<()> {
        match self.total_wood_collected.checked_add(amount_chopped) {
            Some(v) => {
                if self.total_wood_collected >= MAX_WOOD_PER_TREE {
                    self.total_wood_collected = 0;
                    msg!("Tree successfully chopped. New Tree coming up.");
                } else {
                    self.total_wood_collected = v;
                    msg!("Total wood chopped: {}", v);
                }
            }
            None => {
                msg!("The ever tree is completly chopped!");
            }
        };

        Ok(())
    }

    pub fn on_pokemon_caught(&mut self, amount_caught: u64) -> Result<()> {
        match self.total_pokemon_in_world.checked_sub(amount_caught) {
            Some(v) => {
                if self.total_pokemon_in_world == 0 {
                    self.total_pokemon_in_world = MAX_POKEMON_IN_WORLD;
                    msg!("All your pokemon rioted and escaped! youll have to catch em
                        again lmao");
                } else {
                    self.total_pokemon_in_world = v;
                    msg!("Total pokemon caught: {}", amount_caught);
                }
            }
            None => {       // in case of int overflow
                msg!("You sure caught a lot of pokemon!");
            }
        };

        msg!("Total pokemon in world: {}", self.total_pokemon_in_world);

        Ok(())
    }
}
