using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;

namespace Service.Api.Integrations
{
    public interface IPokemonApi
    {
        [Get("/gender/{id}")]
        [Headers("Accept: application/json", "Accept-Encoding: gzip,deflate")]
        Task<ApiResponse<Genders>> Get(string id);
    }


public class PokemonSpecies
{
    public string name { get; set; }
    public string url { get; set; }
}

public class PokemonSpeciesDetail
{
    public int rate { get; set; }
    public PokemonSpecies pokemon_species { get; set; }
}

public class RequiredForEvolution
{
    public string name { get; set; }
    public string url { get; set; }
}

public class Genders
{
    public int id { get; set; }
    public string name { get; set; }
    public List<PokemonSpeciesDetail> pokemon_species_details { get; set; }
    public List<RequiredForEvolution> required_for_evolution { get; set; }
}

}
