using Robust.Shared.Configuration;
using Content.Server._EGG.CCVar;

namespace Content.Server._EGG.BountyContracts;

public sealed partial class EGGBountyContractSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public TimeSpan NextAntagDecisionTimerLength { get; private set; }

    private void InitializeCVars()
    {
        _cfg.OnValueChanged(CCVars.EGGBountyNextAntagDecisionSeconds, value => NextAntagDecisionTimerLength = TimeSpan.FromSeconds(value), true);
    }
}
