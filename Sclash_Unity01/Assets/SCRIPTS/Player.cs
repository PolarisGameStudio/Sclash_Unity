﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Events

    public delegate void OnDrawnEvent();
    public event OnDrawnEvent DrawnEvent;


    #endregion


    #region VARIABLES
    #region MANAGERS
    [Header("MANAGERS")]
    // Audio manager
    [Tooltip("The name of the object in the scene containing the AudioManager script component, to find its reference")]
    [SerializeField] string audioManagerName = "GlobalManager";
    AudioManager audioManager;

    // Game manager
    [Tooltip("The name of the object in the scene containing the GlobalManager script component, to find its reference")]
    [SerializeField] string gameManagerName = "GlobalManager";
    GameManager gameManager;

    // Input manager
    [Tooltip("The name of the object in the scene containing the InputManager script component, to find its reference")]
    [SerializeField] string inputManagerName = "GlobalManager";

    [SerializeField] protected InputManager inputManager = null;

    [SerializeField] string statsManagerName = "GlobalManager";
    StatsManager statsManager = null;
    #endregion





    #region PLAYER'S COMPONENTS
    [Header("PLAYER'S COMPONENTS")]
    public Rigidbody2D rb = null;
    public PlayerAnimations playerAnimations = null;
    public CharacterChanger characterChanger = null;
    public IAChanger iaChanger = null;
    [Tooltip("The basic collider of the player")]
    public Collider2D playerCollider;
    [Tooltip("All of the player's 2D colliders")]
    public Collider2D[] playerColliders = null;
    public SpriteRenderer spriteRenderer = null;
    [Tooltip("The reference to the light component which lits the player with their color")]
    public Light playerLight = null;
    #endregion






    #region PLAYER STATES
    // PLAYER STATES
    [Header("PLAYER STATES")]
    [SerializeField] public STATE playerState = STATE.normal;

    public enum STATE
    {
        frozen,
        sneathing,
        sneathed,
        drawing,
        normal,
        charging,
        attacking,
        pommeling,
        parrying,
        maintainParrying,
        preparingToJump,
        jumping,
        dashing,
        recovering,
        clashed,
        enemyKilled,
        enemyKilledEndMatch,
        dead,
    }


    [HideInInspector] public STATE oldState = STATE.normal;

    [SerializeField] bool hasFinishedAnim = false;
    #endregion






    #region PLAYERS IDENTIFICATION
    [Header("PLAYERS IDENTIFICATION")]

    [SerializeField] public Text characterNameDisplay = null;
    [SerializeField] public Image characterIdentificationArrow = null;
    [HideInInspector] public int characterIndex = 0;
    public int playerNum = 0;
    int otherPlayerNum = 0;
    [HideInInspector] public int networkPlayerNum = 0;
    public bool playerIsAI;
    Player opponent;
    #endregion






    #region HEALTH
    // HEALTH
    [Header("HEALTH")]
    [Tooltip("The maximum health of the player")]
    [SerializeField] float maxHealth = 1;
    float currentHealth;

    [Tooltip("Can the player be hit in the current frames ?")]
    [SerializeField] public bool untouchableFrame = false;

    [Tooltip("The opacity amount of the player's sprite when in untouchable frames")]
    [SerializeField] float untouchableFrameOpacity = 0.3f;
    #endregion






    #region STAMINA
    //STAMINA
    [Header("STAMINA")]
    [Tooltip("The reference to the base stamina slider attached to the player to create the other sliders")]
    [SerializeField] public Slider staminaSlider = null;
    List<Slider> staminaSliders = new List<Slider>();

    [Tooltip("The amount of stamina each move will cost when executed")]
    [SerializeField] public float staminaCostForMoves = 1;
    [Tooltip("The maximum amount of stamina one player can have")]
    [SerializeField] public float maxStamina = 4f;
    [Tooltip("Stamina parameters")]
    [SerializeField]
    float
        durationBeforeStaminaRegen = 0.5f,
        staminaGlobalGainOverTimeMultiplier = 1f,
        idleStaminaGainOverTimeMultiplier = 0.8f,
        backWalkingStaminaGainOverTime = 0.8f,
        frontWalkingStaminaGainOverTime = 0.4f,
        quickStaminaRegenGap = 1,
        lowStaminaGap = 1,
        idleQuickStaminaGainOverTimeMultiplier = 1.2f,
        backWalkingQuickStaminaGainOverTime = 1.2f,
        frontWalkingQuickStaminaGainOverTime = 0.8f,
        staminaBarBaseOpacity = 0.8f,
        staminaRecupTriggerDelay = 0.35f,
        staminaRecupAnimRegenSpeed = 0.025f;
    [HideInInspector] public float stamina = 0;
    float
        currentTimeBeforeStaminaRegen = 0,
        staminaBarsOpacity = 1,
        oldStamina = 0;


    [HideInInspector] public bool canRegenStamina = true;
    bool
        hasReachedLowStamina = false,
        staminaRecupAnimOn = false,
        staminaBreakAnimOn = false;

    [Header("STAMINA COLORS")]
    [Tooltip("Stamina colors depending on how much there is left")]
    [SerializeField] Color staminaBaseColor = Color.green;
    [SerializeField]
    Color
        staminaLowColor = Color.yellow,
        staminaDeadColor = Color.red,
        staminaRecupColor = Color.blue,
        staminaBreakColor = Color.red;
    #endregion






    #region MOVEMENTS
    // MOVEMENTS
    [Header("MOVEMENTS")]
    [Tooltip("The default movement speed of the player")]
    [SerializeField] float baseMovementsSpeed = 2.5f;
    [SerializeField]
    float
        chargeMovementsSpeed = 1.2f,
        sneathedMovementsSpeed = 1.8f,
        attackingMovementsSpeed = 2.2f;
    public float actualMovementsSpeed = 1;

    Vector3 oldPos = Vector3.zero;
    Vector2 netTargetPos = Vector2.zero;
    float lerpValue = 0f;
    bool lerpToTarget = false;
    #endregion






    #region ORIENTATION
    // ORIENTATION
    [Header("ORIENTATION")]
    [Tooltip("The duration before the player can orient again towards the enemy if they need to once they applied the orientation")]
    [SerializeField] protected float orientationCooldown = 0.1f;
    protected float
         orientationCooldownStartTime = 0;

    protected bool
        orientationCooldownFinished = true,
        canOrientTowardsEnemy = true;
    #endregion






    #region ACTIONS & RULES
    [Header("ACTIONS & RULES")]
    [SerializeField] protected bool canJump = true;
    [SerializeField] protected bool canMaintainParry = true;
    [SerializeField] protected bool canBriefParry = false;
    [SerializeField] protected bool quickRegen = false;
    [SerializeField] protected bool quickRegenOnlyWhenReachedLowStaminaGap = true;
    #endregion






    #region FRAMES
    [Header("FRAMES")]
    [Tooltip("Only editable by animator, is currently in parry frames state")]
    [SerializeField] public bool parryFrame = false;
    [SerializeField]
    public bool
        perfectParryFrame = false,
        activeFrame = false,
        clashFrames = false;
    #endregion






    #region DRAW
    // DRAW
    [Header("DRAW")]
    [Tooltip("The reference to the game object containing the text component telling the players to draw their sabers")]
    [SerializeField] public GameObject drawText = null;
    Vector3 drawTextBaseScale = new Vector3(0, 0, 0);
    #endregion






    #region JUMP
    // JUMP
    [Header("JUMP")]
    [SerializeField]
    float
        jumpPower = 10f;
    #endregion






    #region CHARGE
    // CHARGE
    [Header("CHARGE")]
    [Tooltip("The number of charge levels for the attack, so the number of range subdivisions")]
    [SerializeField] public int maxChargeLevel = 4;
    [HideInInspector] public int chargeLevel = 1;

    [Tooltip("Charge duration parameters")]
    [SerializeField]
    float
        durationToNextChargeLevel = 0.7f,
        maxHoldDurationAtMaxCharge = 2f;
    [SerializeField] float attackReleaseAxisInputDeadZoneForDashAttack = 0.1f;
    protected float
        maxChargeLevelStartTime = 0,
        chargeStartTime = 0;

    [HideInInspector] public bool canCharge = true;
    #endregion






    #region ATTACK
    // ATTACK
    [Header("ATTACK")]
    [Tooltip("Attack range parameters")]
    [SerializeField] public float lightAttackRange = 1.8f;
    [Tooltip("Attack range parameters")]
    [SerializeField]
    public float
        heavyAttackRange = 3.2f,
        baseBackAttackRangeDisjoint = 0f,
        forwardAttackBackrangeDisjoint = 2.5f;
    [SerializeField] float axisDeadZoneForAttackDash = 0.2f;
    [HideInInspector] public float actualAttackRange = 0;
    float actualBackAttackRangeDisjoint = 0f;

    [Tooltip("Frame parameters for the attack")]

    [HideInInspector] public bool isAttacking = false;

    List<GameObject> targetsHit = new List<GameObject>();

    // ATTACK RECOVERY
    [Header("ATTACK RECOVERY")]
    [SerializeField] bool hasAttackRecoveryAnimFinished = false;
    #endregion






    #region DASH
    // DASH
    [Header("DASH")]
    [SerializeField]
    public float
        baseDashSpeed = 3;
    [SerializeField]
    public float
        forwardDashDistance = 3,
        backwardsDashDistance = 2.5f;
    [SerializeField]
    protected float
        allowanceDurationForDoubleTapDash = 0.3f,
        forwardAttackDashDistance = 2.5f,
        backwardsAttackDashDistance = 1.5f,
        dashDeadZone = 0.5f,
        shortcutDashDeadZone = 0.5f;
    protected float
       dashDirection = 0,
       temporaryDashDirectionForCalculation = 0,
       dashInitializationStartTime = 0,
       actualUsedDashDistance = 0,
       dashTime = 0;

    protected enum DASHSTEP
    {
        rest,
        firstInput,
        firstRelease,
        invalidated,
    }

    protected DASHSTEP currentDashStep = DASHSTEP.invalidated;
    protected DASHSTEP currentShortcutDashStep = DASHSTEP.invalidated;

    Vector3
        initPos,
        targetPos;

    bool isDashing = false;
    #endregion






    #region POMMEL
    [Header("KICK")]
    [Tooltip("Is currently applying the pommel effect to what they touches ?")]
    [SerializeField] public bool kickFrame = false;
    [HideInInspector] public bool canPommel = true;

    [SerializeField]
    float
        kickRange = 0.88f;
    #endregion






    #region POMMELED
    [Header("KICKED")]
    [Tooltip("The distance the player will be pushed on when pommeled")]
    [SerializeField] float kickKnockbackDistance = 1f;
    #endregion






    #region PARRY
    [Header("PARRY")]
    [HideInInspector] public bool canParry = true;

    //[SerializeField] int numberOfFramesToDetectParryInput = 3;
    int currentParryFramesPressed = 0;
    #endregion





    #region MAINTAIN PARRY
    [Header("MAINTAIN PARRY")]
    [SerializeField] float maintainParryStaminaCostOverTime = 0.1f;
    #endregion





    #region CLASHED
    // CLASHED
    [Header("CLASHED")]
    [Tooltip("The distance the player will be pushed on when clashed")]
    [SerializeField] float clashKnockback = 2;
    //[SerializeField] float clashDuration = 2f;
    [Tooltip("The speed at which the knockback distance will be covered")]
    [SerializeField] public float clashKnockbackSpeed = 2;
    #endregion





    #region FX
    [Header("FX")]
    [Tooltip("The references to the game objects holding the different FXs")]
    [SerializeField] GameObject clashFXPrefabRef = null;
    [SerializeField] GameObject deathBloodFX = null;

    [Tooltip("The attack sign FX object reference, the one that spawns at the range distance before the attack hits")]
    [SerializeField] public ParticleSystem attackRangeFX = null;
    [SerializeField]
    ParticleSystem
        clashKanasFX = null,
        kickKanasFX = null,
        kickedFX = null,
        clashFX = null,
        slashFX = null;




    [SerializeField] float attackSignDisjoint = 0.4f;
    [Tooltip("The amount to rotate the death blood FX's object because for some reason it takes another rotation when it plays :/")]
    [SerializeField] float deathBloodFXRotationForDirectionChange = 240;
    [Tooltip("The width of the attack trail depending on the range of the attack")]
    [SerializeField] GameObject attackSlashFXParent = null;
    [SerializeField]
    float
        lightAttackSwordTrailWidth = 20f,
        heavyAttackSwordTrailWidth = 65f,
        lightAttackSwordTrailScale = 1,
        heavyAttackSwordTrailScale = 3;
    [Tooltip("The minimum speed required for the walk fx to trigger")]
    [SerializeField] protected float minSpeedForWalkFX = 0.05f;

    Vector3 deathFXbaseAngles = new Vector3(0, 0, 0);

    [Tooltip("The reference to the TrailRenderer component of the saber")]
    [SerializeField] TrailRenderer swordTrail = null;

    [Tooltip("The colors of the attack trail depending on the range of the attack")]
    [SerializeField]
    Color
        lightAttackColor = Color.yellow,
        heavyAttackColor = Color.red;

    [SerializeField] Gradient lightAttackGradientColor = null;

    Vector3 deathBloodFXBaseRotation = Vector3.zero;




    [Header("CHARGE FX")]
    [Tooltip("The slider component reference to move the charging FX on the katana")]
    [SerializeField] Slider chargeSlider = null;
    [SerializeField]
    protected ParticleSystem
        chargeFlareFX = null,
        chargeFX = null,
        chargeFullFX = null;





    [Header("STAMINA FX")]
    [SerializeField]
    ParticleSystem
        staminaLossFX = null;
    [SerializeField]
    ParticleSystem
        staminaGainFX = null,
        staminaRecupFX = null,
        staminaRecupFinishedFX = null,
        staminaBreakFX = null;
    #endregion





    #region STAGE DEPENDENT FX
    [Header("STAGE DEPENDENT FX")]
    [SerializeField]
    ParticleSystem
        dashFXFront = null;
    [SerializeField]
    ParticleSystem
        dashFXBack = null,
        attackDashFXFront = null,
        attackDashFXBack = null,
        attackNeutralFX = null;
    [SerializeField]
    protected ParticleSystem
        walkFXFront = null,
        walkFXBack = null;
    #endregion


    [System.Serializable]
    public struct ParticleSet
    {
        public List<GameObject> particleSystems;
    }
    [SerializeField] public List<ParticleSet> particlesSets = new List<ParticleSet>();




    #region AUDIO
    [Header("AUDIO")]
    [Tooltip("The reference to the stamina charged audio FX AudioSource")]
    [SerializeField] AudioSource staminaBarChargedAudioEffectSource = null;
    [SerializeField] AudioSource staminaBreakAudioFX = null;
    [SerializeField] PlayRandomSoundInList notEnoughStaminaSFX = null;
    #endregion









    #region CHEATS FOR DEVELOPMENT PURPOSES
    // CHEATS FOR DEVELOPMENT PURPOSES
    [Header("CHEATS")]
    [Tooltip("The cheat key to trigger a clash for the player")]
    [SerializeField] KeyCode clashCheatKey = KeyCode.Alpha1;
    [Tooltip("The other cheat keys for other effects")]
    [SerializeField]
    KeyCode
        deathCheatKey = KeyCode.Alpha2,
        staminaCheatKey = KeyCode.Alpha4,
        stopStaminaRegenCheatKey = KeyCode.Alpha6,
        triggerStaminaRecupAnim = KeyCode.Alpha7;

    [SerializeField] bool useTransparencyForDodgeFrames = true;
    [SerializeField] bool useExtraDiegeticFX = true;
    [SerializeField] bool infiniteStamina = false;
    #endregion
    #endregion


    #region Network
    bool enemyDead = false;
    #endregion

























    #region FUNCTIONS
    #region BASE FUNCTIONS
    public virtual void Start()
    {
        // GET MANAGERS
        //audioManager = GameObject.Find(audioManagerName).GetComponent<AudioManager>();
        //gameManager = GameObject.Find(gameManagerName).GetComponent<GameManager>();
        // Get input manager
        //inputManager = GameObject.Find(inputManagerName).GetComponent<InputManager>();

        // Get stats manager
        statsManager = GameObject.Find(statsManagerName).GetComponent<StatsManager>();


        actualBackAttackRangeDisjoint = baseBackAttackRangeDisjoint;
        oldStamina = maxStamina;


        // Begin by reseting all the player's values and variable to start fresh
        StartCoroutine(GetOtherPlayerNum());
        SetUpStaminaBars();
        deathFXbaseAngles = deathBloodFX.transform.localEulerAngles;
        ResetAllPlayerValuesForNextMatch();

        characterChanger.FindElements();
        StartCoroutine(characterChanger.ApplyCharacterChange());
    }

    // Update is called once per graphic frame
    public virtual void Update()
    {
        if (photonView != null && !photonView.IsMine)
        {
            UpdateStaminaSlidersValue();
            UpdateStaminaColor();

            SetStaminaBarsOpacity(staminaBarsOpacity);

            return;
        }

        if (opponent == null)
        {
            FindOpponent();
        }

        // Action depending on state
        switch (playerState)
        {
            case STATE.frozen:
                break;

            case STATE.sneathing:
                break;

            case STATE.sneathed:
                ManageOrientation();
                ManageIA();
                ManageDraw();
                break;

            case STATE.drawing:
                break;

            case STATE.normal:
                ManageJumpInput();
                ManageChargeInput();
                ManageDashInput();
                ManagePommel();
                ManageParryInput();
                ManageMaintainParryInput();

                UpdateStaminaSlidersValue();
                UpdateStaminaColor();
                break;

            case STATE.charging:
                ManageDashInput();
                ManagePommel();
                ManageParryInput();
                ManageMaintainParryInput();
                ManageCharging();
                break;

            case STATE.attacking:
                break;

            case STATE.recovering:
                break;

            case STATE.pommeling:
                ManageDashInput();
                break;

            case STATE.parrying:
                UpdateStaminaSlidersValue();
                UpdateStaminaColor();
                break;

            case STATE.maintainParrying:
                ManageMaintainParryInput();
                break;

            case STATE.preparingToJump:
                break;

            case STATE.jumping:
                break;

            case STATE.dashing:
                ManageDashInput();
                ManageChargeInput();
                ManagePommel();
                ManageParryInput();
                ManageMaintainParryInput();
                break;

            case STATE.clashed:
                RunDash();
                break;

            case STATE.enemyKilled:
                break;

            case STATE.enemyKilledEndMatch:
                break;

            case STATE.dead:
                break;
        }


        // Cheatcodes to use for development purposes
        if (GameManager.Instance.cheatCodes)
            CheatsInputs();
    }

    // FixedUpdate is called 50 times per second
    public virtual void FixedUpdate()
    {
        // ONLINE
        if (photonView != null && !photonView.IsMine)
        {
            Vector2 lagDistance = netTargetPos - rb.position;
            //Debug.Log(lagDistance);

            // HIGH DISTANCE -> TELEPORT PLAYER
            if (lagDistance.magnitude > 3f)
            {
                rb.position = netTargetPos;
                lagDistance = Vector2.zero;
            }

            if (lagDistance.magnitude < 0.11f)
            {
                // Player is nearly at the point
                rb.velocity = Vector2.zero;
            }
            else
            {
                //Player must move to the point
                if (playerState != STATE.dashing)
                    rb.velocity = new Vector2(lagDistance.normalized.x * baseMovementsSpeed, rb.velocity.y);
                else
                    rb.velocity = new Vector2(lagDistance.normalized.x * 15, rb.velocity.y);

            }

            return;
        }

        if (kickFrame)
            ApplyPommelHitbox();


        if (useTransparencyForDodgeFrames && untouchableFrame)
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, untouchableFrameOpacity);
        else
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1);

        // Behaviour depending on state
        switch (playerState)
        {
            case STATE.frozen:
                //ManageOrientation();
                SetStaminaBarsOpacity(0);
                rb.velocity = Vector2.zero;
                break;

            case STATE.sneathing:
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                if (hasFinishedAnim)
                {
                    hasFinishedAnim = false;
                    SwitchState(STATE.sneathed);
                }
                break;

            case STATE.sneathed:
                rb.velocity = new Vector2(0, rb.velocity.y);
                break;

            case STATE.drawing:
                //UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();

                if (DrawnEvent != null)
                    DrawnEvent();

                if (hasFinishedAnim)
                {
                    hasFinishedAnim = false;
                    SwitchState(STATE.normal);
                    GameManager.Instance.SaberDrawn(playerNum);
                }
                break;

            case STATE.normal:
                ManageMovementsInputs();
                ManageOrientation();
                ManageStaminaRegen();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                playerAnimations.UpdateIdleStateDependingOnStamina(stamina);
                break;

            case STATE.charging:
                ManageMovementsInputs();
                ManageStaminaRegen();
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                break;

            case STATE.attacking:
                RunDash();
                ManageMovementsInputs();
                if (hasFinishedAnim)
                {
                    hasFinishedAnim = false;
                    SwitchState(STATE.recovering);
                }
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                // Apply damages if the current attack animation has entered active frame, thus activating the bool in the animation
                if (activeFrame)
                    ApplyAttackHitbox();
                break;

            case STATE.recovering:
                if (hasAttackRecoveryAnimFinished)
                {
                    hasFinishedAnim = false;
                    SwitchState(STATE.normal);
                }
                ManageStaminaRegen();
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                RunDash();
                rb.velocity = Vector3.zero;
                break;

            case STATE.pommeling:
                RunDash();
                if (hasFinishedAnim)
                {
                    hasFinishedAnim = false;
                    SwitchState(STATE.normal);
                }
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                rb.velocity = Vector3.zero;
                break;

            case STATE.parrying:
                RunDash();
                if (hasFinishedAnim)
                {
                    hasFinishedAnim = false;
                    SwitchState(STATE.normal);
                }
                SetStaminaBarsOpacity(staminaBarsOpacity);
                rb.velocity = Vector3.zero;
                break;

            case STATE.maintainParrying:
                RunDash();
                if (hasFinishedAnim)
                    SwitchState(STATE.normal);
                StaminaCost(maintainParryStaminaCostOverTime, false);
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                break;

            case STATE.preparingToJump:
                if (hasFinishedAnim)
                    ActuallyJump();
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                break;

            case STATE.jumping:
                if (hasAttackRecoveryAnimFinished)
                    SwitchState(STATE.normal);
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                break;

            case STATE.dashing:
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();
                RunDash();
                break;

            case STATE.clashed:
                ManageStaminaRegen();
                UpdateStaminaSlidersValue();
                SetStaminaBarsOpacity(staminaBarsOpacity);
                UpdateStaminaColor();

                rb.velocity = Vector3.zero;
                break;

            case STATE.enemyKilled:
                ManageMovementsInputs();
                SetStaminaBarsOpacity(0);
                playerAnimations.UpdateIdleStateDependingOnStamina(stamina);
                break;

            case STATE.enemyKilledEndMatch:
                ManageMovementsInputs();
                SetStaminaBarsOpacity(0);
                break;

            case STATE.dead:
                break;
        }
    }
    #endregion






    #region STATE SWITCH
    public void SwitchState(STATE newState)
    {
        if (playerState != STATE.frozen)
            oldState = playerState;


        playerState = newState;

        switch (newState)
        {
            case STATE.frozen:
                SetStaminaBarsOpacity(0);
                attackDashFXFront.Stop();
                attackDashFXBack.Stop();
                dashFXBack.Stop();
                dashFXFront.Stop();
                if (characterChanger != null)
                    characterChanger.enabled = false;
                break;

            case STATE.sneathing:
                rb.velocity = Vector3.zero;
                playerAnimations.TriggerSneath();
                break;

            case STATE.sneathed:
                drawText.SetActive(true);
                staminaBarsOpacity = 0;
                actualMovementsSpeed = sneathedMovementsSpeed;
                rb.simulated = true;
                if (characterChanger != null)
                    characterChanger.enabled = true;

                if (playerNum == 1)
                    iaChanger.enabled = true;
                break;

            case STATE.drawing:
                rb.velocity = Vector3.zero;
                if (characterChanger != null)
                    characterChanger.enabled = false;

                if (playerNum == 0)
                    if (!ConnectManager.Instance.connectedToMaster)
                        if (GameManager.Instance.playersList.Count > 1)
                            GameManager.Instance.playersList[1].GetComponent<IAChanger>().enabled = false;

                break;

            case STATE.normal:
                actualMovementsSpeed = baseMovementsSpeed;
                dashTime = 0;
                isDashing = false;

                playerCollider.isTrigger = false;
                for (int i = 0; i < playerColliders.Length; i++)
                    playerColliders[i].isTrigger = false;
                attackDashFXFront.Stop();
                attackDashFXBack.Stop();
                dashFXBack.Stop();
                dashFXFront.Stop();
                break;

            case STATE.charging:
                isDashing = false;
                chargeLevel = 1;
                chargeStartTime = Time.time;
                actualMovementsSpeed = chargeMovementsSpeed;
                dashFXBack.Stop();
                dashFXFront.Stop();
                chargeFlareFX.gameObject.SetActive(true);
                playerCollider.isTrigger = false;
                for (int i = 0; i < playerColliders.Length; i++)
                    playerColliders[i].isTrigger = false;
                break;

            case STATE.attacking:
                isDashing = true;
                canCharge = false;
                chargeLevel = 1;
                chargeSlider.value = 1;
                actualMovementsSpeed = attackingMovementsSpeed;
                playerCollider.isTrigger = true;
                for (int i = 0; i < playerColliders.Length; i++)
                    playerColliders[i].isTrigger = true;
                PauseStaminaRegen();

                chargeFlareFX.gameObject.SetActive(false);
                chargeFlareFX.gameObject.SetActive(true);

                break;

            case STATE.pommeling:
                chargeLevel = 1;
                rb.velocity = Vector3.zero;
                PauseStaminaRegen();
                chargeFlareFX.gameObject.SetActive(false);
                chargeFlareFX.gameObject.SetActive(true);
                break;

            case STATE.parrying:
                chargeLevel = 1;
                canParry = false;
                PauseStaminaRegen();
                rb.velocity = Vector3.zero;
                dashFXBack.Stop();
                dashFXFront.Stop();
                chargeFlareFX.gameObject.SetActive(false);
                chargeFlareFX.gameObject.SetActive(true);
                break;

            case STATE.maintainParrying:
                chargeLevel = 1;
                rb.velocity = Vector3.zero;
                PauseStaminaRegen();
                dashFXBack.Stop();
                dashFXFront.Stop();
                chargeFlareFX.gameObject.SetActive(false);
                chargeFlareFX.gameObject.SetActive(true);
                break;

            case STATE.preparingToJump:
                rb.velocity = new Vector2(0, rb.velocity.y);
                walkFXBack.Stop();
                walkFXFront.Stop();
                break;

            case STATE.jumping:
                PauseStaminaRegen();
                rb.velocity = new Vector2(0, rb.velocity.y);
                break;

            case STATE.dashing:
                canCharge = false;
                currentDashStep = DASHSTEP.invalidated;
                currentShortcutDashStep = DASHSTEP.invalidated;
                chargeLevel = 1;
                playerCollider.isTrigger = true;
                isDashing = true;
                for (int i = 0; i < playerColliders.Length; i++)
                    playerColliders[i].isTrigger = true;
                PauseStaminaRegen();
                chargeFlareFX.gameObject.SetActive(false);
                break;

            case STATE.recovering:
                rb.velocity = Vector3.zero;
                break;

            case STATE.clashed:
                chargeLevel = 1;
                playerCollider.isTrigger = true;
                for (int i = 0; i < playerColliders.Length; i++)
                    playerColliders[i].isTrigger = true;

                PauseStaminaRegen();
                attackDashFXFront.Stop();
                attackDashFXBack.Stop();
                dashFXBack.Stop();
                dashFXFront.Stop();
                chargeFlareFX.gameObject.SetActive(false);

                attackRangeFX.gameObject.SetActive(false);
                attackRangeFX.gameObject.SetActive(true);
                break;

            case STATE.enemyKilled:
                SetStaminaBarsOpacity(0);
                stamina = maxStamina;
                attackDashFXFront.Stop();
                attackDashFXBack.Stop();
                dashFXBack.Stop();
                dashFXFront.Stop();
                break;

            case STATE.dead:
                rb.velocity = new Vector2(0, rb.velocity.y);
                SetStaminaBarsOpacity(0);
                playerCollider.isTrigger = true;
                for (int i = 0; i < playerColliders.Length; i++)
                    playerColliders[i].isTrigger = true;

                chargeFlareFX.gameObject.SetActive(false);
                chargeFlareFX.gameObject.SetActive(true);
                attackDashFXFront.Stop();
                attackDashFXBack.Stop();
                walkFXBack.Stop();
                walkFXFront.Stop();
                dashFXBack.Stop();
                dashFXFront.Stop();
                drawText.SetActive(false);
                if (characterChanger != null)
                    characterChanger.enabled = false;
                break;
        }
    }
    #endregion







    #region PLAYERS
    // PLAYERS

    IEnumerator GetOtherPlayerNum()
    {
        yield return new WaitForSeconds(0.2f);


        for (int i = 0; i < GameManager.Instance.playersList.Count; i++)
        {
            if (i != playerNum)
            {
                otherPlayerNum = i;
            }
        }
    }

    void FindOpponent()
    {
        foreach (Player p in FindObjectsOfType<Player>())
        {
            if (p != this)
            {
                opponent = p;
                break;
            }
        }
    }

    #endregion








    #region RESET ALL VALUES
    // RESET ALL VALUES
    public void ResetAllPlayerValuesForNextMatch()
    {
        SwitchState(Player.STATE.frozen);


        currentHealth = maxHealth;
        stamina = maxStamina;
        staminaSlider.gameObject.SetActive(true);
        canRegenStamina = true;
        chargeLevel = 1;


        // Restablishes physics
        //rb.gravityScale = 1;
        rb.simulated = true;


        // Restablishes colliders
        playerCollider.isTrigger = false;


        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColliders[i].isTrigger = false;
        }


        // ANIMATIONS
        playerAnimations.CancelCharge(true);
        playerAnimations.ResetAnimsForNextMatch();
        playerAnimations.ResetDrawText();
    }


    [PunRPC]
    public void ResetAllPlayerValuesForNextRound()
    {
        SwitchState(STATE.normal);


        currentHealth = maxHealth;
        stamina = maxStamina;
        staminaSlider.gameObject.SetActive(true);
        canRegenStamina = true;
        chargeLevel = 1;


        // Restablishes physics
        //rb.gravityScale = 1;
        rb.simulated = true;


        // Restablishes colliders
        playerCollider.isTrigger = false;


        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColliders[i].isTrigger = false;
        }


        // ANIMATIONS
        playerAnimations.CancelCharge(true);
        playerAnimations.ResetAnimsForNextRound();
    }
    #endregion








    #region RECEIVE AN ATTACK
    public bool TakeDamage(GameObject instigator, int hitStrength = 1)
    {
        bool hit = false;

        if (playerState != STATE.dead)
        {
            if (Mathf.Sign(instigator.transform.localScale.x) == Mathf.Sign(transform.localScale.x))
            {
                hit = true;
                if (ConnectManager.Instance.connectedToMaster)
                    photonView.RPC("TriggerHit", RpcTarget.AllViaServer);
                else
                    TriggerHit();


                // SOUND
                AudioManager.Instance.BattleEventIncreaseIntensity();
            }
            // CLASH
            else if (clashFrames)
            {
                foreach (GameObject p in GameManager.Instance.playersList)
                {
                    if (ConnectManager.Instance.connectedToMaster)
                        p.GetComponent<PhotonView>().RPC("TriggerClash", RpcTarget.AllViaServer);
                    else
                        p.GetComponent<Player>().TriggerClash();
                }

                // FX
                Vector3 fxPos = new Vector3((GameManager.Instance.playersList[0].transform.position.x + GameManager.Instance.playersList[1].transform.position.x) / 2, clashFX.transform.position.y, clashFX.transform.position.z);
                Instantiate(clashFXPrefabRef, fxPos, clashFX.transform.rotation, null).GetComponent<ParticleSystem>().Play();



                // AUDIO
                AudioManager.Instance.TriggerClashAudioCoroutine();



                // STATS
                if (statsManager)
                {
                    statsManager.AddAction(ACTION.clash, playerNum, 0);
                    statsManager.AddAction(ACTION.clash, otherPlayerNum, 0);
                    Debug.Log("Clash");
                }
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }
            // PARRY
            else if (parryFrame)
            {
                // STAMINA
                //stamina += staminaCostForMoves;
                if (ConnectManager.Instance.connectedToMaster)
                    photonView.RPC("N_TriggerStaminaRecupAnim", RpcTarget.All);
                else
                    StartCoroutine(TriggerStaminaRecupAnim());


                // CLASH
                if (ConnectManager.Instance.connectedToMaster)
                    instigator.GetComponent<PhotonView>().RPC("TriggerClash", RpcTarget.AllViaServer);
                else
                    instigator.GetComponent<Player>().TriggerClash();


                // ANIMATION
                playerAnimations.TriggerPerfectParry();


                // FX
                clashFX.Play();


                // SOUND
                AudioManager.Instance.TriggerParriedAudio();


                // STATS
                if (statsManager)
                    statsManager.AddAction(ACTION.successfulParry, playerNum, 0);
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }
            // UNTOUCHABLE FRAMES
            else if (untouchableFrame)
            {
                GameManager.Instance.TriggerSlowMoCoroutine(GameManager.Instance.dodgeSlowMoDuration, GameManager.Instance.dodgeSlowMoTimeScale, GameManager.Instance.dodgeTimeScaleFadeSpeed);


                // SOUND
                AudioManager.Instance.BattleEventIncreaseIntensity();


                // STATS
                if (statsManager)
                    statsManager.AddAction(ACTION.dodge, playerNum, 0);
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }
            // TOUCHED
            else
            {
                hit = true;


                // SOUND
                if (ConnectManager.Instance.connectedToMaster)
                    photonView.RPC("TriggerHit", RpcTarget.AllViaServer);
                else
                    TriggerHit();


                // AUDIO
                AudioManager.Instance.BattleEventIncreaseIntensity();
            }

            if (ConnectManager.Instance.connectedToMaster)
                photonView.RPC("CheckDeath", RpcTarget.AllViaServer, instigator.GetComponent<Player>().playerNum);
            else
                CheckDeath(instigator.GetComponent<Player>().playerNum);
        }


        // FX
        attackRangeFX.gameObject.SetActive(false);
        attackRangeFX.gameObject.SetActive(true);


        return hit;
    }

    [PunRPC]
    public void CheckDeath(int instigatorNum)
    {
        // IS DEAD ?
        Debug.Log($"{gameObject.name} hit , {gameObject.name} health = {currentHealth}, {gameObject.name} state = {playerState}");
        if (currentHealth <= 0 && playerState != STATE.dead)
        {
            bool wasSneathed = false;


            // ASKS TO START MATCH IF SNEATHED
            if (playerState == STATE.sneathed || playerState == STATE.drawing)
                wasSneathed = true;


            // STATE
            SwitchState(STATE.dead);


            // STARTS MATCH IF PLAYER WAS SNEATHED
            if (wasSneathed)
                GameManager.Instance.SaberDrawn(playerNum);


            // HAS WON ?
            if (GameManager.Instance.score[instigatorNum] + 1 >= GameManager.Instance.scoreToWin)
            {
                GameManager.Instance.TriggerMatchEndFilterEffect(true);
                GameManager.Instance.finalCameraShake.shakeDuration = GameManager.Instance.finalCameraShakeDuration;
            }


            // ANIMATIONS
            playerAnimations.TriggerDeath();
            playerAnimations.DeathActivated(true);


            // FX
            chargeFX.Stop();


            // CAMERA FX
            GameManager.Instance.APlayerIsDead(instigatorNum);


            // STATS
            if (statsManager)
                statsManager.AddAction(ACTION.death, playerNum, 0);
            else
                Debug.Log("Couldn't access statsManager to record action, ignoring");
        }
    }

    // Hit
    [PunRPC]
    void TriggerHit()
    {
        currentHealth -= 1;


        // AUDIO
        AudioManager.Instance.TriggerSuccessfulAttackAudio();
        AudioManager.Instance.BattleEventIncreaseIntensity();


        // FX
        slashFX.Play();
        UpdateFXOrientation();


        // CAMERA FX
        GameManager.Instance.deathCameraShake.shakeDuration = GameManager.Instance.deathCameraShakeDuration;
        GameManager.Instance.TriggerSlowMoCoroutine(GameManager.Instance.roundEndSlowMoDuration, GameManager.Instance.roundEndSlowMoTimeScale, GameManager.Instance.roundEndTimeScaleFadeSpeed);
    }
    #endregion





    void ManageIA()
    {
        if (ConnectManager.Instance.connectedToMaster)
            return;

        if (InputManager.Instance.playerInputs[playerNum].switchChar && opponent.playerIsAI)
        {
            IAScript enemyIA = opponent.GetComponent<IAScript>();
            if (enemyIA != null)
            {
                switch (enemyIA.IADifficulty)
                {
                    case IAScript.Difficulty.Easy:
                        enemyIA.SetDifficulty(IAScript.Difficulty.Medium);
                        break;

                    case IAScript.Difficulty.Medium:
                        enemyIA.SetDifficulty(IAScript.Difficulty.Hard);
                        break;

                    case IAScript.Difficulty.Hard:
                        enemyIA.SetDifficulty(IAScript.Difficulty.Easy);
                        break;
                }
            }
        }
    }





    #region STAMINA
    // Set up stamina bar system
    public void SetUpStaminaBars()
    {
        staminaSliders.Add(staminaSlider);


        for (int i = 0; i < maxStamina - 1; i++)
        {
            staminaSliders.Add(Instantiate(staminaSlider.gameObject, staminaSlider.transform.parent).GetComponent<Slider>());
        }
    }

    // Manage stamina regeneration, executed in FixedUpdate
    void ManageStaminaRegen()
    {
        if (canRegenStamina && !staminaRecupAnimOn)
        {
            // Quick regen gap mode
            if (stamina < quickStaminaRegenGap && quickRegen && (!quickRegenOnlyWhenReachedLowStaminaGap || hasReachedLowStamina))
            {
                // If back walking
                if (rb.velocity.x * -transform.localScale.x < 0)
                {
                    stamina += Time.deltaTime * backWalkingQuickStaminaGainOverTime * staminaGlobalGainOverTimeMultiplier;
                }
                // If idle walking
                else if (Mathf.Abs(rb.velocity.x) <= 0.5f)
                {
                    stamina += Time.deltaTime * idleQuickStaminaGainOverTimeMultiplier * staminaGlobalGainOverTimeMultiplier;
                }
                // If front walking
                else
                {
                    stamina += Time.deltaTime * frontWalkingQuickStaminaGainOverTime * staminaGlobalGainOverTimeMultiplier;
                }
            }
            else if (stamina < maxStamina)
            {
                if (hasReachedLowStamina)
                    hasReachedLowStamina = false;

                // If back walking
                if (rb.velocity.x * -transform.localScale.x < 0)
                {
                    stamina += Time.deltaTime * backWalkingStaminaGainOverTime * staminaGlobalGainOverTimeMultiplier;
                }
                // If idle walking
                else if (Mathf.Abs(rb.velocity.x) <= 0.5f)
                {
                    stamina += Time.deltaTime * idleStaminaGainOverTimeMultiplier * staminaGlobalGainOverTimeMultiplier;
                }
                // If front walking
                else
                {
                    stamina += Time.deltaTime * frontWalkingStaminaGainOverTime * staminaGlobalGainOverTimeMultiplier;
                }
            }
        }


        // Small duration before the player can regen stamina again after a move
        if (currentTimeBeforeStaminaRegen <= 0 && !canRegenStamina)
        {
            currentTimeBeforeStaminaRegen = 0;

            canRegenStamina = true;
        }
        else if (!canRegenStamina)
        {
            currentTimeBeforeStaminaRegen -= Time.deltaTime;
        }
    }

    // Trigger the stamina regen pause duration
    public void PauseStaminaRegen()
    {
        canRegenStamina = false;
        currentTimeBeforeStaminaRegen = durationBeforeStaminaRegen;
    }

    // Function to decrement to stamina
    public void StaminaCost(float cost, bool playFX)
    {
        if (!infiniteStamina)
        {
            stamina -= cost;


            if (stamina < lowStaminaGap)
                hasReachedLowStamina = true;

            if (stamina <= 0)
            {
                stamina = 0;
            }


            // FX
            if (useExtraDiegeticFX && playFX)
                staminaLossFX.Play();
        }
    }

    // Update stamina slider value
    void UpdateStaminaSlidersValue()
    {
        // DETECT STAMINA CHARGE UP
        if (Mathf.FloorToInt(oldStamina) < Mathf.FloorToInt(stamina))
        {
            if (!GameManager.Instance.playerDead && GameManager.Instance.gameState == GameManager.GAMESTATE.game)
            {
                if (!staminaRecupAnimOn && !staminaBreakAnimOn)
                {
                    staminaBarChargedAudioEffectSource.Play();

                    if (useExtraDiegeticFX)
                        staminaGainFX.Play();

                    staminaGainFX.GetComponent<ParticleSystem>().Play();
                }
            }
        }


        oldStamina = stamina;
        staminaSliders[0].value = Mathf.Clamp(stamina, 0, 1);


        // FX pos
        staminaLossFX.gameObject.transform.position = staminaSliders[(int)Mathf.Clamp((int)(stamina + 0.5f), 0, maxStamina - 1)].transform.position;
        staminaGainFX.gameObject.transform.position = staminaSliders[(int)Mathf.Clamp((int)(stamina - 0.5f), 0, maxStamina - 1)].transform.position + new Vector3(0.2f, 0, 0) * Mathf.Sign(transform.localScale.x);

        // Stamina recup anim FX pox
        staminaRecupFX.gameObject.transform.position = staminaSliders[(int)Mathf.Clamp((int)(stamina - 0f), 0, maxStamina - 1)].transform.position + new Vector3(0.1f, 0.1f * Mathf.Sign(transform.localScale.x), 0) * Mathf.Sign(transform.localScale.x);
        staminaRecupFinishedFX.gameObject.transform.position = staminaSliders[(int)Mathf.Clamp((int)(stamina - 0f), 0, maxStamina - 1)].transform.position + new Vector3(0.1f, 0.1f * Mathf.Sign(transform.localScale.x), 0) * Mathf.Sign(transform.localScale.x);


        // Break FX pos
        staminaBreakFX.gameObject.transform.position = staminaSliders[(int)Mathf.Clamp((int)(stamina + 0.5f), 0, maxStamina - 1)].transform.position + new Vector3(0.2f, 0.1f * Mathf.Sign(transform.localScale.x), 0) * Mathf.Sign(transform.localScale.x);


        for (int i = 1; i < staminaSliders.Count; i++)
        {
            staminaSliders[i].value = Mathf.Clamp(stamina, i, i + 1) - i;
        }


        if (stamina >= maxStamina)
        {
            if (staminaBarsOpacity > 0)
                staminaBarsOpacity -= 0.05f;
        }
        else if (staminaBarsOpacity != staminaBarBaseOpacity)
        {
            staminaBarsOpacity = staminaBarBaseOpacity;
        }
    }

    // Manages stamina bars opacity
    void SetStaminaBarsOpacity(float opacity)
    {
        if (!staminaRecupAnimOn)
        {
            for (int i = 0; i < staminaSliders.Count; i++)
            {
                Color
                    fillColor = staminaSliders[i].fillRect.GetComponent<Image>().color,
                    backgroundColor = staminaSliders[i].GetComponent<StaminaSlider>().fillArea.color;


                staminaSliders[i].fillRect.GetComponent<Image>().color = new Color(fillColor.r, fillColor.g, fillColor.b, opacity);
                staminaSliders[i].GetComponent<StaminaSlider>().fillArea.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, opacity);
            }
        }
    }

    void UpdateStaminaColor()
    {
        if (!staminaRecupAnimOn && !staminaBreakAnimOn)
        {
            if (stamina < staminaCostForMoves)
            {
                SetStaminaColor(staminaDeadColor);
            }
            else if (stamina < staminaCostForMoves * 2)
            {
                SetStaminaColor(staminaLowColor);
            }
            else
            {
                SetStaminaColor(staminaBaseColor);
            }
        }
    }

    void SetStaminaColor(Color color)
    {
        for (int i = 0; i < staminaSliders.Count; i++)
        {
            staminaSliders[i].fillRect.gameObject.GetComponent<Image>().color = Color.Lerp(staminaSliders[i].fillRect.gameObject.GetComponent<Image>().color, color, Time.deltaTime * 10);
        }
    }
    #endregion




    #region STAMINA ANIMS
    // Not enough stamina anim
    protected void TriggerNotEnoughStaminaAnim(bool state)
    {
        for (int i = 0; i < staminaSliders.Count; i++)
        {
            if (state)
                staminaSliders[i].GetComponent<Animator>().SetTrigger("NotEnoughStamina");
            else
                staminaSliders[i].GetComponent<Animator>().ResetTrigger("NotEnoughStamina");
        }


        // AUDIO
        if (notEnoughStaminaSFX != null)
            notEnoughStaminaSFX.Play();
    }

    [PunRPC]
    void N_TriggerStaminaRecupAnim()
    {
        StartCoroutine("TriggerStaminaRecupAnim");
    }

    // Stamina recup anim
    IEnumerator TriggerStaminaRecupAnim()
    {
        // COLOR
        for (int i = 0; i < staminaSliders.Count; i++)
        {
            staminaSliders[i].fillRect.gameObject.GetComponent<Image>().color = staminaRecupColor;
        }


        staminaRecupAnimOn = true;


        yield return new WaitForSecondsRealtime(staminaRecupTriggerDelay);


        float regeneratedAmount = 0;


        // FX
        if (useExtraDiegeticFX)
            staminaRecupFX.Play();


        while (regeneratedAmount < 1)
        {
            stamina += staminaRecupAnimRegenSpeed;
            regeneratedAmount += staminaRecupAnimRegenSpeed;


            if (stamina >= maxStamina)
                stamina = maxStamina;


            yield return new WaitForSecondsRealtime(0.01f);
        }

        if (useExtraDiegeticFX)
            staminaRecupFinishedFX.Play();
        staminaRecupAnimOn = false;


        // AUDIO
        staminaBarChargedAudioEffectSource.Play();


        // FX
        if (useExtraDiegeticFX)
        {
            staminaGainFX.Play();
            staminaRecupFX.Stop();
        }
    }

    // Stamina break 
    [PunRPC]
    void InitStaminaBreak()
    {
        for (int i = 0; i < staminaSliders.Count; i++)
        {
            staminaSliders[i].fillRect.gameObject.GetComponent<Image>().color = staminaBreakColor;
        }

        staminaBreakAnimOn = true;
        Invoke("TriggerStaminaBreak", 0.4f);
    }

    void TriggerStaminaBreak()
    {
        TriggerNotEnoughStaminaAnim(false);
        TriggerNotEnoughStaminaAnim(true);
        StaminaCost(staminaCostForMoves, false);
        staminaBreakAudioFX.Play();


        // FX
        if (useExtraDiegeticFX)
            staminaBreakFX.Play();

        Invoke("StopStaminaBreak", 0.6f);
    }

    void StopStaminaBreak()
    {
        staminaBreakAnimOn = false;
    }

    #endregion








    #region MOVEMENTS
    // MOVEMENTS
    public virtual void ManageMovementsInputs()
    {

        // The player move if they can in their state
        if (rb.simulated == false)
            rb.simulated = true;

        if (!playerIsAI)
        {
            if (ConnectManager.Instance.connectedToMaster)
            {
                if (photonView.IsMine)
                {
                    rb.velocity = new Vector2(InputManager.Instance.playerInputs[0].horizontal * actualMovementsSpeed, rb.velocity.y);
                }
            }
            else
            {
                rb.velocity = new Vector2(InputManager.Instance.playerInputs[playerNum].horizontal * actualMovementsSpeed, rb.velocity.y);
            }
        }



        // FX
        if (Mathf.Abs(rb.velocity.x) > minSpeedForWalkFX && GameManager.Instance.gameState == GameManager.GAMESTATE.game && playerState == Player.STATE.normal)
        {
            if ((rb.velocity.x * -transform.localScale.x) < 0)
            {
                walkFXFront.Stop();


                if (!walkFXBack.isPlaying)
                    walkFXBack.Play();
            }
            else
            {
                if (!walkFXFront.isPlaying)
                    walkFXFront.Play();


                walkFXBack.Stop();
            }
        }
        else
        {
            walkFXBack.Stop();
            walkFXFront.Stop();
        }
    }
    #endregion








    #region DRAW
    // Detects draw input
    void ManageDraw()
    {
        if (ConnectManager.Instance.connectedToMaster)
        {
            if (InputManager.Instance.playerInputs[0].anyKey)
            {
                photonView.RPC("TriggerDraw", RpcTarget.AllBufferedViaServer);
            }
        }
        else
        {
            if (InputManager.Instance.playerInputs[playerNum].anyKeyDown && !characterChanger.charactersDatabase.charactersList[characterChanger.currentCharacter].locked)
                TriggerDraw();
        }
    }

    // Triggers saber draw and informs the game manager
    [PunRPC]
    public void TriggerDraw()
    {
        SwitchState(STATE.drawing);
        //gameManager.SaberDrawn(playerNum);
        //gameManager.players


        // ANIMATION
        playerAnimations.TriggerDraw();
        playerAnimations.TriggerDrawText();
    }
    #endregion








    #region JUMP
    // JUMP
    void ManageJumpInput()
    {
        if (canJump)
        {

            if (ConnectManager.Instance.enableMultiplayer)
            {
                if (!InputManager.Instance.playerInputs[0].jump)
                {
                    //StartCoroutine(JumpCoroutine());
                    TriggerJumpBeginning();
                }
            }
            else
            {
                if (InputManager.Instance.playerInputs[playerNum].jump)
                {
                    TriggerJumpBeginning();
                }
            }
        }
    }

    void TriggerJumpBeginning()
    {
        SwitchState(STATE.preparingToJump);
        playerAnimations.TriggerJump();
    }

    void ActuallyJump()
    {
        SwitchState(STATE.jumping);
        rb.velocity = new Vector2(rb.velocity.x, jumpPower);
    }
    #endregion








    #region CHARGE
    // Manages the detection of attack charge inputs
    public virtual void ManageChargeInput()
    {
        if (ConnectManager.Instance.enableMultiplayer)
        {
            // Player presses attack button
            if (InputManager.Instance.playerInputs[0].attack && canCharge)
            {
                if (stamina >= staminaCostForMoves)
                {
                    SwitchState(STATE.charging);
                    canCharge = false;
                    chargeStartTime = Time.time;

                    // FX
                    chargeFlareFX.Play();
                    chargeSlider.value = 1;


                    // ANIMATION
                    playerAnimations.CancelCharge(false);
                    playerAnimations.TriggerCharge(true);


                    // STATS
                    if (statsManager)
                        statsManager.AddAction(ACTION.charge, playerNum, 0);
                    else
                        Debug.Log("Couldn't access statsManager to record action, ignoring");


                    // FX
                    chargeFlareFX.Play();


                    // ANIMATION
                    playerAnimations.CancelCharge(false);
                    playerAnimations.TriggerCharge(true);
                }
            }

            // Player releases attack button
            if (!InputManager.Instance.playerInputs[0].attack)
                canCharge = true;
        }
        else
        {
            // Player presses attack button
            if (InputManager.Instance.playerInputs[playerNum].attack && canCharge)
            {
                if (stamina >= staminaCostForMoves)
                {
                    canCharge = false;
                    SwitchState(STATE.charging);


                    // ANIMATION
                    playerAnimations.CancelCharge(false);
                    playerAnimations.TriggerCharge(true);


                    chargeStartTime = Time.time;


                    // STATS
                    if (statsManager)
                        statsManager.AddAction(ACTION.charge, playerNum, 0);
                    else
                        Debug.Log("Couldn't access statsManager to record action, ignoring");


                    // FX
                    chargeFlareFX.Play();
                }
            }

            // ANIMATION STAMINA
            if (InputManager.Instance.playerInputs[playerNum].attackDown && canCharge && stamina <= staminaCostForMoves)
                TriggerNotEnoughStaminaAnim(true);

            // Player releases attack button
            if (!InputManager.Instance.playerInputs[playerNum].attack)
                canCharge = true;
        }
    }

    void ManageCharging()
    {
        //currentChargeFramesPressed++;


        if (ConnectManager.Instance.connectedToMaster && photonView.IsMine)
        {
            //Player releases attack button
            if (!InputManager.Instance.playerInputs[0].attack)
            {
                photonView.RPC("ReleaseAttack", RpcTarget.AllViaServer);
                return;
            }
        }
        else
        {
            //Player releases attack button
            if (!InputManager.Instance.playerInputs[playerNum].attack)
            {
                ReleaseAttack();
                return;
            }
        }


        // If the player has waited too long charging
        if (chargeLevel >= maxChargeLevel)
        {
            if (Time.time - maxChargeLevelStartTime >= maxHoldDurationAtMaxCharge)
            {
                if (ConnectManager.Instance.enableMultiplayer)
                    photonView.RPC("ReleaseAttack", RpcTarget.All);
                else
                    ReleaseAttack();
            }
        }
        // Pass charge levels
        else if (Time.time - chargeStartTime >= durationToNextChargeLevel)
        {
            chargeStartTime = Time.time;


            if (chargeLevel < maxChargeLevel)
            {
                chargeLevel++;
                chargeSlider.value = chargeSlider.maxValue - (chargeSlider.maxValue / maxChargeLevel) * chargeLevel;


                // FX
                chargeFX.Play();
            }

            if (chargeLevel >= maxChargeLevel)
            {
                chargeSlider.value = 0;
                chargeLevel = maxChargeLevel;
                maxChargeLevelStartTime = Time.time;


                // FX
                chargeFullFX.Play();
                chargeFlareFX.Stop();


                // ANIMATION
                playerAnimations.TriggerMaxCharge();
            }
        }
    }
    #endregion









    #region ATTACK
    // Triggers the attack
    [PunRPC]
    void ReleaseAttack()
    {
        // STATS
        int saveChargeLevelForStats = chargeLevel;


        // Calculates attack range
        if (chargeLevel == 1)
            actualAttackRange = lightAttackRange;
        else if (chargeLevel == maxChargeLevel)
            actualAttackRange = heavyAttackRange;
        else
            actualAttackRange = lightAttackRange + (heavyAttackRange - lightAttackRange) * ((float)chargeLevel - 1) / (float)maxChargeLevel;

        actualBackAttackRangeDisjoint = baseBackAttackRangeDisjoint;



        // FX
        // Trail color and width depending on attack range
        if (chargeLevel == 1)
        {
            swordTrail.startColor = lightAttackColor;
            swordTrail.startWidth = lightAttackSwordTrailWidth;
            attackSlashFXParent.transform.localScale = new Vector3(lightAttackSwordTrailScale, attackSlashFXParent.transform.localScale.y, attackSlashFXParent.transform.localScale.z);
        }
        else if (chargeLevel == maxChargeLevel)
        {
            swordTrail.startColor = heavyAttackColor;
            swordTrail.startWidth = heavyAttackSwordTrailWidth;
            attackSlashFXParent.transform.localScale = new Vector3(heavyAttackSwordTrailScale, attackSlashFXParent.transform.localScale.y, attackSlashFXParent.transform.localScale.z);
        }
        else
        {
            swordTrail.startColor = new Color(
                lightAttackColor.r + (heavyAttackColor.r - lightAttackColor.r) * ((float)actualAttackRange - lightAttackRange) / (float)heavyAttackRange,
                lightAttackColor.g + (heavyAttackColor.g - lightAttackColor.g) * ((float)actualAttackRange - lightAttackRange) / (float)heavyAttackRange,
                lightAttackColor.b + (heavyAttackColor.b - lightAttackColor.b) * ((float)actualAttackRange - lightAttackRange) / (float)heavyAttackRange);



            attackSlashFXParent.transform.localScale = new Vector3(
                lightAttackSwordTrailScale + (heavyAttackSwordTrailScale - lightAttackSwordTrailScale) * (actualAttackRange - lightAttackRange) / (heavyAttackRange - lightAttackRange),
                attackSlashFXParent.transform.localScale.y,
                attackSlashFXParent.transform.localScale.z);

            swordTrail.startWidth = lightAttackSwordTrailWidth + (heavyAttackSwordTrailWidth - lightAttackSwordTrailWidth) * (actualAttackRange - lightAttackRange) / (heavyAttackRange - lightAttackRange);


        }
        //Debug.Log(actualAttackRange);





        // STAMINA
        StaminaCost(staminaCostForMoves, true);


        targetsHit.Clear();


        // FX
        Vector3 attackSignPos = attackRangeFX.transform.localPosition;
        attackRangeFX.transform.localPosition = new Vector3(-(actualAttackRange + attackSignDisjoint), attackSignPos.y, attackSignPos.z);
        if (useExtraDiegeticFX)
            attackRangeFX.Play();
        chargeFlareFX.gameObject.SetActive(false);
        chargeFlareFX.gameObject.SetActive(true);


        // Dash direction & distance
        Vector3 dashDirection3D = new Vector3(0, 0, 0);
        float dashDirection = 0;

        int inputNum;
        if (ConnectManager.Instance.connectedToMaster)
            inputNum = 0;
        else
            inputNum = playerNum;

        if (Mathf.Abs(InputManager.Instance.playerInputs[inputNum].horizontal) > attackReleaseAxisInputDeadZoneForDashAttack)
        {
            dashDirection = Mathf.Sign(InputManager.Instance.playerInputs[inputNum].horizontal) * transform.localScale.x;
            dashDirection3D = new Vector3(Mathf.Sign(InputManager.Instance.playerInputs[inputNum].horizontal), 0, 0);


            // Dash distance
            if (Mathf.Sign(InputManager.Instance.playerInputs[inputNum].horizontal) == -Mathf.Sign(transform.localScale.x))
            {
                actualUsedDashDistance = forwardAttackDashDistance;
                actualBackAttackRangeDisjoint = forwardAttackBackrangeDisjoint;


                // FX
                attackDashFXFront.Play();


                // STATS
                if (statsManager)
                    statsManager.AddAction(ACTION.forwardAttack, inputNum, saveChargeLevelForStats);
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }
            else
            {
                actualUsedDashDistance = backwardsAttackDashDistance;


                // FX
                attackDashFXBack.Play();


                // STATS
                if (statsManager)
                    statsManager.AddAction(ACTION.backwardsAttack, inputNum, saveChargeLevelForStats);
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }
        }
        else
        {
            // FX
            attackNeutralFX.Play();


            // STATS
            if (statsManager)
                statsManager.AddAction(ACTION.neutralAttack, inputNum, saveChargeLevelForStats);
            else
                Debug.Log("Couldn't access statsManager to record action, ignoring");
        }


        dashDirection3D *= actualUsedDashDistance;
        initPos = transform.position;
        targetPos = transform.position + dashDirection3D;
        targetPos.y = transform.position.y;
        dashTime = 0;

        rb.velocity = Vector3.zero;




        // ANIMATION
        playerAnimations.TriggerAttack(dashDirection);


        // STATE SWITCH
        //NE PAS SUPPRIMER
        /*StopAllCoroutines();
        Debug.Log("Stop coroutines");*/
        SwitchState(STATE.attacking);
    }

    // Hits with a phantom collider to apply the attack's damage during active frames
    void ApplyAttackHitbox()
    {
        enemyDead = false;
        //Debug.Log(actualAttackRange);

        Collider2D[] hitsCol = Physics2D.OverlapBoxAll(new Vector2(transform.position.x + (transform.localScale.x * (-actualAttackRange + actualBackAttackRangeDisjoint) / 2), transform.position.y), new Vector2(actualAttackRange + actualBackAttackRangeDisjoint, 1), 0);
        List<GameObject> hits = new List<GameObject>();


        foreach (Collider2D c in hitsCol)
        {
            if (c.CompareTag("Player"))
            {
                if (!hits.Contains(c.transform.parent.gameObject))
                {
                    if ((c.transform.parent.position.x - transform.position.x) * transform.localScale.x > 0)
                    {

                    }


                    hits.Add(c.transform.parent.gameObject);
                    //Debug.Log(c.transform.parent.gameObject);
                }
            }
        }


        foreach (GameObject g in hits)
        {
            if (g != gameObject && !targetsHit.Contains(g))
            {
                targetsHit.Add(g);

                enemyDead = g.GetComponent<Player>().TakeDamage(gameObject, chargeLevel);

                // FX
                attackRangeFX.gameObject.SetActive(false);
                attackRangeFX.gameObject.SetActive(true);


                if (enemyDead)
                {
                    SwitchState(STATE.enemyKilled);
                }
            }
        }
    }
    #endregion








    #region MAINTAIN PARRY
    // Detect parry inputs
    void ManageMaintainParryInput()
    {
        if (canMaintainParry)
        {
            if (InputManager.Instance.playerInputs[playerNum].parry && canParry)
            {
                currentParryFramesPressed++;
                canParry = false;


                if (stamina >= maintainParryStaminaCostOverTime)
                    TriggerMaintainParry();


                currentParryFramesPressed = 0;
            }


            if (stamina <= maintainParryStaminaCostOverTime)
            {
                ReleaseMaintainParry();
            }

            if (!InputManager.Instance.playerInputs[playerNum].parry)
            {
                ReleaseMaintainParry();
                canParry = true;
            }
        }
    }

    // Maintain parry coroutine
    void TriggerMaintainParry()
    {
        // ANIMATION
        playerAnimations.ResetMaintainParry();
        playerAnimations.TriggerMaintainParry();


        // STATE
        SwitchState(STATE.maintainParrying);


        // STATS
        statsManager.AddAction(ACTION.parry, playerNum, chargeLevel);
    }

    void ReleaseMaintainParry()
    {
        // ANIMATION
        playerAnimations.EndMaintainParry();
    }
    #endregion







    #region PARRY
    // PARRY
    // Detect parry inputs
    public virtual void ManageParryInput()
    {
        // If online, only take inputs from player 1
        if (canBriefParry)
        {
            if (ConnectManager.Instance.enableMultiplayer)
            {
                if (InputManager.Instance.playerInputs[0].parry && canParry)
                {
                    currentParryFramesPressed++;
                    canParry = false;
                    if (stamina >= staminaCostForMoves)
                    {
                        photonView.RPC("TriggerParry", RpcTarget.AllViaServer);
                    }

                    currentParryFramesPressed = 0;
                }


                if (!InputManager.Instance.playerInputs[0].parry)
                {
                    canParry = true;
                }
            }
            else
            {
                // Stamina animation
                if (InputManager.Instance.playerInputs[playerNum].parryDown && stamina <= staminaCostForMoves && canParry)
                {
                    TriggerNotEnoughStaminaAnim(true);
                }


                if (InputManager.Instance.playerInputs[playerNum].parry && canParry)
                {
                    canParry = false;


                    if (stamina >= staminaCostForMoves)
                        TriggerParry();
                }



                // Can input again if released the input
                if (!InputManager.Instance.playerInputs[playerNum].parry)
                {
                    canParry = true;
                }
            }
        }
    }

    // Parry coroutine
    [PunRPC]
    protected void TriggerParry()
    {
        // ANIMATION
        playerAnimations.TriggerParry();

        SwitchState(STATE.parrying);
        StaminaCost(staminaCostForMoves, true);

        // STATS
        if (statsManager)
            statsManager.AddAction(ACTION.parry, playerNum, chargeLevel);
        else
            Debug.Log("Couldn't access statsManager to record action, ignoring");
    }
    #endregion








    #region POMMEL
    // Detect pommel inputs
    public virtual void ManagePommel(
        )
    {
        if (ConnectManager.Instance.connectedToMaster)
        {
            if (!InputManager.Instance.playerInputs[0].kick)
            {
                canPommel = true;
            }


            if (InputManager.Instance.playerInputs[0].kick && canPommel)
            {
                canPommel = false;

                photonView.RPC("TriggerPommel", RpcTarget.All);
            }
        }
        else
        {
            if (!InputManager.Instance.playerInputs[playerNum].kick)
            {
                canPommel = true;
            }


            if (InputManager.Instance.playerInputs[playerNum].kick && canPommel)
            {
                canPommel = false;

                TriggerPommel();
            }
        }
    }

    // Pommel coroutine
    [PunRPC]
    protected void TriggerPommel()
    {
        Debug.Log("Trigger pommel");

        // ANIMATION
        playerAnimations.TriggerPommel();


        // STATE
        SwitchState(STATE.pommeling);



        targetsHit.Clear();


        // STATS
        if (statsManager)
            statsManager.AddAction(ACTION.pommel, playerNum, chargeLevel);
        else
            Debug.Log("Couldn't access statsManager to record action, ignoring");
    }

    // Apply pommel hitbox depending on kick frames
    void ApplyPommelHitbox()
    {
        Collider2D[] hitsCol = Physics2D.OverlapBoxAll(new Vector2(transform.position.x + (transform.localScale.x * -kickRange / 2), transform.position.y), new Vector2(kickRange, 1), 0);
        List<GameObject> hits = new List<GameObject>();


        foreach (Collider2D c in hitsCol)
        {
            if (c.CompareTag("Player"))
            {
                if (!hits.Contains(c.transform.parent.gameObject))
                {
                    hits.Add(c.transform.parent.gameObject);
                }
            }
        }


        foreach (GameObject g in hits)
        {
            if (g != gameObject && !targetsHit.Contains(g))
            {
                targetsHit.Add(g);

                //g.GetComponent<PlayerStats>().StaminaCost(kickedStaminaLoss);
                if (g.GetComponent<Player>().playerState != Player.STATE.clashed)
                {
                    if (ConnectManager.Instance.connectedToMaster)
                        g.GetComponent<PhotonView>().RPC("Pommeled", RpcTarget.All);
                    else
                        g.GetComponent<Player>().Pommeled();
                }
            }
        }
    }
    #endregion








    #region POMMELED
    // The player have been kicked
    [PunRPC]
    public void Pommeled()
    {
        if (!kickFrame)
        {
            bool wasSneathed = false;


            // ASKS TO START MATCH IF SNEATHED
            if (playerState == STATE.sneathed || playerState == STATE.drawing)
            {
                wasSneathed = true;
                Debug.Log("WassNEATHED");
            }


            // ANIMATIONs
            playerAnimations.CancelCharge(true);
            playerAnimations.ResetPommeledTrigger();
            playerAnimations.TriggerPommeled();


            // Stamina
            Debug.Log($"Pommeled while {playerState}");
            if (playerState == STATE.parrying || playerState == STATE.attacking)
            {
                //StartCoroutine(TriggerStaminaBreakAnim());
                Debug.Log("Stamina break");
                if (ConnectManager.Instance.connectedToMaster)
                    photonView.RPC("InitStaminaBreak", RpcTarget.All);
                else
                    InitStaminaBreak();
            }

            //NE PAS SUPPRIMER
            //StopAllCoroutines();
            SwitchState(STATE.clashed);
            ApplyOrientation(-GameManager.Instance.playersList[otherPlayerNum].transform.localScale.x);


            // STARTS MATCH IF PLAYER WAS SNEATHED
            if (wasSneathed)
            {
                Debug.Log("Draw");
                GameManager.Instance.SaberDrawn(playerNum);
            }


            canCharge = false;
            chargeLevel = 1;


            // Dash knockback
            dashDirection = transform.localScale.x;
            actualUsedDashDistance = kickKnockbackDistance;
            initPos = transform.position;
            targetPos = transform.position + new Vector3(actualUsedDashDistance * dashDirection, 0, 0);
            dashTime = 0;
            isDashing = true;


            // FX
            kickKanasFX.Play();
            kickedFX.Play();
            GameManager.Instance.pommelCameraShake.shakeDuration = GameManager.Instance.pommelCameraShakeDuration;



            // AUDIO
            //audioManager.TriggerClashAudioCoroutine();
            AudioManager.Instance.BattleEventIncreaseIntensity();


            // STATS
            if (statsManager)
                statsManager.AddAction(ACTION.successfulPommel, otherPlayerNum, chargeLevel);
            else
                Debug.Log("Couldn't access statsManager to record action, ignoring");
        }
    }
    #endregion








    #region CLASHED
    // The player have been clashed / parried
    [PunRPC]
    void TriggerClash()
    {
        SwitchState(STATE.clashed);

        //NE PAS SUPPRIMER
        /*StopAllCoroutines();
        Debug.Log("Stop coroutines");*/
        GameManager.Instance.clashCameraShake.shakeDuration = GameManager.Instance.clashCameraShakeDuration;
        GameManager.Instance.TriggerSlowMoCoroutine(GameManager.Instance.clashSlowMoDuration, GameManager.Instance.clashSlowMoTimeScale, GameManager.Instance.clashTimeScaleFadeSpeed);


        temporaryDashDirectionForCalculation = transform.localScale.x;
        actualUsedDashDistance = clashKnockback;
        initPos = transform.position;
        targetPos = transform.position + new Vector3(actualUsedDashDistance * temporaryDashDirectionForCalculation, 0, 0);
        dashTime = 0;
        isDashing = true;


        // ANIMATION
        playerAnimations.ResetClashedTrigger();
        playerAnimations.CancelCharge(true);
        playerAnimations.TriggerClashed(true);


        // SOUND
        AudioManager.Instance.BattleEventIncreaseIntensity();


        // FX
        if (GameManager.Instance.playersList.Count > 1 && !GameManager.Instance.playersList[otherPlayerNum].GetComponent<Player>().clashKanasFX.isPlaying)
            clashKanasFX.Play();
    }
    #endregion








    #region DASH
    //DASH
    // Functions to detect the dash input etc
    public virtual void ManageDashInput()
    {
        // If multiplayer, only check for input 1
        if (ConnectManager.Instance.connectedToMaster)
        {

            // Detects dash with basic input rather than double tap, shortcut
            if (Mathf.Abs(InputManager.Instance.playerInputs[0].dash) < shortcutDashDeadZone && currentShortcutDashStep == DASHSTEP.invalidated && stamina >= staminaCostForMoves)
            {
                //inputManager.playerInputs[playerStats.playerNum - 1].horizontal;
                currentShortcutDashStep = DASHSTEP.rest;
            }


            if (Mathf.Abs(InputManager.Instance.playerInputs[0].dash) > shortcutDashDeadZone && currentShortcutDashStep == DASHSTEP.rest)
            {
                dashDirection = Mathf.Sign(InputManager.Instance.playerInputs[0].dash);

                photonView.RPC("TriggerBasicDash", RpcTarget.All);

                //TriggerBasicDash();
            }


            // Resets the dash input if too much time has passed
            if (currentDashStep == DASHSTEP.firstInput || currentDashStep == DASHSTEP.firstRelease)
            {
                if (Time.time - dashInitializationStartTime > allowanceDurationForDoubleTapDash)
                {
                    currentDashStep = DASHSTEP.invalidated;
                }
            }


            // The player needs to let go the direction before pressing it again to dash
            if (Mathf.Abs(InputManager.Instance.playerInputs[0].horizontal) < dashDeadZone)
            {
                if (currentDashStep == DASHSTEP.firstInput)
                {
                    currentDashStep = DASHSTEP.firstRelease;
                }
                // To make the first dash input he must have not been pressing it before, we need a double tap
                else if (currentDashStep == DASHSTEP.invalidated)
                {
                    currentDashStep = DASHSTEP.rest;
                }
            }


            // When the player presses the direction
            // Presses the
            if (Mathf.Abs(InputManager.Instance.playerInputs[0].horizontal) > dashDeadZone)
            {
                temporaryDashDirectionForCalculation = Mathf.Sign(InputManager.Instance.playerInputs[0].horizontal);

                if (currentDashStep == DASHSTEP.rest)
                {
                    currentDashStep = DASHSTEP.firstInput;
                    dashDirection = temporaryDashDirectionForCalculation;
                    dashInitializationStartTime = Time.time;

                }
                // Dash is validated, the player is gonna dash
                else if (currentDashStep == DASHSTEP.firstRelease && dashDirection == temporaryDashDirectionForCalculation)
                {
                    currentDashStep = DASHSTEP.invalidated;
                    photonView.RPC("TriggerBasicDash", RpcTarget.All);
                    //TriggerBasicDash();
                }
            }
        }


        // If not multiplayer, check for the player's input
        else
        {

            if (Mathf.Abs(InputManager.Instance.playerInputs[playerNum].dash) < shortcutDashDeadZone && currentShortcutDashStep == DASHSTEP.invalidated)
            {
                //inputManager.playerInputs[playerStats.playerNum - 1].horizontal;
                currentShortcutDashStep = DASHSTEP.rest;
            }

            // Detects dash with basic input rather than double tap, shortcut
            if (Mathf.Abs(InputManager.Instance.playerInputs[playerNum].dash) > shortcutDashDeadZone && currentShortcutDashStep == DASHSTEP.rest)
            {
                dashDirection = Mathf.Sign(InputManager.Instance.playerInputs[playerNum].dash);


                TriggerBasicDash();
            }


            // Resets the dash input if too much time has passed
            if (currentDashStep == DASHSTEP.firstInput || currentDashStep == DASHSTEP.firstRelease)
            {
                if (Time.time - dashInitializationStartTime > allowanceDurationForDoubleTapDash)
                {
                    currentDashStep = DASHSTEP.invalidated;
                }
            }


            // The player needs to let go the direction before pressing it again to dash
            if (Mathf.Abs(InputManager.Instance.playerInputs[playerNum].horizontal) < dashDeadZone)
            {
                if (currentDashStep == DASHSTEP.firstInput)
                {
                    currentDashStep = DASHSTEP.firstRelease;
                }
                // To make the first dash input he must have not been pressing it before, we need a double tap
                else if (currentDashStep == DASHSTEP.invalidated)
                {
                    currentDashStep = DASHSTEP.rest;
                }
            }


            if (Mathf.Abs(InputManager.Instance.playerInputs[playerNum].horizontal) > dashDeadZone && Mathf.Sign(InputManager.Instance.playerInputs[playerNum].horizontal) != temporaryDashDirectionForCalculation)
            {
                if (currentDashStep == DASHSTEP.firstInput || currentDashStep == DASHSTEP.firstRelease)
                {
                    currentDashStep = DASHSTEP.invalidated;
                }
            }


            // When the player presses the direction
            if (Mathf.Abs(InputManager.Instance.playerInputs[playerNum].horizontal) > dashDeadZone)
            {
                temporaryDashDirectionForCalculation = Mathf.Sign(InputManager.Instance.playerInputs[playerNum].horizontal);

                if (currentDashStep == DASHSTEP.rest)
                {
                    currentDashStep = DASHSTEP.firstInput;
                    dashDirection = temporaryDashDirectionForCalculation;
                    dashInitializationStartTime = Time.time;

                }
                // Dash is validated, the player is gonna dash
                else if (currentDashStep == DASHSTEP.firstRelease && dashDirection == temporaryDashDirectionForCalculation)
                {
                    currentDashStep = DASHSTEP.invalidated;
                    TriggerBasicDash();
                }
            }
        }
    }

    // If the player collides with a wall
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            //EndDash();
            targetPos = transform.position;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            //EndDash();
            targetPos = transform.position;
        }
    }

    // Triggers the dash (Not the clash or attack dash) for it to run
    [PunRPC]
    protected void TriggerBasicDash()
    {
        // Triggers dash if enough stamina
        if (stamina >= staminaCostForMoves)
        {
            // CHANGE STATE
            SwitchState(STATE.dashing);


            // STAMINA
            StaminaCost(staminaCostForMoves, true);

            //NE PAS SUPPRIMER
            /* StopAllCoroutines();
             Debug.Log("Stop coroutines");*/
            dashTime = 0;


            if (dashDirection == -transform.localScale.x)
            {
                actualUsedDashDistance = forwardDashDistance;
                dashFXFront.Play();


                // STATS
                if (statsManager)
                    statsManager.AddAction(ACTION.forwardDash, otherPlayerNum, chargeLevel);
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }
            else
            {
                actualUsedDashDistance = backwardsDashDistance;
                dashFXBack.Play();


                // STATS
                if (statsManager)
                    statsManager.AddAction(ACTION.backwardsDash, otherPlayerNum, chargeLevel);
                else
                    Debug.Log("Couldn't access statsManager to record action, ignoring");
            }


            // ANIMATION
            playerAnimations.TriggerDash(dashDirection * transform.localScale.x);


            initPos = transform.position;
            targetPos = transform.position + new Vector3(actualUsedDashDistance * dashDirection, 0, 0);
        }


        // Stamina animation
        else
            TriggerNotEnoughStaminaAnim(true);
    }

    // Runs the dash, to use in FixedUpdate
    void RunDash()
    {
        if (isDashing)
        {
            // Sets the dash speed
            if (playerState == STATE.clashed)
                dashTime += Time.deltaTime * clashKnockbackSpeed;
            else
                dashTime += Time.deltaTime * baseDashSpeed;


            transform.position = Vector3.Lerp(initPos, targetPos, dashTime);


            if (dashTime >= 1.0f)
            {
                EndDash();
            }
        }
    }

    // End currently running dash
    void EndDash()
    {
        // CHANGE STATE
        if (playerState != STATE.attacking && playerState != STATE.recovering)
        {
            SwitchState(STATE.normal);
        }


        isDashing = false;


        // ANIMATION
        playerAnimations.TriggerClashed(false);
        //Debug.Log("End dash");


        // FX
        dashFXFront.Stop();
        dashFXBack.Stop();
        attackDashFXFront.Stop();
        attackDashFXBack.Stop();
    }
    #endregion







    #region ORIENTATION
    // ORIENTATION CALLED IN UPDATE
    public virtual void ManageOrientation()
    {
        if (photonView != null)
            if (!photonView.IsMine)
                return;

        // Orient towards the enemy if player can in their current state
        if (canOrientTowardsEnemy)
        {
            GameObject z = null, p1 = null, p2 = null;
            Vector3 self = Vector3.zero, other = Vector3.zero;
            Player[] stats = new Player[2];
            for (int i = 0; i < GameManager.Instance.playersList.Count; i++)
            {
                if (GameManager.Instance.playersList[i] == null)
                    return;


                stats[i] = GameManager.Instance.playersList[i].GetComponent<Player>();
            }

            foreach (Player stat in stats)
            {
                if (stat == null)
                    return;

                switch (stat.playerNum)
                {
                    case 0:
                        p1 = stat.gameObject;
                        break;

                    case 1:
                        p2 = stat.gameObject;
                        break;

                    default:
                        break;
                }
            }

            if (p1 == null)
            {
                Debug.LogWarning("Player 1 not found");
                z = new GameObject();
                z.transform.position = Vector3.zero;
                p1 = z;
            }

            if (p2 == null)
            {
                Debug.LogWarning("Player 2 not found");
                z = new GameObject();
                z.transform.position = Vector3.zero;
                p2 = z;
            }

            if (p1 == gameObject)
            {
                self = p1.transform.position;
                other = p2.transform.position;
            }
            else if (p2 == gameObject)
            {
                self = p2.transform.position;
                other = p1.transform.position;
            }

            float sign = Mathf.Sign(self.x - other.x);

            Destroy(z);

            if (orientationCooldownFinished)
                ApplyOrientation(sign);
        }


        if (Time.time >= orientationCooldown + orientationCooldownStartTime)
        {
            orientationCooldownFinished = true;
        }
    }


    // Immediatly rotates the player
    protected void ApplyOrientation(float sign)
    {
        if (sign > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
            UpdateNameScale(Mathf.Abs(characterNameDisplay.rectTransform.localScale.x));
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
            UpdateNameScale(-Mathf.Abs(characterNameDisplay.rectTransform.localScale.x));
        }


        orientationCooldownStartTime = Time.time;
        orientationCooldownFinished = false;
    }


    void UpdateFXOrientation()
    {
        // FX
        Vector3 deathBloodFXRotation = deathBloodFX.gameObject.transform.localEulerAngles;


        if (GameManager.Instance.playersList[otherPlayerNum].transform.localScale.x >= 0)
        {
            deathBloodFX.gameObject.transform.localEulerAngles = new Vector3(deathBloodFXRotation.x, deathBloodFXRotation.y, -deathBloodFXRotationForDirectionChange * transform.localScale.x);


            // Changes the draw text indication's scale so that it's, well, readable for a human being
            drawText.transform.localScale = new Vector3(-drawTextBaseScale.x, drawTextBaseScale.y, drawTextBaseScale.z);
        }
        else
        {
            deathBloodFX.gameObject.transform.localEulerAngles = deathBloodFXBaseRotation;


            // Changes the draw text indication's scale so that it's, well, readable for a human being
            drawText.transform.localScale = new Vector3(drawTextBaseScale.x, drawTextBaseScale.y, drawTextBaseScale.z);
        }
    }

    void UpdateNameScale(float newXScale)
    {
        characterNameDisplay.rectTransform.localScale = new Vector3(newXScale, characterNameDisplay.rectTransform.localScale.y, characterNameDisplay.rectTransform.localScale.z);
    }
    #endregion






    #region DRAW RANGE
    // Draw the attack range when the player is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(new Vector3(transform.position.x + (transform.localScale.x * (-lightAttackRange + baseBackAttackRangeDisjoint) / 2), transform.position.y, transform.position.z), new Vector3(lightAttackRange + baseBackAttackRangeDisjoint, 1, 1));
        Gizmos.DrawWireCube(new Vector3(transform.position.x + (transform.localScale.x * -kickRange / 2), transform.position.y, transform.position.z), new Vector3(kickRange, 1, 1));
    }

    // Draw the attack range is the attack is in active frames in the scene viewer
    private void OnDrawGizmos()
    {
        if (activeFrame)
            Gizmos.DrawWireCube(new Vector3(transform.position.x + (transform.localScale.x * (-actualAttackRange + baseBackAttackRangeDisjoint) / 2), transform.position.y, transform.position.z), new Vector3(actualAttackRange + baseBackAttackRangeDisjoint, 1, 1));

        if (kickFrame)
            Gizmos.DrawWireCube(new Vector3(transform.position.x + (transform.localScale.x * -kickRange / 2), transform.position.y, transform.position.z), new Vector3(kickRange, 1, 1));
    }
    #endregion







    #region CHEATS
    // CHEATS
    void CheatsInputs()
    {
        if (Input.GetKeyDown(clashCheatKey))
        {
            TriggerClash();
        }


        if (Input.GetKeyDown(deathCheatKey))
        {
            TakeDamage(gameObject, 1);
        }


        if (Input.GetKeyDown(staminaCheatKey))
        {
            stamina = maxStamina;
        }


        if (Input.GetKeyDown(stopStaminaRegenCheatKey))
        {
            if (canRegenStamina)
                canRegenStamina = false;
            else
                canRegenStamina = true;
        }


        if (Input.GetKeyDown(triggerStaminaRecupAnim))
        {
            StartCoroutine(TriggerStaminaRecupAnim());
            Debug.Log("Triggered stamina recup anim");
        }
    }

    #endregion
    #endregion


    #region Network
    [PunRPC]
    public void ResetPos()
    {
        netTargetPos = rb.position;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.name);
            stream.SendNext(currentHealth);
            stream.SendNext(playerNum);
            stream.SendNext(stamina);
            stream.SendNext(transform.position);
            stream.SendNext(transform.localScale.x);
            stream.SendNext(enemyDead);
            stream.SendNext(staminaBarsOpacity);
            //stream.SendNext(rb.velocity);
            stream.SendNext(actualMovementsSpeed);
            stream.SendNext(playerState);
        }
        else if (stream.IsReading)
        {
            transform.name = (string)stream.ReceiveNext();
            currentHealth = (float)stream.ReceiveNext();
            playerNum = (int)stream.ReceiveNext();
            stamina = (float)stream.ReceiveNext();
            Vector3 DistantPos = (Vector3)stream.ReceiveNext();
            float xScale = (float)stream.ReceiveNext();
            enemyDead = (bool)stream.ReceiveNext();
            staminaBarsOpacity = (float)stream.ReceiveNext();
            //rb.velocity = (Vector2)stream.ReceiveNext();
            actualMovementsSpeed = (float)stream.ReceiveNext();
            SwitchState((STATE)stream.ReceiveNext());

            //Calculate target position based on lag
            netTargetPos = new Vector2(DistantPos.x, DistantPos.y);

            transform.localScale = new Vector3(xScale, transform.localScale.y, transform.localScale.z);
        }
    }
    #endregion
}
