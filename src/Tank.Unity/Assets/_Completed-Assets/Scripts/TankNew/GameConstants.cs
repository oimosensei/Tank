using UnityEngine;

[CreateAssetMenu(fileName = "GameConstants", menuName = "Tank/Game Constants")]
public class GameConstants : ScriptableObject
{
    [Header("Player Settings")]
    [SerializeField] private Color[] playerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
    
    [Header("Tank Health")]
    [SerializeField] private float startingHealth = 100f;
    
    [Header("Tank Shooting")]
    [SerializeField] private float minLaunchForce = 15f;
    [SerializeField] private float maxLaunchForce = 30f;
    [SerializeField] private float maxChargeTime = 0.75f;
    
    [Header("Tank Movement")]
    [SerializeField] private float tankSpeed = 12f;
    [SerializeField] private float tankTurnSpeed = 180f;
    
    [Header("Turret Control")]
    [SerializeField] private float turretRotationSensitivity = 1f;
    
    public Color[] PlayerColors => playerColors;
    public float StartingHealth => startingHealth;
    public float MinLaunchForce => minLaunchForce;
    public float MaxLaunchForce => maxLaunchForce;
    public float MaxChargeTime => maxChargeTime;
    public float TankSpeed => tankSpeed;
    public float TankTurnSpeed => tankTurnSpeed;
    public float TurretRotationSensitivity => turretRotationSensitivity;
}