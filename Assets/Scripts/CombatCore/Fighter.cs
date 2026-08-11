namespace TheLift.CombatCore
{
    public class Fighter
    {
        private readonly FighterConfig _config;
        private readonly ActionConfig _actionConfig;

        private int _framesSinceLastStaminaSpend;
        private bool _isExhausted;

        private ActionPhase _actionPhase = ActionPhase.Neutral;
        private ActionDefinition _currentAction;
        private int _framesInPhase;
        private int _currentStartupFrames;
        private int _staggerDurationFrames;

        private int _lightChainCount;
        private int _framesSinceLastLightAction;

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

        public Fighter(FighterConfig config = null, ActionConfig actionConfig = null)
        {
            _config = config ?? new FighterConfig();
            _actionConfig = actionConfig ?? new ActionConfig();

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
            // Composure never regenerates (items only, not modelled yet).
            // Adrenaline falls only out of combat (§4.4) — there is no combat state
            // to be "out of" yet, so it holds until a later phase adds one.
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
        // action's Startup/Active/Recovery, and not while Staggered. "No combos" (§4.1).
        public bool TryStartAction(ActionType type)
        {
            if (_actionPhase != ActionPhase.Neutral) return false;

            ActionDefinition definition = _actionConfig.GetDefinition(type);
            if (Stamina < definition.StaminaCost) return false;

            int startupBonus = 0;
            if (type == ActionType.Light)
            {
                if (_lightChainCount > 0 && _framesSinceLastLightAction > _actionConfig.LightComboWindowFrames)
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

        // Applies this fighter's currently Active action to a target. No-ops outside the
        // Active window — there is no defence system yet, so a caller must decide the hit
        // landed and call this only while Active.
        public void ResolveHit(Fighter target)
        {
            if (_actionPhase != ActionPhase.Active || _currentAction == null) return;

            float multiplier = IsExhausted ? 0.5f : 1f; // §4.3 — Exhausted: damage -50%

            target.DamageComposure(_currentAction.ComposureDamage * multiplier);
            target.DamageBalance(_currentAction.BalanceDamage * multiplier);

            float rattledDamage = _currentAction.RattledDamage * multiplier;
            if (rattledDamage > 0f) target.AddRattled(rattledDamage);

            if (_currentAction.Type == ActionType.Heavy)
            {
                target.Stagger(_actionConfig.HeavyStaggerFrames); // §4.5 — heavies break Balance -> 18f stagger
            }
        }

        public void Stagger(int frames)
        {
            _actionPhase = ActionPhase.Staggered;
            _framesInPhase = 0;
            _staggerDurationFrames = frames;
            _currentAction = null;
            _currentStartupFrames = 0;
            _lightChainCount = 0;
        }

        private void TickStamina()
        {
            if (_framesSinceLastStaminaSpend < _config.StaminaRegenDelayFrames)
            {
                _framesSinceLastStaminaSpend++;
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
