using System.Collections;

using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Player Movement Data", menuName = "Scriptable Objects/Player/Player Movement Data")]
public class PlayerMovementData : ScriptableObject
{
    [Header("Walk")]
    [Range(1f, 100f)] public float MaxWalkSpeed = 10f;
    
    [Header("Run")]
    [Range(1f, 100f)] public float MaxRunSpeed = 20f;

    [Header("Acceleration")]
    [Range(0.25f, 50)] public float GroundAcceleration = 5f;
    [Range(0.25f, 50)] public float GroundDeceleration = 20f;
    [Range(0.25f, 50)] public float AirAcceleration = 5f;
    [Range(0.25f, 50)] public float AirDeceleration = 20f;
    [Range(0.25f, 50)] public float WallJumpMoveAcceleration = 5f;
    [Range(0.25f, 50)] public float WallJumpMoveDeceleration = 5f;

    [Header("Jump")]
    public float Jumpheight = 6.5f;
    [Range(1f, 1.1f)] public float JumpHeightCompensationFactor = 1.054f;
    public float TimeTillJumpApex = 0.35f;
    [Range(0.01f, 5f)] public float GravityOnReleaseMultiplier = 2f;
    public float MaxFallSpeed = 26f;
    [Range(1, 5)] public int NumberOfJumpsAllowed = 2;

    [Header("Reset Jump Option")]
    public bool ResetJumpsOnWallSlide = true;

    [Header("Jump Cut")]
    [Range(0.02f, 0.3f)] public float TimeForUpwardsCancle = 0.027f;

    [Header("Jump Apex")]
    [Range(0.5f, 1f)] public float ApexThreshold = 0.97f;
    [Range(0.01f, 1f)] public float ApexHangTime = 0.075f;

    [Header("Jump Buffer")]
    [Range(0f, 1f)] public float JumpBufferTime = 0.1f;

    [Header("Jump Coyote Time")]
    [Range(0f, 1f)] public float JumpCoyoteTime = 0.1f;

    [Header("Wall SLide")]
    [Min(0.01f)] public float WallSlideSpeed = 5f;
    [Range(0.25f, 50f)] public float WallSlideDecelerationSpeed = 50f;

    [Header("Wall Jump")]
    public Vector2 WallJumpDirection = new Vector2(-20f, 6.5f);
    [Range(0f, 1f)] public float WallJumpPostBufferTime = 0.125f;
    [Range(0.01f, 5f)] public float WallJumpGravityOnReleaseMultiplier = 1f;

    [Header("Dash")]
    [Range(0.01f, 1f)] public float DashTime = 0.11f;
    [Range(1f, 200f)] public float DashSpeed = 50f;
    [Range(0f, 1f)] public float TimeBetweenGroundDashes = 0.25f;
    public bool ResetDashOnWallSlide = true;
    [Range(0, 5)] public int NumberOfDashes = 2;
    [Range(0f, 0.5f)] public float DashDiaganollyBias = 0.4f;

    [Header("Dash Cancel Time")]
    [Range(0.01f, 5f)] public float DashGravityOnReleaseMultiplier = 1f;
    [Range(0.02f, 0.3f)] public float DashTimeForUpwardsCancel = 0.027f;


    [Header("Grounded/Collision Checks")]
    public LayerMask GroundLayer;
    public LayerMask WallLayer;
    public float GroundDetectionRayLength = 0.02f;
    [Space]
    public float HeadDetectionRayLength = 0.02f;
    [Range(0, 2f)] public float HeadWidth = 0.75f;
    [Space]
    public float WallDetectioRayLength = 0.125f;
    [Range(0.01f, 2f)] public float WallDetectionRayHeightMultiplier = 0.9f;

    [Header("Debug")]
    public bool DebugShowIsGroundedBox;
    public bool DebugShowHeadBumpBox;
    public bool DebugShowWallHitBox;

    [Header("Jump Visualization Tool")]
    public bool ShowWalkJumpArc = false;
    public bool ShowRunJumpArc = false;
    public bool StopOnCollision = true;

    public readonly Vector2[] DashDirections = new Vector2[]
    {
        new Vector2(0, 0), // Nothing
        new Vector2(0, 1), // Up
        new Vector2(0, -1), // Down
        new Vector2(1, 0), // Right
        new Vector2(-1, 0), // Left
        new Vector2(1, 1).normalized, // Top-Right
        new Vector2(-1, 1).normalized, // Top-Left
        new Vector2(1, -1).normalized, // Bottom-Right
        new Vector2(-1, -1).normalized, // Bottom-Left
    };

    // Jump Gravity & Velocity
    public float Gravity {  get; private set; }
    public float InitialJumpVelocity { get; private set; }
    public float AdjustedJumpHeight { get; private set; }

    // Wall Jump Gravity & Velocity
    public float WallJumpGravity { get; private set; }
    public float InitialWallJumpVelocity { get; private set; }
    public float AdjustedWallJumpHeight { get; private set; }

    private void OnValidate()
    {
        CalculateValues();
    }

    private void OnEnable()
    {
        CalculateValues();
    }

    private void CalculateValues()
    {
        AdjustedJumpHeight = Jumpheight * JumpHeightCompensationFactor;
        Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2);
        InitialJumpVelocity = Mathf.Abs(Gravity) * TimeTillJumpApex;

        AdjustedWallJumpHeight = WallJumpDirection.y * JumpHeightCompensationFactor;
        WallJumpGravity = -(2f * AdjustedWallJumpHeight) / Mathf.Pow(TimeTillJumpApex, 2f);
        InitialWallJumpVelocity = Mathf.Abs(WallJumpGravity) * TimeTillJumpApex;    
    }

    


}
