using POGOProtos.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGo.NecroBot.Logic.Extentions
{
    public static class PokemonDataExtensions
    {
        public static double GetIV(this PokemonData pokemon)
        {
            return (pokemon.IndividualAttack + pokemon.IndividualDefense + pokemon.IndividualStamina) / 45.0;
        }
    }
}
