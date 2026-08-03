using UnityEngine;

namespace Player
{
    /// <summary>
    /// This is just a simple class for being able to control the player/camera and to define the current player position for tracking POI objects.
    /// I'm just using a very bare-bones implementation because the player controller and camera seem to be out of scope for this prototype.
    /// I'd like to emphasise that I would not use inputs or cameras like this is a real project.
    /// 
    /// Note: WASD for horizontal movement and Space/Shift for vertical movement.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] Camera camera;
        [SerializeField] Transform playerTransform;

        [Header("Movement Settings")]
        [SerializeField] float moveSpeed = 50f;
        [SerializeField] float turnSpeed = 100f;
        
        float yaw = 0;
        
        /// <summary>
        /// This is a very simple singleton setup. I would not make a player controller a singleton in a real project.
        /// But for the sake of trying to keep this basic to avoid overcomplicating the prototype.
        /// </summary>
        public static PlayerController Instance;
        void Awake() => Instance = this;


        void Update()
        {
            HandleRotation();
            HandleMovement();
        }

        void HandleRotation()
        {
            float horizontal = Input.GetAxis("Horizontal"); // A/D
            yaw += horizontal * turnSpeed * Time.deltaTime;

            playerTransform.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        void HandleMovement()
        {
            float vertical = Input.GetAxis("Vertical");     // W/S

            // Forward/backward movement relative to facing direction, only on XZ plane
            Vector3 forward = playerTransform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 moveDirection = (forward * vertical).normalized;

            // Vertical movement
            // Space = Up
            // Shift = Down
            float upDown = 0;
            if (Input.GetKey(KeyCode.Space)) upDown += 1;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) upDown -= 1;

            Vector3 verticalMove = Vector3.up * upDown;

            playerTransform.position += (moveDirection + verticalMove) * (moveSpeed * Time.deltaTime);
        }

        public static Vector3 GetPosition() => Instance.playerTransform.position;

        public bool IsInCameraFrustum(Vector3 position)
        {
            Vector3 screenPoint = camera.WorldToViewportPoint(position);
            return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
        }
    }
}