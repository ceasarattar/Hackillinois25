use anchor_lang::prelude::*;
use crate::constants::*;

#[derive(Default, AnchorSerialize, AnchorDeserialize, Clone, Debug)]
pub struct PokeGym {
    pub gym_name: String,
    pub gym_coords: [u64; 2],
    pub gym_boss: Pubkey,
    pub gym_boss_power: u64,
    pub gym_payable: bool,
}

#[account]
pub struct GameData {
    pub total_wood_collected: u64,
    pub total_pokemon_in_world: u64,

    // also add a poke_gym struct here
    pub poke_gym: PokeGym,
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

    pub fn on_gym_captured(&mut self, new_boss: Pubkey, new_power: u64) -> Result<()> {
        self.poke_gym.gym_boss = new_boss;
        self.poke_gym.gym_boss_power = new_power;
        self.poke_gym.gym_payable = true;

        msg!("Gym successfully captured by {}", new_boss);

        Ok(())
    }
}
