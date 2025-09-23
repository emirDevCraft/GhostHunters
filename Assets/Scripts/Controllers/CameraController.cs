using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;              
    [SerializeField] private float targetHeight = 1.7f;     

    [Header("Camera Distance")]
    [SerializeField] private float distance = 5f;           
    [SerializeField] private float minDistance = 2f;       
    [SerializeField] private float maxDistance = 10f;       
    [SerializeField] private float zoomSpeed = 2f;          

    [Header("Camera Rotation")]
    [SerializeField] private float mouseSensitivity = 2f;   
    [SerializeField] private float minVerticalAngle = -30f; 
    [SerializeField] private float maxVerticalAngle = 60f;  

    [Header("Camera Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.3f;  
    [SerializeField] private float rotationSmoothTime = 0.15f;  

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;  
    [SerializeField] private float collisionRadius = 0.3f;     
    [SerializeField] private float collisionOffset = 0.2f;     

    [Header("Auto Rotation")]
    [SerializeField] private bool autoRotateWhenMoving = true; 
    [SerializeField] private float autoRotateSpeed = 2f;       
    [SerializeField] private float autoRotateDelay = 2f;       

    
    private float yaw;                 
    private float pitch;                
    private float currentDistance;      
    
    private Vector3 positionVelocity;  
    private Vector3 rotationVelocity;   
    
    private Vector3 lastTargetPosition; 
    private float timeSinceLastMovement;

    
    private bool isMouseLocked = true;

    private void Start()
    {
       
        if (target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError("ThirdPersonCameraFollow: Target not found!");
            return;
        }

        
        currentDistance = distance;
        lastTargetPosition = target.position;
        
        
        Vector3 angles = target.eulerAngles;
        yaw = angles.y;
        pitch = 20f; // 

        
        SetCursorLock(true);
    }

    private void Update()
    {
        if (target == null) return;

        HandleInput();
        HandleAutoRotation();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(!isMouseLocked);
        }

        
        if (!isMouseLocked) return;

        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    private void HandleAutoRotation()
    {
        if (!autoRotateWhenMoving) return;

        
        Vector3 currentTargetPos = target.position;
        float moveDistance = Vector3.Distance(currentTargetPos, lastTargetPosition);
        
        if (moveDistance > 0.1f)
        {
            timeSinceLastMovement = 0f;
            lastTargetPosition = currentTargetPos;
        }
        else
        {
            timeSinceLastMovement += Time.deltaTime;

            
            if (timeSinceLastMovement > autoRotateDelay)
            {
                float targetYaw = target.eulerAngles.y;
                float yawDifference = Mathf.DeltaAngle(yaw, targetYaw);
                
                if (Mathf.Abs(yawDifference) > 5f)
                {
                    yaw = Mathf.LerpAngle(yaw, targetYaw, autoRotateSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void UpdateCameraPosition()
    {
        
        Vector3 targetPosition = target.position + Vector3.up * targetHeight;

        
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        
        Vector3 idealPosition = targetPosition - (rotation * Vector3.forward * currentDistance);

        
        Vector3 finalPosition = HandleCameraCollision(targetPosition, idealPosition);

       
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref positionVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime / rotationSmoothTime);
    }

    private Vector3 HandleCameraCollision(Vector3 targetPos, Vector3 idealPos)
    {
        Vector3 direction = idealPos - targetPos;
        float idealDistance = direction.magnitude;

        
        if (Physics.SphereCast(targetPos, collisionRadius, direction.normalized, out RaycastHit hit, idealDistance, collisionLayers))
        {
            
            float safeDistance = Mathf.Max(hit.distance - collisionOffset, minDistance * 0.5f);
            return targetPos + direction.normalized * safeDistance;
        }

        return idealPos;
    }

    private void SetCursorLock(bool locked)
    {
        isMouseLocked = locked;
        
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

   
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            lastTargetPosition = target.position;
    }

    public void SetDistance(float newDistance)
    {
        currentDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        distance = currentDistance;
    }

    public void AddRotation(float deltaYaw, float deltaPitch)
    {
        yaw += deltaYaw;
        pitch = Mathf.Clamp(pitch + deltaPitch, minVerticalAngle, maxVerticalAngle);
    }

        private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + Vector3.up * targetHeight;
        
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 0.2f);
        
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
        
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(targetPos, transform.position);
    }
}