using UnityEngine;

public class PlayerMovement : AnimatorCoder
{
    [Header("References")]
    public PlayerMovementData moveStats;
    [SerializeField] private Collider2D bodyColl;
    [SerializeField] private Collider2D feetColl;

    private Rigidbody2D rb;

    // Movement variables
    private float horizontalVelocity;
    private bool isFacingRight;

    // Collision check variables
    private RaycastHit2D groundHit;
    private RaycastHit2D headHit;
    private RaycastHit2D wallHit;
    private RaycastHit2D lastWallHit;
    private bool isGrounded;
    private bool bumpedHead;
    private bool isTouchingWall;

    // Jump variables
    public float VerticalVelocity {  get; private set; }
    private bool jumpInput;
    private bool isJumping;
    private bool isFalling;
    private bool isFastFalling;
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    private int numberOfJumpsUsed;

    // Wall slide variables
    private bool isWallSliding;
    private bool isWallSlideFalling;

    // Wall jump variables
    private bool useWallJumpMoveStats;
    private bool isWallJumping;
    private float wallJumpTime;
    private bool isWallJumpFastFalling;
    private bool isWallJumpFalling;
    private float wallJumpFastFallTime;
    private float wallJumpFastFallReleaseSpeed;

    // Dash variables
    private bool isDashing;
    private bool isAirDashing;
    private float dashTimer;
    private float dashOnGroundTimer;
    private int numberOfDashesUsed;
    private Vector2 dashDirection;
    private bool isDashFastFalling;
    private float dashFastFallTime;
    private float dashFastFallReleaseSpeed;

    // Apex variables
    private float apexPoint;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;
    private float wallJumpApexPoint;
    private float timePastWallJumpApexThreshold;
    private bool isPastWallJumpApexThreshold;

    // Buffer variables
    private float jumpBufferTimer;
    private bool jumpReleasedDuringBuffer;
    private float wallJumpPostBufferTimer;

    // Coyote time variables
    private float coyoteTimer;

    private AnimationData IDLE = new(Animations.IDLE);
    private AnimationData RUN = new(Animations.RUN);
    private AnimationData JUMP = new(Animations.JUMP);
    private AnimationData DOUBLEJUMP = new(Animations.DOUBLEJUMP);
    private AnimationData WALLJUMP = new(Animations.WALLJUMP);
    private AnimationData FALL = new(Animations.FALL);

    public override void DefaultAnimation(int layer)
    {
        throw new System.NotImplementedException();
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        isFacingRight = true;
    }

    private void Start()
    {
        Initialize();
        Play(IDLE);
    }

    private void Update()
    {
        CountTimer();
        JumpChecks();
        LandChecks();
        WallSlideCheck();
        WallJumpCheck();
        DashCheck();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();
        Fall();
        WallSlide();
        WallJump();
        Dash();

        if (isGrounded)
        {
            Move(moveStats.GroundAcceleration, moveStats.GroundDeceleration, InputReader.Instance.moveInput);
        }
        else
        {
            if (useWallJumpMoveStats)
            {
                Move(moveStats.WallJumpMoveAcceleration, moveStats.WallJumpMoveDeceleration, InputReader.Instance.moveInput);
            }
            else
            {
                Move(moveStats.AirAcceleration, moveStats.AirDeceleration, InputReader.Instance.moveInput);
            } 
        }

        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        if (!isDashing)
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -moveStats.MaxFallSpeed, 50f);
        }
        else
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -100f, 100f);
        }

        rb.velocity = new Vector2(horizontalVelocity, VerticalVelocity);
    }

    #region Movement

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (isDashing)
        {
            return;
        }

       if(moveInput != Vector2.zero)
        {
            TurnCheck(moveInput);

            float targetVelocity = 0f;
            if (InputReader.Instance._runHeld)
            {
                targetVelocity = moveInput.x * moveStats.MaxRunSpeed;
            }
            else
            {
                targetVelocity = moveInput.x * moveStats.MaxWalkSpeed;
            }

            horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            Play(RUN);
        }

       if(moveInput == Vector2.zero)
        {
            horizontalVelocity = Mathf.Lerp(horizontalVelocity, 0f, deceleration * Time.fixedDeltaTime);
            Play(IDLE);
        }
        
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if(isFacingRight && moveInput.x < 0)
        {
            Turn(false);
        }
        else if(!isFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            isFacingRight = true;
            transform.Rotate(0f, 180f, 0f);
        }
        else 
        {
            isFacingRight = false;
            transform.Rotate(0f,-180f, 0f);
        }
    }

    #endregion

    #region Jump

    private void JumpChecks()
    {
        //when we press the jump button
        if (InputReader.Instance._jumpPressed)
        {
            if(isWallSlideFalling && wallJumpPostBufferTimer >= 0f) { return; }
            if(isWallSliding || (isTouchingWall && !isGrounded)) { return; }

            jumpBufferTimer = moveStats.JumpBufferTime;
            jumpReleasedDuringBuffer = false;
        }

        //when we release the jump button
        if (!InputReader.Instance._jumpHeld)
        {
            if(jumpBufferTimer > 0)
            {
                jumpReleasedDuringBuffer = true;
            }

            if(isJumping && VerticalVelocity > 0f)
            {
                if (isPastApexThreshold)
                {
                    isPastApexThreshold = false;
                    isFastFalling = true;
                    fastFallTime = moveStats.TimeForUpwardsCancle;
                    VerticalVelocity = 0f;
                }
                else
                {
                    isFastFalling = true;
                    fastFallReleaseSpeed = VerticalVelocity;
                }
                
            }
        }

        //initiate jump with jump buffering and coyote time
        if (jumpBufferTimer > 0 && !isJumping && (isGrounded || coyoteTimer > 0f))
        {
            InitiateJump(1);
            if (jumpReleasedDuringBuffer)
            {
                isFastFalling = true;
                fastFallReleaseSpeed = VerticalVelocity;
            }

        }

        //double jump
         if (jumpBufferTimer > 0 && (isJumping || isWallJumping || isWallSlideFalling || isAirDashing || isDashFastFalling) && !isTouchingWall && numberOfJumpsUsed < moveStats.NumberOfJumpsAllowed)
        {
            isFastFalling = false;
            InitiateJump(1);

            if (isDashFastFalling)
            {
                isDashFastFalling = false;
            }
        }

        //air jump after coyote time lapsed
        if (jumpBufferTimer > 0 && isFalling && !isWallSlideFalling && numberOfJumpsUsed < moveStats.NumberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            isFastFalling = false;
        }
            
    }

    private void InitiateJump(int numberOfJumpsUsed)
    {
        if (!isJumping)
        {
            isJumping = true;
            Play(JUMP);
        }

        ResetWallJumpValues();

        jumpBufferTimer = 0f;
        this.numberOfJumpsUsed += numberOfJumpsUsed;
        VerticalVelocity = moveStats.InitialJumpVelocity;
    }

    private void Jump()
    {
        //apply gravity while jumping
        if (isJumping)
        {
            
            //check for head bumped
            if (bumpedHead)
            {
                isFastFalling = true;
            }

            //gravity ascending
            if(VerticalVelocity >= 0f)
            {
                //apex controls
                apexPoint = Mathf.InverseLerp(moveStats.InitialJumpVelocity, 0f, VerticalVelocity);
                if(apexPoint > moveStats.ApexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePastApexThreshold = 0f;

                    }

                    if (isPastApexThreshold)
                    {
                        timePastApexThreshold += Time.fixedDeltaTime;
                        if (timePastApexThreshold < moveStats.ApexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }
                    }
                }
                else if(!isFastFalling)
                {
                    VerticalVelocity += moveStats.Gravity * Time.fixedDeltaTime;
                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;
                    }
                }
                
            }

            else if (!isFastFalling)
            {
                VerticalVelocity += moveStats.Gravity * moveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if(VerticalVelocity < 0f)
            {
                if (!isFalling)
                {
                    isFalling = true; 
                }
            }
        }

        if (isFastFalling)
        {
            if(fastFallTime >= moveStats.TimeForUpwardsCancle)
            {
                VerticalVelocity += moveStats.Gravity * moveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if ( fastFallTime < moveStats.TimeForUpwardsCancle)
            {
                VerticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / moveStats.TimeForUpwardsCancle));
            }
            fastFallTime += Time.fixedDeltaTime;
        }

    }

    private void ResetJumpValues()
    {
        isJumping = false;
        isFalling = false;
        isFastFalling = false;
        fastFallTime = 0f;
        isPastApexThreshold = false;
    }

    #endregion

    #region Wall Jump

    private void WallJumpCheck()
    {
        if (ShouldApplyWallJumpBuffer())
        {
            wallJumpPostBufferTimer = moveStats.WallJumpPostBufferTime;
        }

        if(!InputReader.Instance._jumpHeld && !isWallSliding && !isTouchingWall && isWallJumping)
        {
            if(VerticalVelocity > 0f)
            {
                if (isPastWallJumpApexThreshold)
                {
                    isPastWallJumpApexThreshold = false;
                    isWallJumpFastFalling = true;
                    wallJumpFastFallTime = moveStats.TimeForUpwardsCancle;

                    VerticalVelocity = 0f;
                }

                else 
                {
                    isWallJumpFastFalling = true;
                    wallJumpFastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }
        if(InputReader.Instance._jumpPressed && wallJumpPostBufferTimer > 0)
        {
            InitiateWallJump();
        }
    }

    private void InitiateWallJump()
    {
        if (!isWallJumping)
        {
            isWallJumping = true;
            useWallJumpMoveStats = true;
        }
        StopWallSlide();
        ResetJumpValues();
        wallJumpTime = 0f;

        VerticalVelocity = moveStats.InitialWallJumpVelocity;

        int directionMultiplier = 0;
        Vector2 hitPoint = lastWallHit.collider.ClosestPoint(bodyColl.bounds.center);

        if (hitPoint.x > transform.position.x)
        {
            directionMultiplier = -1;
        }
        else { directionMultiplier = 1; }

        horizontalVelocity = Mathf.Abs(moveStats.WallJumpDirection.x) * directionMultiplier;
    }

    private void WallJump()
    {
        if (isWallJumping)
        {
            wallJumpTime += Time.deltaTime;
            if (wallJumpTime >= moveStats.TimeTillJumpApex)
            {
                useWallJumpMoveStats = false;
            }

            if (bumpedHead)
            {
                isWallJumpFastFalling = true;
                useWallJumpMoveStats = false;

            }

            if(VerticalVelocity >= 0f)
            {
                wallJumpApexPoint = Mathf.InverseLerp(moveStats.WallJumpDirection.y, 0f, VerticalVelocity);

                if(wallJumpApexPoint > moveStats.ApexThreshold)
                {
                    if (!isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold = true;
                        timePastWallJumpApexThreshold = 0f;
                    }

                    if (isPastWallJumpApexThreshold)
                    {
                        timePastWallJumpApexThreshold += Time.fixedDeltaTime;
                        if (timePastWallJumpApexThreshold < moveStats.ApexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else { VerticalVelocity = -0.01f; }
                    }
                }

                else if (!isWallJumpFastFalling)
                {
                    VerticalVelocity += moveStats.WallJumpGravity * Time.fixedDeltaTime;

                    if (isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold = false;
                    }
                }
            }

            else if (!isWallJumpFalling)
            {
                VerticalVelocity += moveStats.WallJumpGravity * Time.fixedDeltaTime;
            }

            else if( VerticalVelocity < 0f)
            {
                if (!isWallJumpFalling)
                {
                    isWallJumpFalling = true;
                }
            }
        }

        if (isWallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= moveStats.TimeForUpwardsCancle)
            {
                VerticalVelocity += moveStats.WallJumpGravity * moveStats.WallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if(wallJumpFastFallTime < moveStats.TimeForUpwardsCancle)
            {
                VerticalVelocity = Mathf.Lerp(wallJumpFastFallReleaseSpeed, 0f, (wallJumpFastFallReleaseSpeed / moveStats.TimeForUpwardsCancle));
            }

            wallJumpFastFallTime += Time.fixedDeltaTime;
        }
    }

    private bool ShouldApplyWallJumpBuffer()
    {
        if (!isGrounded && (isTouchingWall || isWallSliding))
        { 
            return true;
        }
        else { return false; }
    }

    private void ResetWallJumpValues()
    {
        isWallSlideFalling = false;
        useWallJumpMoveStats = false;
        isWallJumpFastFalling = false;
        isWallJumping = false;
        isWallJumpFalling = false;
        isPastWallJumpApexThreshold = false;
        wallJumpFastFallTime = 0f;
        wallJumpTime = 0f;
    }

    #endregion

    #region Wall Slide

    private void WallSlideCheck()
    {
        if(isTouchingWall && !isGrounded && !isDashing)
        {
            if(VerticalVelocity < 0f && !isWallSliding)
            {
                ResetJumpValues();
                ResetWallJumpValues();
                ResetDashValues();

                if (moveStats.ResetDashOnWallSlide)
                {
                    ResetDashes();
                }

                isWallSlideFalling = false;
                isWallSliding = true;

                if (moveStats.ResetJumpsOnWallSlide)
                {
                    numberOfJumpsUsed = 0;
                }
            }
        }

        else if(isWallSliding && !isTouchingWall && !isGrounded && !isWallSlideFalling)
        {
            isWallSlideFalling = true;
            StopWallSlide();
        }

        else { StopWallSlide(); }
    }

    private void WallSlide()
    {
        if (isWallSliding)
        {
            VerticalVelocity = Mathf.Lerp(VerticalVelocity, -moveStats.WallSlideSpeed, moveStats.WallSlideDecelerationSpeed * Time.fixedDeltaTime);
        }
    }

    private void StopWallSlide()
    {
        if (isWallSliding)
        {
            numberOfJumpsUsed++;
            isWallSliding = false;
        }
    }

    #endregion

    #region Dash

    private void DashCheck()
    {
        if (InputReader.Instance._dashPressed)
        {
            // ground dash
            if(isGrounded && dashOnGroundTimer < 0 && !isDashing)
            {
                InitiateDash();
            }

            // air dash
            else if(!isDashing && !isGrounded && numberOfDashesUsed < moveStats.NumberOfDashes)
            {
                isAirDashing = true;
                InitiateDash();

                if(wallJumpPostBufferTimer > 0f)
                {
                    numberOfJumpsUsed--;
                    if(numberOfJumpsUsed < 0f)
                    {
                        numberOfJumpsUsed = 0;
                    }
                }
            }
        }
    }

    private void InitiateDash()
    {
        dashDirection = InputReader.Instance.moveInput;

        Vector2 closestDirection = Vector2.zero;
        float minDistance = Vector2.Distance(dashDirection, moveStats.DashDirections[0]);

        for (int i = 0; i < moveStats.DashDirections.Length; i++)
        {
            if(dashDirection == moveStats.DashDirections[i])
            {
                closestDirection = dashDirection;
                break;
            }

            float distance = Vector2.Distance(dashDirection, moveStats.DashDirections[i]);

            bool isDiagonal = (Mathf.Abs(moveStats.DashDirections[i].x) == 1 && Mathf.Abs(moveStats.DashDirections[i].y) == 1);

            if (isDiagonal)
            {
                distance -= moveStats.DashDiaganollyBias;
            }
            else if(distance < minDistance) 
            { 
                minDistance = distance;
                closestDirection = moveStats.DashDirections[i];
            }
        }

        if(closestDirection == Vector2.zero)
        {
            if (isFacingRight)
            {
                closestDirection = Vector2.right;
            }
            else { closestDirection = Vector2.left; }
        }

        dashDirection = closestDirection;
        numberOfDashesUsed++;
        isDashing = true;
        dashTimer = 0f;
        dashOnGroundTimer = moveStats.TimeBetweenGroundDashes;

        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();
    }

    private void Dash()
    {
        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;

            if (dashTimer >= moveStats.DashTime)
            {
                if (isGrounded)
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;  

                if(!isJumping && isWallJumping)
                {
                    dashFastFallTime = 0f;
                    dashFastFallReleaseSpeed = VerticalVelocity;

                    if (!isGrounded)
                    {
                        isDashFastFalling = true;
                    }
                }

                return;
            }


            horizontalVelocity = moveStats.DashSpeed * dashDirection.x;

            if(dashDirection.y != 0f || isAirDashing)
            {
                VerticalVelocity = moveStats.DashSpeed * dashDirection.y;
            }
        }

        else if (isDashFastFalling)
        {

            if(VerticalVelocity < 0f)
            {
                if(dashFastFallTime < moveStats.DashTimeForUpwardsCancel)
                {
                    VerticalVelocity = Mathf.Lerp(dashFastFallReleaseSpeed, 0f, (dashFastFallTime / moveStats.DashTimeForUpwardsCancel));
                }
                else if (dashFastFallTime >= moveStats.DashTimeForUpwardsCancel)
                {
                    VerticalVelocity += moveStats.Gravity * moveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }

                dashFastFallTime += Time.fixedDeltaTime;
            }

            else
            {
                VerticalVelocity += moveStats.Gravity * moveStats.DashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
        }
    }

    private void ResetDashValues()
    {
        isDashFastFalling = false;
        dashOnGroundTimer = -0.01f;
    }

    private void ResetDashes()
    {
        numberOfDashesUsed = 0; 
    }

    #endregion

    #region Land/Fall

    public void Fall()
    {
        if (!isGrounded && !isJumping && !isWallSliding && !isWallJumping && !isDashing && !isDashFastFalling)
        {
            if (!isFalling)
            {
                isFalling = true;
            }

            VerticalVelocity += moveStats.Gravity * Time.fixedDeltaTime;
        }
    }

    public void LandChecks()
    {
        //landed
        if ((isJumping || isFalling || isWallJumpFalling || isWallJumping || isWallSlideFalling || isWallSliding || isDashFastFalling) && isGrounded && VerticalVelocity <= 0f)
        {
            ResetJumpValues();
            StopWallSlide();
            ResetWallJumpValues();
            ResetDashes();

            numberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;

            ResetDashValues();
        }
    }

    #endregion

    #region Collisions
    private void CollisionChecks()
    {
        IsGrounded();
        BumpedHead();
        IsTouchingWall();
    }

    private void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(feetColl.bounds.center.x, feetColl.bounds.min.y);
        Vector2 boxCastSize = new Vector2(feetColl.bounds.size.x, moveStats.GroundDetectionRayLength);

        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, moveStats.GroundDetectionRayLength, moveStats.GroundLayer);
        if(groundHit.collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        if (moveStats.DebugShowIsGroundedBox)
        {

            Color rayColor;
            if (isGrounded)
            {
                rayColor = Color.red;
            }
            else { rayColor = Color.green; }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * moveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + (boxCastSize.x / 2), boxCastOrigin.y), Vector2.down * moveStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - moveStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
        }

    }

    private void BumpedHead()
    {
        Vector2 boxCastOrigin = new Vector2(bodyColl.bounds.center.x, bodyColl.bounds.center.y);
        Vector2 boxCastSize = new Vector2(bodyColl.bounds.size.x * moveStats.HeadWidth, moveStats.HeadDetectionRayLength);

        headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, moveStats.HeadDetectionRayLength, moveStats.GroundLayer);
        if(headHit.collider != null)
        {
            bumpedHead = true;
        }
        else { bumpedHead= false; }

        if (moveStats.DebugShowHeadBumpBox)
        {
            float headwidth = moveStats.HeadWidth;

            Color rayColor;
            if (bumpedHead)
            {
                rayColor = Color.red;
            } else { rayColor = Color.green; }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headwidth, boxCastOrigin.y), Vector2.up * moveStats.HeadDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + (boxCastSize.x / 2) * headwidth, boxCastOrigin.y), Vector2.up * moveStats.HeadDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headwidth, boxCastOrigin.y + moveStats.HeadDetectionRayLength), Vector2.right * boxCastSize.x * headwidth, rayColor);
        }
    }

    private void IsTouchingWall()
    {
        float originEndPoint = 0f;

        if (isFacingRight)
        {
            originEndPoint = bodyColl.bounds.max.x;
        }
        else { originEndPoint = bodyColl.bounds.min.x; }

        float adjustedHeight = bodyColl.bounds.size.y * moveStats.WallDetectionRayHeightMultiplier;

        Vector2 boxCastOrigin = new Vector2(originEndPoint, bodyColl.bounds.center.y);
        Vector2 boxCastSize = new Vector2(moveStats.WallDetectioRayLength, adjustedHeight);

        wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, moveStats.WallDetectioRayLength, moveStats.WallLayer);

        if (wallHit.collider != null)
        {
            lastWallHit = wallHit;
            isTouchingWall = true;
        }
        else
        {
            isTouchingWall = false;
        }

        // Debug Visuals
        if (moveStats.DebugShowWallHitBox)
        {
            Color rayColour;
            if (isTouchingWall)
            {
                rayColour = Color.green;
            }
            else { rayColour = Color.red; }

            Vector2 boxBottomLeft = new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxBottomRight = new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y - boxCastSize.y / 2);
            Vector2 boxTopLeft = new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);
            Vector2 boxTopRight = new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y + boxCastSize.y / 2);

            Debug.DrawLine(boxBottomLeft, boxBottomRight, rayColour);
            Debug.DrawLine(boxBottomRight, boxTopRight, rayColour);
            Debug.DrawLine(boxTopRight, boxTopLeft, rayColour);
            Debug.DrawLine(boxTopLeft, boxBottomLeft, rayColour);
        }
    }
    #endregion

    #region Timer

    private void CountTimer()
    {
        jumpBufferTimer -= Time.deltaTime;

        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else
        {
            coyoteTimer = moveStats.JumpCoyoteTime;
        }

        if (!ShouldApplyWallJumpBuffer())
        {
            wallJumpPostBufferTimer -= Time.deltaTime;
        }

        if (isGrounded)
        {
            dashOnGroundTimer -= Time.deltaTime;
        }
    }

    #endregion

}
