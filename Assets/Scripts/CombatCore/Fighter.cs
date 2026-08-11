namespace TheLift.CombatCore
{
    public class Fighter
    {
        private readonly FighterConfig _config;

        private int _framesSinceLastStaminaSpend;
        private bool _isExhausted;

        public float Stamina { get; private set; }
        public float Balance { get; private set; }
        public float Composure { get; private set; }
        public float Rattled { get; private set; }
        public float Adrenaline { get; private set; }

        public bool IsExhausted => _isExhausted;

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

        public Fighter(FighterConfig config = null)
        {
            _config = config ?? new FighterConfig();
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

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
