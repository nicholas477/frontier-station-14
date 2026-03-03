using Robust.Shared.Configuration;
namespace Content.Server._EGG.CCVar;

[CVarDefs]
public sealed partial class CCVars
{
    /// <summary>
    /// Interval, in seconds, between automatic checks that give players antag bounty contracts.
    /// </summary>
    public static readonly CVarDef<float> EGGBountyNextAntagDecisionSeconds =
        CVarDef.Create("eggbounty.next_antag_decision_seconds", 600f, CVar.SERVERONLY | CVar.ARCHIVE);
}
