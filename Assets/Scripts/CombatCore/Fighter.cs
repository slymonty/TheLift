namespace TheLift.CombatCore
{
    public class Fighter
    {
        private readonly FighterConfig _config;
        private readonly ActionConfig _actionConfig;
        private readonly DefenceConfig _defenceConfig;
        private readonly CompromisedConfig _compromisedConfig;

        private int _framesSinceLastStaminaSpend;
        private bool _isExhausted;

        private ActionPhase _actionPhase = ActionPhase.Neutral;
        private ActionDefinition _currentAction;
        private int _framesInPhase;
        private int _currentStartupFrames;
        private int _staggerDurationFrames;

        private int _lightChainCount;
        private int _framesSinceLastLightAction;

        private int _slipFramesRemaining;
        private bool _isCovering;
        private bool _isGivingGround;
        private TieUp _activeTieUp;

        private int _proneFramesRemaining;
        private int _clingChainCount;
        private int _framesSinceLastClingEnded;

        public float Stamina { get; private set; }
        public float Balance { get; private set; }
        public float Composure { get; private set; }
        public float Rattled { get; private set; }
        public float Adrenaline { get; private set; }

        public bool IsExhausted => _isExhausted;

        public ActionPhase ActionPhase => _actionPhase;
        public ActionType? CurrentActionType => _currentAction?.Type;
        public int CurrentStartupFrames => _currentStartupFrames;
        public int LightChainCount => _lightChainCount;

        public bool IsSlipping => _slipFramesRemaining > 0;
        public bool IsCovering => _isCovering;
        public bool IsGivingGround => _isGivingGround;
        public bool IsTiedUp => _activeTieUp != null && _activeTieUp.IsActive;
        public TieUp ActiveTieUp => _activeTieUp;

        public bool IsProne => _proneFramesRemaining > 0;
        public int ProneFramesRemaining => _proneFramesRemaining;

        // §4.10 — Concussed and Down are an exhaustive "Cling only" restriction: no
        // Snag, no Post up, no Drag down, regardless of any other state.
        private bool IsClingOnlyCompromised => RattledState == RattledState.Concussed || RattledState == RattledState.Down;

        // §4.9 — eligible to Cling: Dazed, Concussed, and Down all list it; Exhausted
        // "also counts as Compromised" and Cling is the one verb present at every
        // Rattled-based tier, so it is extended here as Exhausted's concrete effect.
        public bool CanCling =>
            RattledState == RattledState.Dazed || IsClingOnlyCompromised || IsExhausted;

        public AdrenalineState AdrenalineState
        {
            get
            {
                if (Adrenaline >= _config.AdrenalineGoneThreshold) return AdrenalineState.Gone;
                if (Adrenaline >= _config.AdrenalineFloodedThreshold) return AdrenalineState.Flooded;
                if (Adrenaline >= _config.AdrenalineHotThreshold) return AdrenalineState.Hot;
                return AdrenalineState.Composed;
            }
        }

        public RattledState RattledState
        {
            get
            {
                if (Rattled >= _config.RattledDownThreshold) return RattledState.Down;
                if (Rattled >= _config.RattledConcussedThreshold) return RattledState.Concussed;
                if (Rattled >= _config.RattledDazedThreshold) return RattledState.Dazed;
                if (Rattled >= _config.RattledShakenThreshold) return RattledState.Shaken;
                return RattledState.Fine;
            }
        }

        public Fighter(FighterConfig config = null, ActionConfig actionConfig = null, DefenceConfig defenceConfig = null, CompromisedConfig compromisedConfig = null)
        {
            _config = config ?? new FighterConfig();
            _actionConfig = actionConfig ?? new ActionConfig();
            _defenceConfig = defenceConfig ?? new DefenceConfig();
            _compromisedConfig = compromisedConfig ?? new CompromisedConfig();

            Stamina = _config.MaxStamina;
            Balance = _config.MaxBalance;
            Composure = _config.MaxComposure;
            Rattled = 0f;
            Adrenaline = 0f;
            _framesSinceLastStaminaSpend = _config.StaminaRegenDelayFrames;
            _isExhausted = false;
        }

        public void Tick(int frame)
        {
            TickStamina();
            TickBalance();
            TickRattled();
            TickActionState();
            if (_slipFramesRemaining > 0) _slipFramesRemaining--;
            if (_proneFramesRemaining > 0) _proneFramesRemaining--;
            _framesSinceLastClingEnded++;
            // Composure never regenerates (items only, not modelled yet).
            // Adrenaline falls only out of combat (§4.4) — there is no combat state
            // to be "out of" yet, so it holds until a later phase adds one.
            // TieUp/Grapple/Cling are paired states shared by two fighters and are
            // ticked externally by the caller, once per frame, not from here (§4.6/§4.7/§4.9).
        }

        public void SpendStamina(float amount)
        {
            if (amount <= 0f) return;

            Stamina = Clamp(Stamina - amount, 0f, _config.MaxStamina);
            _framesSinceLastStaminaSpend = 0;

            if (Stamina <= 0f)
            {
                _isExhausted = true;
            }
        }

        public void DamageBalance(float amount)
        {
            if (amount <= 0f) return;
            Balance = Clamp(Balance - amount, 0f, _config.MaxBalance);
        }

        public void DamageComposure(float amount)
        {
            if (amount <= 0f) return;
            Composure = Clamp(Composure - amount, 0f, _config.MaxComposure);
        }

        public void AddRattled(float amount)
        {
            if (amount <= 0f) return;
            Rattled = Clamp(Rattled + amount, 0f, _config.MaxRattled);
        }

        public void AddAdrenaline(float amount)
        {
            if (amount <= 0f) return;
            Adrenaline = Clamp(Adrenaline + amount, 0f, _config.MaxAdrenaline);
        }

        // §4.5 — a new action can only be committed to from Neutral: not during another
        // action's Startup/Active/Recovery, not while Staggered, and not while committed
        // to a §4.6 defensive stance or a paired tie-up/grapple. "No combos" (§4.1).
        public bool TryStartAction(ActionType type)
        {
            if (!IsFreeToAct()) return false;

            ActionDefinition definition = _actionConfig.GetDefinition(type);
            if (Stamina < definition.StaminaCost) return false;

            int startupBonus = 0;
            if (type == ActionType.Light)
            {
                // Frame-boundary convention: a window of N frames is valid on ticks
                // 0..N-1 and expires on tick N (matches Stagger/Startup/Active/Recovery).
                if (_lightChainCount > 0 && _framesSinceLastLightAction >= _actionConfig.LightComboWindowFrames)
                {
                    _lightChainCount = 0; // combo window expired — this is a fresh chain
                }
                if (_lightChainCount >= 2) return false; // there is no third light in a chain

                startupBonus = _lightChainCount == 1 ? _actionConfig.LightComboStartupBonusFrames : 0;
            }
            else
            {
                _lightChainCount = 0; // throwing anything else breaks the chain
            }

            SpendStamina(definition.StaminaCost);

            _currentAction = definition;
            _currentStartupFrames = definition.StartupFrames + startupBonus;
            _actionPhase = ActionPhase.Startup;
            _framesInPhase = 0;

            if (type == ActionType.Light)
            {
                _lightChainCount++;
            }

            return true;
        }

        // Applies this fighter's currently Active action (a strike) to a target. No-ops
        // outside the Active window — a caller must decide the hit landed and call this
        // only while Active. §4.6 defence is checked here: Slip beats a strike outright
        // (never a grapple — grapples resolve through their own methods and do not call
        // this); Cover reduces but never zeroes the damage. No invincibility frames exist
        // anywhere (§4.1), so nothing else in this method can produce zero damage.
        public void ResolveHit(Fighter target)
        {
            if (_actionPhase != ActionPhase.Active || _currentAction == null) return;

            if (target.IsSlipping) return; // §4.6 — Slip beats strikes only

            float multiplier = IsExhausted ? 0.5f : 1f; // §4.3 — Exhausted: damage -50%

            float composureDamage = _currentAction.ComposureDamage * multiplier;
            float balanceDamage = _currentAction.BalanceDamage * multiplier;
            float rattledDamage = _currentAction.RattledDamage * multiplier;

            if (target.IsCovering)
            {
                // §4.6 — Cover absorbs ~70%; it never prevents a grapple, and it never
                // reduces a strike to zero.
                composureDamage *= _defenceConfig.CoverDamageMultiplier;
                balanceDamage *= _defenceConfig.CoverDamageMultiplier;
                rattledDamage *= _defenceConfig.CoverDamageMultiplier;
                target.SpendStamina(_defenceConfig.CoverStaminaCostPerHit);
            }

            target.DamageComposure(composureDamage);
            target.DamageBalance(balanceDamage);
            if (rattledDamage > 0f) target.AddRattled(rattledDamage);

            if (_currentAction.Type == ActionType.Heavy)
            {
                target.Stagger(_actionConfig.HeavyStaggerFrames); // §4.5 — heavies break Balance -> 18f stagger
            }
        }

        // §4.6 — SLIP: 12 stamina, ~6f window, beats strikes only (a grab still catches
        // you mid-slip — ResolveHit never checks IsSlipping for grapples). Unavailable
        // when Flooded; extended here to Gone too, since Gone is strictly more impaired
        // and Slip is explicitly "the skill-expression move" (fine motor).
        public bool TrySlip()
        {
            if (!IsFreeToAct()) return false;
            if (AdrenalineState == AdrenalineState.Flooded || AdrenalineState == AdrenalineState.Gone) return false;
            if (Stamina < _defenceConfig.SlipStaminaCost) return false;

            SpendStamina(_defenceConfig.SlipStaminaCost);
            _slipFramesRemaining = _defenceConfig.SlipWindowFrames;
            return true;
        }

        // §4.6 — COVER: no cost to raise; costs 10 per hit actually absorbed (charged in
        // ResolveHit). Does not prevent a grapple (§4.7 resolution never checks this).
        public bool StartCover()
        {
            if (!IsFreeToAct()) return false;
            _isCovering = true;
            return true;
        }

        public void StopCover()
        {
            _isCovering = false;
        }

        // §4.6 — GIVE GROUND: free. Can't attack while giving ground (movement/speed is a
        // Game-layer concern with no representation in CombatCore).
        public bool StartGivingGround()
        {
            if (!IsFreeToAct()) return false;
            _isGivingGround = true;
            return true;
        }

        public void StopGivingGround()
        {
            _isGivingGround = false;
        }

        // §4.6 — SHOVE: 15 stamina, free against an Exhausted target. Hits Balance (no
        // exact number given — see DefenceConfig.ShoveBalanceDamage).
        //
        // Ruling: Shove breaks a clinch immediately for the fighter who pays, when the
        // target is that fighter's own Tie-up partner. Inside that clinch, Shove always
        // costs the full 15 — the "free against Exhausted" discount is neutral-only,
        // since Tie-up is already a stamina contest ("higher remaining stamina wins the
        // break") and a free exit would undercut it. The other participant gets no free
        // exit of their own; the hold simply ends for both. A shove against someone who
        // is tied up with a third fighter does not touch that tie-up at all.
        public bool TryShove(Fighter target)
        {
            if (target == null) return false;

            bool isEscapingSharedTieUp =
                _activeTieUp != null && _activeTieUp.IsActive &&
                _activeTieUp.Involves(this) && _activeTieUp.Involves(target);

            if (isEscapingSharedTieUp)
            {
                if (Stamina < _defenceConfig.ShoveStaminaCost) return false;

                SpendStamina(_defenceConfig.ShoveStaminaCost);
                target.DamageBalance(_defenceConfig.ShoveBalanceDamage);
                _activeTieUp.Break();
                return true;
            }

            if (!IsFreeToAct()) return false;

            float cost = target.IsExhausted ? 0f : _defenceConfig.ShoveStaminaCost;
            if (Stamina < cost) return false;

            SpendStamina(cost);
            target.DamageBalance(_defenceConfig.ShoveBalanceDamage);

            return true;
        }

        public void Stagger(int frames)
        {
            _actionPhase = ActionPhase.Staggered;
            _framesInPhase = 0;
            _staggerDurationFrames = frames;
            _currentAction = null;
            _currentStartupFrames = 0;
            _lightChainCount = 0;
            _isCovering = false;
            _isGivingGround = false;
        }

        // §4.8 — "Knocked-down players are prone ~2s." Interrupts whatever this fighter
        // was doing, the same way Stagger does; Prone itself is tracked as an independent
        // timer (like Slip), not a distinct ActionPhase, since a fighter can be knocked
        // prone by something unrelated to a Heavy stagger.
        public void KnockDown(int frames)
        {
            _proneFramesRemaining = frames;
            _actionPhase = ActionPhase.Neutral;
            _framesInPhase = 0;
            _currentAction = null;
            _currentStartupFrames = 0;
            _lightChainCount = 0;
            _isCovering = false;
            _isGivingGround = false;
        }

        // §4.9 — SNAG: 15 stamina, from prone/kneeling (Prone, or Dazed's knee-drop).
        // Concussed/Down override to "Cling only" even if also Prone. Target Balance -60,
        // amplified if the target was sprinting (no exact multiplier given — see config).
        public bool TrySnag(Fighter target, bool targetIsSprinting = false)
        {
            if (target == null) return false;
            if (IsClingOnlyCompromised) return false;

            bool eligible = IsProne || RattledState == RattledState.Dazed;
            if (!eligible) return false;

            if (Stamina < _compromisedConfig.SnagStaminaCost) return false;
            SpendStamina(_compromisedConfig.SnagStaminaCost);

            float balanceDamage = _compromisedConfig.SnagBalanceDamage;
            if (targetIsSprinting) balanceDamage *= _compromisedConfig.SnagSprintingMultiplier;
            target.DamageBalance(balanceDamage);

            return true;
        }

        // §4.9 — DRAG DOWN: 22 stamina, from Prone. Both fighters end up prone.
        public bool TryDragDown(Fighter target)
        {
            if (target == null) return false;
            if (IsClingOnlyCompromised) return false;
            if (!IsProne) return false;

            if (Stamina < _compromisedConfig.DragDownStaminaCost) return false;
            SpendStamina(_compromisedConfig.DragDownStaminaCost);

            target.KnockDown(_compromisedConfig.DragDownProneFrames);
            return true;
        }

        // §4.9 — POST UP: 10 stamina, only while Dazed, requires nearby furniture
        // (modelled as a caller-supplied flag — no positional system exists here).
        // Recovery -30%: shortens the Prone timer, if one is currently running.
        public bool TryPostUp(bool nearbyFurniture)
        {
            if (RattledState != RattledState.Dazed) return false;
            if (!nearbyFurniture) return false;
            if (Stamina < _compromisedConfig.PostUpStaminaCost) return false;

            SpendStamina(_compromisedConfig.PostUpStaminaCost);

            if (IsProne)
            {
                _proneFramesRemaining = (int)System.Math.Round(
                    _proneFramesRemaining * (1f - _compromisedConfig.PostUpRecoveryReductionFraction));
            }

            return true;
        }

        // §4.9 — STOMP-OFF: only breaks a Cling; the clinger can always re-grab. A
        // teammate pays 12 and 0.5s; the clung fighter can pay their own way out at
        // 15 and slower — but only if they aren't themselves Dazed and up, since that
        // tier's "Can do" list doesn't include Stomp-off ("the cheap fix requires a
        // teammate"). Hits Balance, not Composure; adds Rattled, not damage.
        public bool TryStompOff(Cling cling, bool isSelf)
        {
            if (cling == null || !cling.IsActive) return false;
            if (RattledState == RattledState.Dazed || IsClingOnlyCompromised) return false;
            if (isSelf && cling.Target != this) return false;
            if (!isSelf && this == cling.Clinger) return false;

            float cost = isSelf ? _compromisedConfig.StompOffSelfStaminaCost : _compromisedConfig.StompOffTeammateStaminaCost;
            if (Stamina < cost) return false;

            SpendStamina(cost);
            cling.Clinger.DamageBalance(_compromisedConfig.StompOffBalanceDamage);
            cling.Clinger.AddRattled(_compromisedConfig.StompOffRattledDamage);
            cling.End();

            return true;
        }

        // A fighter can only commit to something new from a clean Neutral: not mid-action,
        // not Staggered, not holding a §4.6 stance, not locked in a paired tie-up, not
        // Prone, and not Dazed/Concussed/Down — §4.10's "Can do" column for those tiers
        // is an exhaustive list (Snag/Cling/Post up, or Cling only) that does not include
        // strikes or any other normal action.
        private bool IsFreeToAct()
        {
            return _actionPhase == ActionPhase.Neutral
                && !IsSlipping && !_isCovering && !_isGivingGround && !IsTiedUp
                && !IsProne
                && RattledState != RattledState.Dazed
                && !IsClingOnlyCompromised;
        }

        internal bool CanEnterTieUp() => IsFreeToAct();

        internal void EnterTieUp(TieUp tieUp)
        {
            _activeTieUp = tieUp;
        }

        internal void ExitTieUp()
        {
            _activeTieUp = null;
        }

        // §4.9 — "Escalating cost on re-Cling (8 -> 12 -> 18/sec)". Frame-boundary
        // convention: the 10s window is valid on ticks 0..N-1 and stale at tick N.
        internal int BeginClingChain(CompromisedConfig config)
        {
            if (_clingChainCount > 0 && _framesSinceLastClingEnded >= config.ClingChainWindowFrames)
            {
                _clingChainCount = 0; // window expired -- this is a fresh chain
            }

            int tierIndex = _clingChainCount > 2 ? 2 : _clingChainCount;
            _clingChainCount = _clingChainCount >= 2 ? 2 : _clingChainCount + 1;
            return tierIndex;
        }

        internal void EndClingChain()
        {
            _framesSinceLastClingEnded = 0;
        }

        private void TickStamina()
        {
            // Frame-boundary convention: a delay of N frames blocks regen on ticks
            // 0..N-1 and regen resumes on tick N — the same tick that reaches the
            // threshold, not the one after (matches Stagger/Startup/Active/Recovery).
            if (_framesSinceLastStaminaSpend < _config.StaminaRegenDelayFrames)
            {
                _framesSinceLastStaminaSpend++;
            }

            if (_framesSinceLastStaminaSpend < _config.StaminaRegenDelayFrames)
            {
                return;
            }

            float regenPerFrame = _config.StaminaRegenPerSecond / _config.FramesPerSecond;
            Stamina = Clamp(Stamina + regenPerFrame, 0f, _config.MaxStamina);

            // Hysteresis: Exhausted was set at 0 stamina and only clears at the
            // threshold, not the moment stamina ticks above zero (§4.3).
            if (_isExhausted && Stamina >= _config.ExhaustedClearThreshold)
            {
                _isExhausted = false;
            }
        }

        private void TickBalance()
        {
            float regenPerFrame = _config.BalanceRegenPerSecond / _config.FramesPerSecond;
            Balance = Clamp(Balance + regenPerFrame, 0f, _config.MaxBalance);
        }

        private void TickRattled()
        {
            float regenPerFrame = _config.RattledRegenPerSecond / _config.FramesPerSecond;
            Rattled = Clamp(Rattled - regenPerFrame, 0f, _config.MaxRattled);
        }

        private void TickActionState()
        {
            _framesSinceLastLightAction++;

            switch (_actionPhase)
            {
                case ActionPhase.Neutral:
                    return;

                case ActionPhase.Startup:
                    _framesInPhase++;
                    if (_framesInPhase >= _currentStartupFrames)
                    {
                        _actionPhase = ActionPhase.Active;
                        _framesInPhase = 0;
                    }
                    return;

                case ActionPhase.Active:
                    _framesInPhase++;
                    if (_framesInPhase >= _currentAction.ActiveFrames)
                    {
                        _actionPhase = ActionPhase.Recovery;
                        _framesInPhase = 0;
                    }
                    return;

                case ActionPhase.Recovery:
                    _framesInPhase++;
                    if (_framesInPhase >= _currentAction.RecoveryFrames)
                    {
                        // Combo window starts counting from the end of Recovery, not Startup.
                        if (_currentAction.Type == ActionType.Light)
                        {
                            _framesSinceLastLightAction = 0;
                        }
                        _actionPhase = ActionPhase.Neutral;
                        _framesInPhase = 0;
                        _currentAction = null;
                        _currentStartupFrames = 0;
                    }
                    return;

                case ActionPhase.Staggered:
                    _framesInPhase++;
                    if (_framesInPhase >= _staggerDurationFrames)
                    {
                        _actionPhase = ActionPhase.Neutral;
                        _framesInPhase = 0;
                    }
                    return;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
