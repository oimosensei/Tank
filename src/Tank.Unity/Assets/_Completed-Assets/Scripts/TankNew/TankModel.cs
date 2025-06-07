using System;
using UniRx;
using UnityEngine;

namespace Nakatani
{
    // タンクの状態をリアクティブに管理するModelクラス。
    public class TankModel : IDisposable
    {
        // --- リアクティブな状態プロパティ ---
        public IReactiveProperty<int> PlayerNumber { get; }
        public IReactiveProperty<Color> PlayerColor { get; }
        public IReactiveProperty<int> Wins { get; }

        public IReactiveProperty<float> CurrentHealth { get; }
        public IReadOnlyReactiveProperty<bool> IsDead { get; }
        public BoolReactiveProperty IsControlEnabled { get; } = new BoolReactiveProperty(false);

        // 入力状態
        public ReactiveProperty<float> MovementInputValue { get; } = new ReactiveProperty<float>();
        public ReactiveProperty<float> TurnInputValue { get; } = new ReactiveProperty<float>();

        // 射撃状態
        public ReactiveProperty<float> CurrentLaunchForce { get; }
        public BoolReactiveProperty IsCharging { get; } = new BoolReactiveProperty(false);


        // --- イベントストリーム ---
        public ISubject<Unit> OnFire { get; } = new Subject<Unit>();
        public ISubject<Unit> OnDeath { get; } = new Subject<Unit>();


        // --- 読み取り専用の派生プロパティ ---
        public IReadOnlyReactiveProperty<string> ColoredPlayerText { get; }

        public bool isSelf = true;


        // --- 内部状態・設定 ---
        public readonly float m_StartingHealth;
        public readonly float m_MinLaunchForce;
        public readonly float m_MaxLaunchForce;
        private readonly float m_ChargeSpeed;
        private bool m_Fired; // 内部的な発射済みフラグ

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public TankModel(int playerNumber, Color playerColor, float startingHealth, float minLaunchForce, float maxLaunchForce, float maxChargeTime)
        {
            // 初期値設定
            m_StartingHealth = startingHealth;
            m_MinLaunchForce = minLaunchForce;
            m_MaxLaunchForce = maxLaunchForce;
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / maxChargeTime;

            // ReactivePropertyの初期化
            PlayerNumber = new ReactiveProperty<int>(playerNumber);
            PlayerColor = new ReactiveProperty<Color>(playerColor);
            Wins = new ReactiveProperty<int>(0);
            CurrentHealth = new ReactiveProperty<float>(m_StartingHealth);
            CurrentLaunchForce = new ReactiveProperty<float>(m_MinLaunchForce);

            // 派生プロパティの定義
            IsDead = CurrentHealth.Select(h => h <= 0f).ToReadOnlyReactiveProperty();

            ColoredPlayerText = PlayerNumber.CombineLatest(PlayerColor,
                (num, color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>PLAYER {num}</color>")
                .ToReadOnlyReactiveProperty();

            // イベントストリームの購読設定
            IsDead.Where(isDead => isDead)
                .Subscribe(_ => OnDeath.OnNext(Unit.Default))
                .AddTo(_disposables);
        }

        // --- 状態操作メソッド ---
        public void Reset()
        {
            CurrentHealth.Value = m_StartingHealth;
            MovementInputValue.Value = 0f;
            TurnInputValue.Value = 0f;
            CurrentLaunchForce.Value = m_MinLaunchForce;
            IsCharging.Value = false;
            m_Fired = true; // 発射不可状態から開始
        }

        public void TakeDamage(float amount)
        {
            if (IsDead.Value) return;
            CurrentHealth.Value -= amount;
        }

        public void HandleShootingInput(bool getButtonDown, bool getButton, bool getButtonUp)
        {
            if (!IsControlEnabled.Value) return;

            // 最大までチャージされたら自動発射
            if (CurrentLaunchForce.Value >= m_MaxLaunchForce && !m_Fired)
            {
                Fire();
            }
            // 射撃ボタン押下
            else if (getButtonDown)
            {
                m_Fired = false;
                IsCharging.Value = true;
                CurrentLaunchForce.Value = m_MinLaunchForce;
            }
            // 射撃ボタン長押し中
            else if (getButton && !m_Fired)
            {
                CurrentLaunchForce.Value += m_ChargeSpeed * Time.deltaTime;
            }
            // 射撃ボタン解放
            else if (getButtonUp && !m_Fired)
            {
                Fire();
            }
        }

        private void Fire()
        {
            m_Fired = true;
            IsCharging.Value = false;
            OnFire.OnNext(Unit.Default);
            CurrentLaunchForce.Value = m_MinLaunchForce;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}