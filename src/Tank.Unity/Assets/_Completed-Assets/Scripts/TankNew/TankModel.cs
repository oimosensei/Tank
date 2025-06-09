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

        // --- イベントストリーム ---
        public ISubject<Unit> OnDeath { get; } = new Subject<Unit>();


        // --- 読み取り専用の派生プロパティ ---
        public IReadOnlyReactiveProperty<string> ColoredPlayerText { get; }

        public bool isSelf = true;


        // --- 内部状態・設定 ---
        public readonly float m_StartingHealth;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public TankModel(int playerNumber, Color playerColor, float startingHealth)
        {
            // 初期値設定
            m_StartingHealth = startingHealth;

            // ReactivePropertyの初期化
            PlayerNumber = new ReactiveProperty<int>(playerNumber);
            PlayerColor = new ReactiveProperty<Color>(playerColor);
            Wins = new ReactiveProperty<int>(0);
            CurrentHealth = new ReactiveProperty<float>(m_StartingHealth);

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
        }

        public void TakeDamage(float amount)
        {
            if (IsDead.Value) return;
            CurrentHealth.Value -= amount;
        }


        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}