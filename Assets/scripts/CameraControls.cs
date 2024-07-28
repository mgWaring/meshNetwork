using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float sprintSpeed;
    public GameObject projectile;

    private CameraInput m_Controls;
    private Vector2 m_Rotation;

    private bool mouseLook = false;

    public void OnEnable()
    {
        m_Controls.Enable();
    }

    public void OnDisable()
    {
        m_Controls.Disable();
    }
    public void Awake()
    {
        m_Controls = new CameraInput();


        m_Controls.camera.act.performed += Act;
        m_Controls.camera.toggle.performed += ctx => mouseLook = !mouseLook;
    }

    private void Act(InputAction.CallbackContext ctx)
    {
        if(!mouseLook)
            return;
    }

    public void Update()
    {
        Vector2 look = m_Controls.camera.look.ReadValue<Vector2>();
        Vector2 move = m_Controls.camera.move.ReadValue<Vector2>();

        // Update orientation first, then move. Otherwise move orientation will lag
        // behind by one frame.
        Look(look);
        Move(move);
    }

    private void Move(Vector2 direction)
    {
        if(!mouseLook)
            return;
        if (direction.sqrMagnitude < 0.01)
            return;
        float scaledMoveSpeed = moveSpeed * Time.deltaTime;

        Vector3 move = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z) * new Vector3(direction.x, 0, direction.y);
        transform.position += move * scaledMoveSpeed;
    }

    private void Look(Vector2 rotate)
    {
        if(!mouseLook)
            return;
        if (rotate.sqrMagnitude < 0.01)
            return;
        float scaledRotateSpeed = rotateSpeed * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRotateSpeed;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
        transform.localEulerAngles = m_Rotation;
    }
}
