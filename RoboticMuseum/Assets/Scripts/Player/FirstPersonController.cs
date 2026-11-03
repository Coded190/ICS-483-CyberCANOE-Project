using UnityEngine;

namespace RoboticMuseum.Player
{
    /// <summary>
    /// WASD moves the body on the horizontal plane. Mouse yaw turns the body.
    /// Mouse pitch turns the head only, so both eye cameras stay parallel.
    /// Click locks the cursor. Escape releases it.
    /// </summary>
    public class FirstPersonController : MonoBehaviour
    {
        public Transform head;
        public float eyeHeightMeters = 1.6f;
        public float moveSpeedMetersPerSecond = 3.5f;
        public float lookDegreesPerAxisUnit = 2.0f;
        public float pitchLimitDegrees = 80f;

        CharacterController body;
        float pitchDegrees;
        float verticalVelocity;
        bool loggedStartup;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            if (head != null)
            {
                var local = head.localPosition;
                local.y = eyeHeightMeters;
                head.localPosition = local;
            }
        }

        void Update()
        {
            if (!loggedStartup)
            {
                Debug.Log(
                    $"[FirstPerson] startup eye_height_m={eyeHeightMeters:F3} speed_mps={moveSpeedMetersPerSecond:F2} pitch_limit_deg={pitchLimitDegrees:F1}");
                loggedStartup = true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Cursor.lockState == CursorLockMode.Locked && head != null)
            {
                float yaw = Input.GetAxis("Mouse X") * lookDegreesPerAxisUnit;
                float pitchDelta = Input.GetAxis("Mouse Y") * lookDegreesPerAxisUnit;
                transform.Rotate(0f, yaw, 0f, Space.World);
                pitchDegrees = Mathf.Clamp(pitchDegrees - pitchDelta, -pitchLimitDegrees, pitchLimitDegrees);
                head.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            }

            Vector3 wish = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                wish += Vector3.forward;
            }

            if (Input.GetKey(KeyCode.S))
            {
                wish += Vector3.back;
            }

            if (Input.GetKey(KeyCode.A))
            {
                wish += Vector3.left;
            }

            if (Input.GetKey(KeyCode.D))
            {
                wish += Vector3.right;
            }

            if (wish.sqrMagnitude > 1f)
            {
                wish.Normalize();
            }

            if (body == null)
            {
                return;
            }

            if (body.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }

            Vector3 motion = transform.TransformDirection(wish) * moveSpeedMetersPerSecond;
            motion.y = verticalVelocity;
            body.Move(motion * Time.deltaTime);
        }
    }
}
