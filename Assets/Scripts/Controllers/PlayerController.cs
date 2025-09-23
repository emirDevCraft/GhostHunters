using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;          
    [SerializeField] private float sprintMultiplier = 1.8f; 
    [SerializeField] private float rotationSpeed = 10f;     

    [Header("Jump & Ground")]
    [SerializeField] private float jumpHeight = 1.2f;       
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private Transform groundCheckPoint;   
    [SerializeField] private LayerMask groundLayers = ~0;  

    [Header("References")]
    [SerializeField] private Transform cameraTransform;     
    [SerializeField] private Animator animator;            

    private Rigidbody rb;

    
    private readonly int animSpeedHash = Animator.StringToHash("Speed");
    private readonly int animGroundHash = Animator.StringToHash("IsGrounded");
    private readonly int animJumpHash = Animator.StringToHash("Jump");

    private bool isGrounded;
    private bool jumpPressed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

       
        if (groundCheckPoint == null)
        {
            var go = new GameObject("GroundCheckPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            groundCheckPoint = go.transform;
        }
    }

    private void Update()
    {
        
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);

        
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);

        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpPressed = true;
        }

        
        if (animator)
        {
            float speedNormalized = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude / (moveSpeed * sprintMultiplier);
            animator.SetFloat(animSpeedHash, speedNormalized, 0.08f, Time.deltaTime);
            animator.SetBool(animGroundHash, isGrounded);
        }

        
        moveInput = new Vector2(inputX, inputZ);
        sprintPressed = sprint;
    }

    private Vector2 moveInput;
    private bool sprintPressed;

    private void FixedUpdate()
    {
        
        Vector3 camForward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 camRight = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;

        Vector3 desiredMove = (camForward * moveInput.y + camRight * moveInput.x);
        float targetSpeed = moveSpeed * (sprintPressed ? sprintMultiplier : 1f);

        Vector3 horizontalVel = desiredMove.normalized * targetSpeed;

        
        Vector3 newVelocity = new Vector3(horizontalVel.x, rb.velocity.y, horizontalVel.z);

       
        if (jumpPressed && isGrounded)
        {
            float gravity = -Physics.gravity.y;
            float jumpV = Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, jumpHeight));
            newVelocity.y = jumpV;

            if (animator) animator.SetTrigger(animJumpHash);

            jumpPressed = false;
        }

        rb.velocity = newVelocity;

       
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
            Quaternion smoothed = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothed);
        }
    }

    
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }

    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }
}