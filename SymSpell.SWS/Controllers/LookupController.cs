using Microsoft.AspNetCore.Mvc;

namespace SymSpellSWS.Controllers;

[ApiController]
[Route("[controller]")]
public class LookupController : ControllerBase
{
    private readonly SymSpell _symSpell;

    public LookupController(SymSpell symSpell)
    {
        _symSpell = symSpell;
    }

    [HttpGet]
    public IEnumerable<SymSpell.SuggestItem> Get([FromQuery] string word, [FromQuery] string verbosity = "Closest")
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return Enumerable.Empty<SymSpell.SuggestItem>();
        }
        
        var verbosityEnum = verbosity.ToLower() switch
        {
            "top" => SymSpell.Verbosity.Top,
            "all" => SymSpell.Verbosity.All,
            _ => SymSpell.Verbosity.Closest,
        };

        return _symSpell.Lookup(word, verbosityEnum);
    }
}
