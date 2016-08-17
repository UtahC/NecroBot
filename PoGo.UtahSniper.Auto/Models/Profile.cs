using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POGOProtos.Enums;

namespace PoGo.UtahSniper.Auto.Models
{
    public class Profile
    {
        public Profile(string subpath, string content)
        {
            var args = content.Split(' ');
            this.subpath = subpath;
            for (int i = 0; i < args.Length; i++)
            {
                PokemonId pokeId = PokemonId.Missingno;
                var parseSuccess = Enum.TryParse(args[i], out pokeId);
                if (parseSuccess && pokeId != PokemonId.Missingno)
                    wantedPoke.Add(pokeId);
            }
            lastSnipingStartTime = DateTime.MinValue;
        }

        public string subpath = "";
        public List<PokemonId> wantedPoke = new List<PokemonId>();
        public DateTime lastSnipingStartTime;
        public List<PokemonLocation> LocsVisited = new List<PokemonLocation>();
        public bool isSniping = false;
    }
}
